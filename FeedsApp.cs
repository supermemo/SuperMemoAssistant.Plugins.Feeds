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
// Created On:   2019/04/10 17:45
// Modified On:  2019/04/10 21:10
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Windows;
using SuperMemoAssistant.Interop.Plugins;

namespace SuperMemoAssistant.Plugins.Feeds
{
  internal class FeedsApp : PluginApp
  {
    #region Constants & Statics

    public static readonly List<string> ResourceDictionaries = new List<string>
    {
      "pack://application:,,,/SuperMemoAssistant.Plugins.Feeds;component/UI/FeedsDataTemplate.xaml",
      "pack://application:,,,/SuperMemoAssistant.Plugins.Feeds;component/UI/FeedFiltersDataTemplate.xaml",
    };

    #endregion




    #region Constructors

    public FeedsApp()
    {
      Startup += FeedsApp_Startup;
    }

    #endregion




    #region Methods

    private void FeedsApp_Startup(object sender, StartupEventArgs e)
    {
      foreach (var resDictSrc in ResourceDictionaries)
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
          Source = new Uri(resDictSrc,
                           UriKind.RelativeOrAbsolute)
        });
    }

    #endregion
  }
}
