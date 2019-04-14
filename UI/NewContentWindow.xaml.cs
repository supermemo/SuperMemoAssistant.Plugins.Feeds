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
// Modified On:  2019/04/14 00:47
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SuperMemoAssistant.Extensions;
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
      FeedsData = new ObservableCollection<FeedData>(feedsData);

      InitializeComponent();

      ProtectionLock = lockProtection;
    }

    #endregion




    #region Properties & Fields - Public

    public ObservableCollection<FeedData> FeedsData      { get; }
    public bool                           ProtectionLock { get; private set; }
    public ICommand                       ImportCommand  => new AsyncRelayCommand(ImportFeeds, () => !ProtectionLock);
    public ICommand                       CancelCommand  => new RelayCommand(Close, () => !ProtectionLock);

    #endregion




    #region Methods

    private void TreeView_Loaded(object sender, RoutedEventArgs e) { }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
      switch (e.Key)
      {
        case Key.Enter:
          BtnOk.SimulateClick();
          break;

        case Key.Escape:
          BtnCancel.SimulateClick();
          break;
      }
    }

    private Task ImportFeeds()
    {
      return FeedTasks.ImportFeeds(FeedsData)
                      .ContinueWith(t => Dispatcher.Invoke(Close));
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      if (ProtectionLock)
        new DelayedTask(() => Dispatcher.Invoke(() => ProtectionLock = false))
          .Trigger(1000);
    }

    #endregion
  }
}
