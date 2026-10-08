// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.TimeCycles
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NTRes.NinjaTrader.Gui.Chart;
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

public class TimeCycles : DrawingTool
{
  private Brush areaBrush;
  private readonly DeviceBrush areaBrushDevice;
  private int areaOpacity;
  private List<int> anchorBars;
  private const int cursorSensitivity = 15;
  private int diameter;
  private int radius;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 0)]
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

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 1)]
  [Range(0, 100)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  public virtual object Icon => (object) Icons.DrawTimeCycles;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 2)]
  public Stroke OutlineStroke { get; set; }

  [Browsable(false)]
  public ChartAnchor StartAnchor { get; set; }

  [Browsable(false)]
  public ChartAnchor EndAnchor { get; set; }

  [Display(ResourceType = typeof (ChartResources), GroupName = "GuiChartsCategoryData", Name = "GuiChartsChartAnchorStartTime", Order = 0)]
  [PropertyEditor("NinjaTrader.Gui.Tools.ChartAnchorTimeEditor")]
  public DateTime StartTime
  {
    get => this.StartAnchor.Time;
    set => this.StartAnchor.Time = value;
  }

  [Display(ResourceType = typeof (ChartResources), GroupName = "GuiChartsCategoryData", Name = "GuiChartsChartAnchorEndTime", Order = 1)]
  [PropertyEditor("NinjaTrader.Gui.Tools.ChartAnchorTimeEditor")]
  public DateTime EndTime
  {
    get => this.EndAnchor.Time;
    set => this.EndAnchor.Time = value;
  }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
  {
  }

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new TimeCycles.\u003CGetAlertConditionItems\u003Ed__41(-2);
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private ChartBars GetChartBars() => (ChartBars) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private int GetClosestBarAnchor(ChartControl chartControl, Point p, bool ignoreHitTest) => 0;

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
  public virtual IEnumerable<Condition> GetValidAlertConditions() => (IEnumerable<Condition>) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

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
  private bool IsPointInsideTimeCycles(ChartPanel chartPanel, Point p) => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private bool IsPointOnTimeCyclesOutline(
    ChartControl chartControl,
    ChartPanel chartPanel,
    Point p)
  {
    return false;
  }

  public virtual bool IsVisibleOnChart(
    ChartControl chartControl,
    ChartScale chartScale,
    DateTime firstTimeOnChart,
    DateTime lastTimeOnChart)
  {
    return true;
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
  public virtual void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void UpdateAnchors(ChartControl chartControl, int startX)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public TimeCycles()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static TimeCycles()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
