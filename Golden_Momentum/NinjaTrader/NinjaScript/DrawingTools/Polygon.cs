// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.Polygon
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
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

public class Polygon : DrawingTool
{
  private Brush areaBrush;
  private readonly DeviceBrush areaBrushDevice;
  private int areaOpacity;
  private const double cursorSensitivity = 15.0;
  private ChartAnchor editingAnchor;
  private DateTime lastClick;

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

  [Range(0, 100)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolAreaOpacity", GroupName = "NinjaScriptGeneral", Order = 1)]
  public int AreaOpacity
  {
    get => this.areaOpacity;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  public virtual object Icon => (object) Icons.DrawPolygon;

  [ExcludeFromTemplate]
  [SkipOnCopyTo(true)]
  [Browsable(false)]
  public List<ChartAnchor> ChartAnchors { get; set; }

  [ExcludeFromTemplate]
  [SkipOnCopyTo(true)]
  [Display(Order = 0)]
  public ChartAnchor StartAnchor
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (ChartAnchor) null;
    [MethodImpl(MethodImplOptions.NoInlining)] set
    {
    }
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolTextOutlineStroke", GroupName = "NinjaScriptGeneral", Order = 2)]
  public Stroke OutlineStroke { get; set; }

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private PathGeometry CreatePolygonGeometry(
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

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new Polygon.\u003CGetAlertConditionItems\u003Ed__35(-2);
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
  private Point[] GetPolygonAnchorPoints(
    ChartControl chartControl,
    ChartScale chartScale,
    bool includeCentroid)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point[] GetCentroid(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale)
  {
    return (Point[]) null;
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
  private bool IsPointInsidePolygon(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    Point p)
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
  public virtual void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public Polygon()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static Polygon()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
