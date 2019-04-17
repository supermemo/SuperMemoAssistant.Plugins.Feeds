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
// Created On:   2019/04/10 17:23
// Modified On:  2019/04/15 19:52
// Modified By:  Alexis

#endregion




using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Anotar.Serilog;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.Plugins.Feeds.Configs;
using SuperMemoAssistant.Plugins.Feeds.Tasks;
using SuperMemoAssistant.Plugins.Feeds.UI;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.Sentry;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Sys.ComponentModel;
using SuperMemoAssistant.Sys.Remoting;

namespace SuperMemoAssistant.Plugins.Feeds
{
  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  public class FeedsPlugin : SentrySMAPluginBase<FeedsPlugin>
  {
    #region Properties & Fields - Non-Public

    private FeedsCfg       FeedsConfig  { get; set; }
    private FeedsGlobalCfg GlobalConfig { get; set; }

    #endregion




    #region Constructors

    public FeedsPlugin() { }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "Feeds";

    public override bool HasSettings => true;

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    protected override void PluginInit()
    {
      GlobalConfig = Svc.Configuration.Load<FeedsGlobalCfg>().Result ?? new FeedsGlobalCfg();

      FeedsConfig = GlobalConfig.CollectionsFeeds.SafeGet(Svc.SMA.Collection.GetKnoFilePath());

      if (FeedsConfig == null)
      {
        FeedsConfig = new FeedsCfg();
        GlobalConfig.CollectionsFeeds[Svc.SMA.Collection.GetKnoFilePath()] = FeedsConfig;
      }

      Svc.SMA.UI.ElementWindow.OnAvailable += new ActionProxy(ElementWindow_OnAvailable);

      //Svc.HotKeyManager
      //   .RegisterGlobal(
      //     "LaTeXToImage",
      //     "Convert LaTeX to Image",
      //     HotKeyScope.SMBrowser,
      //     new HotKey(Key.L, KeyModifiers.CtrlAlt),
      //     ConvertLaTeXToImage
      //   )
      //   .RegisterGlobal(
      //     "ImageToLaTeX",Class1.cs
      //     "Convert Image to LaTeX",
      //     HotKeyScope.SMBrowser,
      //     new HotKey(Key.L, KeyModifiers.CtrlAltShift),
      //     ConvertImageToLaTeX
      //   );
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      Application.Current.Dispatcher.Invoke(
        () => new ConfigurationWindow(FeedsConfig)
        {
          SaveMethod = SaveConfig
        }.ShowAndActivate()
      );
    }

    protected override Application CreateApplication()
    {
      return new FeedsApp();
    }

    #endregion




    #region Methods

    private void ElementWindow_OnAvailable()
    {
      DownloadAndImportFeeds().RunAsync();
    }

    public async Task DownloadAndImportFeeds(bool downloadInBackground = true, bool lockProtection = true)
    {
      var feedsData = await FeedTasks.DownloadFeeds(FeedsConfig);

      if (feedsData.Count == 0 || feedsData.All(fd => fd.NewItems.Count == 0))
      {
        LogTo.Debug("No new entries downloaded.");
        return;
      }

      Application.Current.Dispatcher.Invoke(
        () =>
        {
          LogTo.Debug("Creating NewContentWindow");
          new NewContentWindow(feedsData, lockProtection).ShowAndActivate();
        }
      );
    }

    public void SaveConfig()
    {
      SaveConfig(FeedsConfig);
    }

    private void SaveConfig(INotifyPropertyChangedEx config)
    {
      if (config.IsChanged)
      {
        config.IsChanged = false;

        if (config.GetType() == typeof(FeedsCfg))
          Svc.Configuration.Save(GlobalConfig, typeof(FeedsGlobalCfg)).RunAsync();

        else
          Svc.Configuration.Save(config, config.GetType()).RunAsync();
      }
    }

    #endregion
  }
}
