﻿#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2019/04/10 23:43
// Modified On:  2019/04/17 14:31
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anotar.Serilog;
using CodeHollow.FeedReader;
using Forge.Forms;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Plugins.Feeds.Configs;
using SuperMemoAssistant.Plugins.Feeds.Extensions;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.HTML;
using SuperMemoAssistant.Services.HTML.Extensions;

namespace SuperMemoAssistant.Plugins.Feeds.Tasks
{
  public static class FeedTasks
  {
    #region Methods

    public static async Task<List<FeedData>> DownloadFeeds(FeedsCfg feedsCfg)
    {
      LogTo.Debug("Downloading feeds");
      var res = await Task.WhenAll(feedsCfg.Feeds.Select(DownloadFeed));

      var feedsData = res.Where(fd => fd != null).ToList();

      LogTo.Debug($"Downloaded {feedsData.Sum(fd => fd.NewItems.Count)} items");

      return feedsData;
    }

    private static async Task<FeedData> DownloadFeed(FeedCfg feedCfg)
    {
      try
      {
        var lastRefresh = DateTime.Now;
        var feed        = await FeedReader.ReadAsync(feedCfg.InterpolateUrl());

        if (feed?.Items == null || feed.Items.Count <= 0)
          return null;

        feedCfg.PendingRefreshDate = lastRefresh;
        var feedData = await DownloadFeedContents(new FeedData(feedCfg, feed));

        if (feedData == null)
          return null;

        feedData.NewItems = feedData.NewItems
                                    .OrderByDescending(fi => fi.PublishingDate ?? DateTime.MinValue)
                                    .ToList();

        return feedData;
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, $"Exception while reading feed {feedCfg.Name}");
      }

      return null;
    }

    private static async Task<FeedData> DownloadFeedContents(FeedData feedData)
    {
      var feedCfg   = feedData.FeedCfg;
      var throttler = new SemaphoreSlim(6);
      var client    = new HttpClient(new HttpClientHandler { UseCookies = false });
      
      client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

      if (string.IsNullOrWhiteSpace(feedCfg.Cookie) == false)
        client.DefaultRequestHeaders.Add("Cookie", feedCfg.Cookie);

      try
      {
        var feedItemTasks = feedData.Feed.Items.Select(
          feedItem => DownloadFeedContent(feedCfg,
                                          feedItem,
                                          throttler,
                                          // ReSharper disable once AccessToDisposedClosure
                                          client)
        );
        var feedItems = await Task.WhenAll(feedItemTasks);

        feedData.NewItems.AddRange(feedItems.Where(fi => fi != null));

        return feedData.NewItems.Any() == false ? null : feedData;
      }
      finally
      {
        client.Dispose();
      }
    }

    private static async Task<FeedItemExt> DownloadFeedContent(FeedCfg       feedCfg,
                                                               FeedItem      feedItem,
                                                               SemaphoreSlim throttler,
                                                               HttpClient    client)
    {
      try
      {
        await throttler.WaitAsync();

        //
        // Check & update publishing dates

        if (feedCfg.UsePubDate)
        {
          if (feedItem.PublishingDate == null)
          {
            LogTo.Warning(
              $"Date missing, or unknown format for feed {feedCfg.Name}, item title '{feedItem.Title}', raw date '{feedItem.PublishingDateString}'");
            return null;
          }

          if (feedItem.PublishingDate <= feedCfg.LastPubDate)
            return null;
        }

        //
        // Check guid

        if (feedCfg.UseGuid)
          if (feedCfg.EntriesGuid.Contains(feedItem.Id))
            return null;

        //
        // Check categories

        if (feedCfg.ShouldExcludeCategory(feedItem))
          return null;

        //
        // Download content or use inline content

        if (feedItem.Link != null)
        {
          var httpReq = new HttpRequestMessage(HttpMethod.Get,
                                               feedItem.MakeLink(feedCfg));
          var httpResp = await client.SendAsync(httpReq);

          if (httpResp != null && httpResp.IsSuccessStatusCode)
          {
            feedItem.Content = await httpResp.Content.ReadAsStringAsync();
          }

          else
          {
            feedItem.Content = null;
            LogTo.Warning(
              $"Failed to download content for feed {feedCfg.Name}, item title '{feedItem.Title}', link '{feedItem.MakeLink(feedCfg)}'. HTTP Status code : {httpResp?.StatusCode}");
          }
        }

        else
        {
          feedItem.Content = feedItem.Content ?? feedItem.Description;
        }

        if (string.IsNullOrWhiteSpace(feedItem.Content))
          return null;

        //
        // Process content if necessary & push back

        if (feedCfg.Filters.Any())
          feedItem.Content = string.Join(
            "\r\n",
            feedCfg.Filters
                   .Select(f => f.Filter(feedItem.Content))
                   .Where(s => string.IsNullOrWhiteSpace(s) == false)
          );

        if (feedItem.Link != null)
          feedItem.Content = HtmlUtils.EnsureAbsoluteLinks(feedItem.Content, new Uri(feedItem.MakeLink(feedCfg)));

        // Add feed item
        return new FeedItemExt(feedItem);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, $"Exception while downloading content for feed {feedCfg.Name}, item title '{feedItem.Title}', link '{feedItem.MakeLink(feedCfg)}'");
      }
      finally
      {
        throttler.Release();
      }

      return null;
    }

#pragma warning disable 1998
    public static async Task ImportFeeds(
#pragma warning restore 1998
      ICollection<FeedData> feedsData,
      Action<int, int>      progressCallback = null)
    {
      try
      {
        int i          = 0;
        int totalCount = feedsData.Sum(fd => fd.NewItems.Count);

        var builders = new List<ElementBuilder>();

        progressCallback?.Invoke(i, totalCount);

        foreach (var feedData in feedsData)
        foreach (var feedItem in feedData.NewItems.Where(fi => fi.IsSelected))
          builders.Add(
            new ElementBuilder(ElementType.Topic,
                               new TextContent(true, feedItem.Content))
              .WithParent(feedData.FeedCfg.RootElement)
              .WithPriority(feedData.FeedCfg.Priority)
              .WithReference(
                // ReSharper disable once PossibleInvalidOperationException
                r => r.WithDate(feedItem.PublishingDate?.ToString())
                      .WithTitle(feedItem.Title)
                      .WithAuthor(feedItem.Author)
                      .WithComment(StringEx.Join(", ", feedItem.Categories))
                      .WithSource($"Feed: {feedData.FeedCfg.Name} (<a>{feedData.FeedCfg.SourceUrl}</a>)")
                      .WithLink(feedItem.MakeLink(feedData.FeedCfg)))
              .DoNotDisplay()
          );

        var res = Svc.SMA.Registry.Element.Add(
          out var results,
          ElemCreationFlags.CreateSubfolders,
          builders.ToArray()
        );

        if (res == false)
        {
          var msg = results.GetErrorString();
          Show.Window().For(new Alert(msg, "Feeds: Error")).RunAsync();

          return;
        }

        progressCallback?.Invoke(++i, totalCount);

        // Update feeds state

        foreach (var feedData in feedsData)
        {
          var lastPubDate = feedData.FeedCfg.LastPubDate;
          var feedItems = feedData.NewItems.Where(fi => fi.IsSelected).ToList();

          foreach (var feedItem in feedItems)
          {
            // published date time
            if (feedItem.PublishingDate.HasValue)
              feedData.FeedCfg.LastPubDate = feedItem.PublishingDate > lastPubDate
                ? feedItem.PublishingDate.Value
                : lastPubDate;

            // Guid
            feedData.FeedCfg.EntriesGuid.Add(feedItem.Id);
          }

          if (feedItems.Any())
            feedData.FeedCfg.LastRefreshDate = feedData.FeedCfg.PendingRefreshDate;
        }

        Svc<FeedsPlugin>.Plugin.SaveConfig();
      }
      catch (Exception ex)
      {
        // TODO: report error through callback & display
        LogTo.Error(ex, "Exception while importing feed item in SuperMemo");
      }
    }

    #endregion
  }
}
