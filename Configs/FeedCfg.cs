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
// Created On:   2019/04/10 23:58
// Modified On:  2019/04/16 17:33
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Plugins.Feeds.Models;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.UI.Configuration.ElementPicker;
using SuperMemoAssistant.Sys.ComponentModel;

namespace SuperMemoAssistant.Plugins.Feeds.Configs
{
  [Form(Mode = DefaultFields.None)]
  [Title("Feed Settings",
    IsVisible = "{Env DialogHostContext}")]
  [DialogAction("save",
    "Save",
    IsDefault = true,
    Validates = true)]
  public class FeedCfg : IElementPickerCallback, INotifyPropertyChangedEx
  {
    #region Constructors

    public FeedCfg()
    {
      LastPubDate = DateTime.MinValue;
    }

    #endregion




    #region Properties & Fields - Public

    [Field(Name = "Name")]
    [Value(Must.NotBeEmpty)]
    public string Name { get; set; }
    [Field(Name = "Source URL (RSS / Atom)")]
    [Value(Must.NotBeEmpty)]
    public string SourceUrl { get; set; }

    [Field(Name = "Use feed guid ?")]
    public bool UseGuid { get; set; } = true;
    [Field(Name = "Use feed published date ?")]
    public bool UsePubDate { get; set; } = false;

    [Field(Name = "Content link cookie")]
    public string Cookie { get; set; }
    [Field(Name = "Content link parameter")]
    public string LinkParameter { get; set; }

    [Field(Name = "Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
      0,
      StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
      100,
      StrictValidation = true)]
    public double Priority { get; set; } = 25;

    [JsonIgnore]
    [Action(ElementPicker.ElementPickerAction,
      "Browse",
      Placement = Placement.Inline)]
    [Field(Name  = "Root Element",
      IsReadOnly = true)]
    public string ElementField
    {
      // ReSharper disable once ValueParameterNotUsed
      set
      {
        /* empty */
      }
      get => RootDictElement == null
        ? "N/A"
        : RootDictElement.ToString();
    }

    [JsonIgnore]
    [Field(Name = "Excluded/Included categories (default = exclude)")]
    [MultiLine]
    public string CategoryFiltersString
    {
      get => string.Join("\n", CategoryFilters);
      set => CategoryFilters = value.Replace("\r\n", "\n")
                                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(c => new CategoryFilter(c))
                                    .ToHashSet();
    }

    [Field(Name = "Content link regex")]
    [DirectContent]
    public FeedFilterRootViewModel FiltersRoot { get; set; } = new FeedFilterRootViewModel();


    //
    // Config only

    public int RootDictElementId { get; set; }

    public HashSet<CategoryFilter> CategoryFilters { get; set; } = new HashSet<CategoryFilter>();

    public HashSet<string> EntriesGuid { get; set; } = new HashSet<string>();

    public DateTime LastPubDate { get; set; }

    public DateTime LastRefreshDate { get; set; }


    //
    // Helpers

    [JsonIgnore]
    public ObservableCollection<FeedFilter> Filters => FiltersRoot.Children;

    [JsonIgnore]
    public IElement RootDictElement => Svc.SMA.Registry.Element[RootDictElementId <= 0 ? 1 : RootDictElementId];

    [JsonIgnore]
    public DateTime PendingRefreshDate { get; set; }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    [JsonIgnore]
    public bool IsChanged { get; set; }

    #endregion




    #region Methods Impl

    public void SetElement(IElement elem)
    {
      RootDictElementId = elem.Id;

      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElementField)));
    }

    #endregion




    #region Events

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
