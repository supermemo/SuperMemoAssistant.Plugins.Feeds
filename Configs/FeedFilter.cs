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
// Created On:   2019/04/13 16:28
// Modified On:  2019/04/17 14:04
// Modified By:  Alexis

#endregion




using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Forge.Forms;
using Forge.Forms.Annotations;
using Forge.Forms.Validation;
using Newtonsoft.Json;
using SuperMemoAssistant.Plugins.Feeds.Extensions;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Sys.Windows.Input;

namespace SuperMemoAssistant.Plugins.Feeds.Configs
{
  [Form(Mode = DefaultFields.None)]
  [Title("Filter Settings",
    IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
    "Cancel",
    IsCancel = true)]
  [DialogAction("save",
    "Save",
    IsDefault = true,
    Validates = true)]
  public class FeedFilter : FeedFilterBase, INotifyPropertyChanged
  {
    #region Properties & Fields - Non-Public

    private string _filterError = null;

    #endregion




    #region Constructors

    /// <inheritdoc />
    public FeedFilter() { }

    #endregion




    #region Properties & Fields - Public

    [Field(Name = "Filter type")]
    [SelectFrom(typeof(FeedFilterType),
      SelectionType = SelectionType.RadioButtonsInline)]
    public FeedFilterType Type { get; set; } = FeedFilterType.XPath;

    [Field(Name = "Filter")]
    [Value(Must.NotBeEmpty)]
    [Value(Must.SatisfyMethod, nameof(Validate), Message = "{Binding FilterError}")]
    public string Filter { get; set; }

    [JsonIgnore]
    public ICommand EditCommand => new AsyncRelayCommand(EditFilter);

    [JsonIgnore]
    public string FilterError => _filterError ?? "Unknown error";

    #endregion




    #region Methods

    public static bool Validate(ValidationContext validationContext)
    {
      var feedFilter = (FeedFilter)validationContext.Model;

      switch (validationContext.PropertyName)
      {
        case nameof(Filter):
          var ret = feedFilter.ValidateFilter(out feedFilter._filterError);
          feedFilter.PropertyChanged?.Invoke(feedFilter, new PropertyChangedEventArgs(nameof(FilterError)));

          return ret;
      }

      return false;
    }

    private Task EditFilter()
    {
      return Show.Window().For<FeedFilter>(this);
    }

    #endregion




    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
