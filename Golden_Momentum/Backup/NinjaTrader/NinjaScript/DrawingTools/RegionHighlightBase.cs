// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.RegionHighlightBase
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

[CLSCompliant(false)]
public abstract class RegionHighlightBase : DrawingTool
{
  private int areaOpacity;
  private Brush areaBrush;
  private readonly DeviceBrush areaBrushDevice;
  private const double cursorSensitivity = 15.0;
  private ChartAnchor editingAnchor;
  private bool hasSetZOrder;

  public virtual bool SupportsAlerts => true;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolRiskRewardAnchorLineStroke", GroupName = "NinjaScriptGeneral", Order = 5)]
  public Stroke AnchorLineStroke { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 3)]
  [XmlIgnore]
  public Brush AreaBrush
  {
    get => this.areaBrush;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Browsable(false)]
  public string AreaBrushSerialize
  {
    get => Serialize.BrushToString(this.AreaBrush);
    set => this.AreaBrush = Serialize.StringToBrush(value);
  }

  [Range(0, 100)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 4)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  [Browsable(false)]
  [XmlIgnore]
  internal RegionHighlightMode Mode { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 6)]
  public Stroke OutlineStroke { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
  {
  }

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new RegionHighlightBase.\u003CGetAlertConditionItems\u003Ed__40(-2);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Cursor GetCursor(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    Point point)
  {
    return (Cursor) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual IEnumerable<Condition> GetValidAlertConditions() => (IEnumerable<Condition>) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual bool IsAlertConditionTrue(
    AlertConditionItem conditionItem,
    Condition condition,
    ChartAlertValue[] values,
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual bool IsVisibleOnChart(
    ChartControl chartControl,
    ChartScale chartScale,
    DateTime firstTimeOnChart,
    DateTime lastTimeOnChart)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseDown(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseMove(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnMouseUp(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected RegionHighlightBase()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static RegionHighlightBase()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
