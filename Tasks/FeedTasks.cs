#region License & Metadata

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
// Modified On:  2019/04/11 17:34
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Anotar.Serilog;
using CodeHollow.FeedReader;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Plugins.Feeds.Configs;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.Feeds.Tasks
{
  public static class FeedTasks
  {
    #region Methods

    public static async Task<List<FeedData>> DownloadFeeds(FeedsCfg feedsCfg)
    {
      var res = await Task.WhenAll(feedsCfg.Feeds.Select(DownloadFeed));

      return res.Where(fd => fd != null).ToList();
    }

    private static async Task<FeedData> DownloadFeed(FeedCfg feedCfg)
    {
      try
      {
        var feed = await FeedReader.ReadAsync(feedCfg.SourceUrl);

        if (feed?.Items != null && feed.Items.Count > 0)
          return new FeedData(feedCfg, feed);
      }
      catch (Exception ex)
      {
        LogTo.Warning(ex, $"Exception while reading feed {feedCfg.Name}");
      }

      return null;
    }

    public static Task DownloadFeedsContents(List<FeedData> feedsData)
    {
      return Task.WhenAll(feedsData.Select(DownloadFeedContent));
    }

    private static async Task DownloadFeedContent(FeedData feedData)
    {
      DateTime lastPubDate = feedData.FeedCfg.LastFeedDateTime;

      foreach (var feedItem in feedData.Feed.Items)
        try
        {
          //
          // Check & update publishing dates

          if (feedItem.PublishingDate == null)
          {
            LogTo.Warning(
              $"Date missing, or unknown format for feed {feedData.FeedCfg.Name}, item title '{feedItem.Title}', raw date '{feedItem.PublishingDateString}'");
            continue;
          }

          if (feedItem.PublishingDate <= feedData.FeedCfg.LastFeedDateTime)
            continue;

          lastPubDate = feedItem.PublishingDate > lastPubDate ? feedItem.PublishingDate.Value : lastPubDate;

          //
          // Download content or use inline content

          if (feedItem.Link != null)
            using (HttpClient client = new HttpClient())
            {
              client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

              if (string.IsNullOrWhiteSpace(feedData.FeedCfg.Cookie) == false)
                client.DefaultRequestHeaders.Add("Cookie", feedData.FeedCfg.Cookie);

              var httpReq = new HttpRequestMessage(HttpMethod.Get,
                                                   feedItem.Link);
              var httpResp = await client.SendAsync(httpReq);

              if (httpResp != null && httpResp.IsSuccessStatusCode)
              {
                feedItem.Content = await httpResp.Content.ReadAsStringAsync();
              }

              else
              {
                feedItem.Content = null;
                LogTo.Warning(
                  $"Failed to download content for feed {feedData.FeedCfg.Name}, item title '{feedItem.Title}'. HTTP Status code : {httpResp?.StatusCode}");
              }
            }

          else
            feedItem.Content = feedItem.Content ?? feedItem.Description;

          //
          // Process content if necessary & push back

          if (feedItem.Content != null)
          {
            // Optional regex
            if (string.IsNullOrWhiteSpace(feedData.FeedCfg.Regex) == false)
            {
              var filteredContents = new List<string>();
              var regex            = new Regex(feedData.FeedCfg.Regex);
              var matches          = regex.Matches(feedItem.Content);

              for (int i = 0; i < matches.Count; i++)
              {
                var match = matches[i];

                if (match.Success == false)
                  continue;

                for (int j = 1; j < match.Groups.Count; j++)
                  filteredContents.Add(match.Groups[j].Value);
              }

              feedItem.Content = string.Join("\r\n", filteredContents);
            }

            // Add feed item
            feedData.NewItems.Add(feedItem);
          }
        }
        catch (Exception ex)
        {
          LogTo.Error(ex, $"Exception while downloading content for feed {feedData.FeedCfg.Name}, item title '{feedItem.Title}'");
        }

      // Update feed published date time
      feedData.FeedCfg.LastFeedDateTime = lastPubDate;
    }

#pragma warning disable 1998
    public static async Task ImportFeeds(
#pragma warning restore 1998
      List<FeedData> feedsData,
      Action<int, int> progressCallback = null)
    {
      int i          = 0;
      int totalCount = feedsData.Sum(fd => fd.NewItems.Count);

      progressCallback?.Invoke(i, totalCount);

      foreach (var feedData in feedsData)
      foreach (var feedItem in feedData.NewItems)
        try
        {
          Svc.SMA.Registry.Element.Add(
            new ElementBuilder(ElementType.Topic,
                               new TextContent(true, feedItem.Content))
              .WithParent(feedData.FeedCfg.RootDictElement)
              .WithPriority(feedData.FeedCfg.Priority)
              .WithReference(
                // ReSharper disable once PossibleInvalidOperationException
                r => r.WithDate(feedItem.PublishingDate.Value)
                      .WithTitle(feedItem.Title)
                      .WithAuthor(feedItem.Author)
                      .WithComment(feedItem.Categories == null ? null : string.Join(", ", feedItem.Categories))
                      .WithSource($"Feed: {feedData.FeedCfg.Name} ({feedData.FeedCfg.SourceUrl})")
                      .WithLink(feedItem.Link))
              .DoNotDisplay()
          );
          
          progressCallback?.Invoke(++i, totalCount);
        }
        catch (Exception ex)
        {
          // TODO: report error through callback & display
          LogTo.Error(ex, $"Exception while importing feed item in SuperMemo, for feed {feedData.FeedCfg.Name}, item title '{feedItem.Title}'");
        }
    }

    #endregion
  }
}
