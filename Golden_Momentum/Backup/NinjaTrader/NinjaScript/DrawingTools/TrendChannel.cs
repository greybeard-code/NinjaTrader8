// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.TrendChannel
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using SharpDX;
using SharpDX.Direct2D1;
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

public class TrendChannel : PriceLevelContainer
{
  private int areaOpacity;
  private Brush areaBrush;
  private readonly DeviceBrush areaDeviceBrush;
  private const double cursorSensitivity = 15.0;
  private ChartAnchor editingAnchor;
  private PathGeometry fillMainGeometry;
  private Vector2[] fillMainFig;
  private PathGeometry fillLeftGeometry;
  private Vector2[] fillLeftFig;
  private PathGeometry fillRightGeometry;
  private Vector2[] fillRightFig;
  private bool isReadyForMovingSecondLeg;
  private bool updateEndAnc;

  public virtual object Icon => (object) Icons.DrawTrendChannel;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
  [XmlIgnore]
  public Brush AreaBrush
  {
    get => this.areaBrush;
    set => this.areaBrush = BrushExtensions.ToFrozenBrush(value);
  }

  [Browsable(false)]
  public string AreaBrushSerialize
  {
    get => Serialize.BrushToString(this.AreaBrush);
    set => this.AreaBrush = Serialize.StringToBrush(value);
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
  [Range(0, 100)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
  public bool IsExtendedLinesRight { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
  public bool IsExtendedLinesLeft { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTrendChannelTrendStroke", GroupName = "NinjaScriptLines", Order = 1)]
  public Stroke Stroke { get; set; }

  [ExcludeFromTemplate]
  [Display(Order = 10)]
  public ChartAnchor TrendEndAnchor { get; set; }

  [ExcludeFromTemplate]
  [Display(Order = 0)]
  public ChartAnchor TrendStartAnchor { get; set; }

  [ExcludeFromTemplate]
  [Display(Order = 20)]
  public ChartAnchor ParallelStartAnchor { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTrendChannelParallelStroke", GroupName = "NinjaScriptLines", Order = 2)]
  public Stroke ParallelStroke { get; set; }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new TrendChannel.\u003CGetAlertConditionItems\u003Ed__59(-2)
    {
      \u003C\u003E4__this = this
    };
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

  public virtual void OnEdited(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    DrawingTool oldinstance)
  {
    this.SetParallelLine(chartControl, false);
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
  private void SetParallelLine(ChartControl chartControl, bool initialSet)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public TrendChannel()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static TrendChannel()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
