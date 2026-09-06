// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.ShapeBase
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
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

public abstract class ShapeBase : DrawingTool
{
  private int areaOpacity;
  private Brush areaBrush;
  private readonly DeviceBrush areaBrushDevice;
  private const double cursorSensitivity = 15.0;
  private ChartAnchor editingAnchor;
  private ChartAnchor editingLeftAnchor;
  private ChartAnchor editingTopAnchor;
  private ChartAnchor editingBottomAnchor;
  private ChartAnchor editingRightAnchor;
  private ChartAnchor lastMouseMoveDataPoint;
  private ShapeBase.ResizeMode resizeMode;

  [XmlIgnore]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolShapesAreaBrush", GroupName = "NinjaScriptGeneral", Order = 1)]
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
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 2)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 3)]
  public Stroke OutlineStroke { get; set; }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  [Display(Order = 3)]
  public ChartAnchor MiddleAnchor { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  [Browsable(false)]
  protected ShapeBase.ChartShapeType ShapeType { get; set; }

  public virtual bool SupportsAlerts => true;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private PathGeometry CreateTriangleGeometry(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    double pixelAdjust)
  {
    return (PathGeometry) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void Dispose(bool disposing)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Rect GetAnchorsRect(ChartControl chartControl, ChartScale chartScale) => new Rect();

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
  private static Point? GetClosestPoint(
    IEnumerable<Point> inputPoints,
    Point desired,
    bool useSensitivity)
  {
    return new Point?();
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point[] GetEllipseAnchorPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private ShapeBase.ResizeMode GetResizeModeForPoint(
    Point pt,
    ChartControl chartControl,
    ChartScale chartScale,
    bool useCursorSens)
  {
    return new ShapeBase.ResizeMode();
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point[] GetTriangleAnchorPoints(
    ChartControl chartControl,
    ChartScale chartScale,
    bool includeCentroid)
  {
    return (Point[]) null;
  }

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new ShapeBase.\u003CGetAlertConditionItems\u003Ed__56(-2);
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
  protected ShapeBase()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static ShapeBase()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  protected enum ChartShapeType
  {
    Unset,
    Ellipse,
    Rectangle,
    Triangle,
  }

  protected enum ResizeMode
  {
    None,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    MoveAll,
  }
}
