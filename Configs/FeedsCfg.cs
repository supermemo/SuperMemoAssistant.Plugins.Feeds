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
// Created On:   2019/04/10 18:04
// Modified On:  2019/04/10 23:03
// Modified By:  Alexis

#endregion




using System.ComponentModel;
using System.Linq;
using Forge.Forms;
using Forge.Forms.Annotations;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.ComponentModel;

namespace SuperMemoAssistant.Plugins.Feeds.Configs
{
  [Form(Mode = DefaultFields.None)]
  [Action("runNow", "Run now", Placement = Placement.Before)]
  public class FeedsCfg : INotifyPropertyChangedEx, IActionHandler
  {
    #region Properties & Fields - Non-Public

    private FeedList _feeds;
    private bool     _isChanged;

    #endregion




    #region Constructors

    public FeedsCfg()
    {
      Feeds = new FeedList();
    }

    #endregion




    #region Properties & Fields - Public

    [Field]
    [DirectContent]
    public FeedList Feeds
    {
      get => _feeds;
      set
      {
        _feeds                   =  value;
        _feeds.CollectionChanged += Feeds_CollectionChanged;
      }
    }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public bool IsChanged { get => _isChanged || Feeds.Any(f => f.IsChanged); set => _isChanged = value; }

    #endregion




    #region Methods Impl

    public override string ToString()
    {
      return "Feeds";
    }

    #endregion




    #region Methods
    public void HandleAction(IActionContext actionContext)
    {
      switch (actionContext.Action as string)
      {
        case "runNow":
          Svc<FeedsPlugin>.Plugin.DownloadAndImportFeeds(false, false).RunAsync();
          break;
      }
    }

    private void Feeds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      IsChanged = true;
    }

    #endregion




    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
