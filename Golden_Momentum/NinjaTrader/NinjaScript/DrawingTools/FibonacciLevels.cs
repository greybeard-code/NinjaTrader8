// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.FibonacciLevels
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class FibonacciLevels : PriceLevelContainer
{
  protected const int CursorSensitivity = 15;
  private int priceLevelOpacity;
  protected ChartAnchor editingAnchor;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciLevelsBaseAnchorLineStroke", GroupName = "NinjaScriptLines", Order = 1)]
  public Stroke AnchorLineStroke { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  [Range(0, 100)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolPriceLevelsOpacity", GroupName = "NinjaScriptGeneral")]
  public int PriceLevelOpacity
  {
    get => this.priceLevelOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  public virtual bool SupportsAlerts => true;

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new FibonacciLevels.\u003CGetAlertConditionItems\u003Ed__22(-2)
    {
      \u003C\u003E4__this = this
    };
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static FibonacciLevels()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
