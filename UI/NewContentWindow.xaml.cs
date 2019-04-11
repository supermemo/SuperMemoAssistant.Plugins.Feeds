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
// Created On:   2019/04/11 01:22
// Modified On:  2019/04/11 20:31
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Plugins.Feeds.Tasks;
using SuperMemoAssistant.Sys.Threading;
using SuperMemoAssistant.Sys.Windows.Input;

namespace SuperMemoAssistant.Plugins.Feeds.UI
{
  /// <summary>Interaction logic for NewContentWindow.xaml</summary>
  public partial class NewContentWindow : Window
  {
    #region Constructors

    public NewContentWindow(List<FeedData> feedsData, bool lockProtection)
    {
      InitializeComponent();
      FeedsData      = feedsData;
      ProtectionLock = lockProtection;
    }

    #endregion




    #region Properties & Fields - Public

    public List<FeedData> FeedsData      { get; }
    public bool           ProtectionLock { get; private set; }
    public int            Progress       { get; private set; }
    public string         ProgressText   { get; private set; }
    public ICommand       ImportCommand  => new AsyncRelayCommand(ImportFeeds);
    public ICommand       CancelCommand  => new RelayCommand(Close);

    #endregion




    #region Methods

    private void TreeView_Loaded(object sender, RoutedEventArgs e) { }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
      if (ProtectionLock)
        return;

      switch (e.Key)
      {
        case Key.Enter:
          BtnOk.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
          break;

        case Key.Escape:
          BtnCancel.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
          break;
      }
    }

    private Task ImportFeeds()
    {
      void UpdateProgress(int i, int total)
      {
        Dispatcher.Invoke(
          () =>
          {
            Progress     = (int)(i / (float)total * 100);
            ProgressText = $"{Progress}% ({i}/{total})";

            GetWindow(this)?.Activate();
          }
        );
      }

      return FeedTasks.ImportFeeds(FeedsData, UpdateProgress)
                      .ContinueWith(t => Dispatcher.Invoke(Close));
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (ProtectionLock)
        new DelayedTask(() => ProtectionLock = false)
          .Trigger(500);
    }

    #endregion
  }
}
