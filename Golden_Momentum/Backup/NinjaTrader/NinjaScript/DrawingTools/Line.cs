// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.Line
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
using System.Xml.Serialization;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class Line : DrawingTool
{
  [CLSCompliant(false)]
  protected PathGeometry ArrowPathGeometry;
  private ChartAnchor cursorAnchor;
  private const double cursorSensitivity = 15.0;
  private ChartAnchor editingAnchor;

  public virtual IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [Display(Order = 2)]
  public ChartAnchor EndAnchor { get; set; }

  [Display(Order = 1)]
  public ChartAnchor StartAnchor { get; set; }

  public virtual object Icon => (object) Icons.DrawLineTool;

  [XmlIgnore]
  [Browsable(false)]
  protected Line.ChartLineType LineType { get; set; }

  [Display(ResourceType = typeof (Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolLine", Order = 99)]
  public Stroke Stroke { get; set; }

  public virtual bool SupportsAlerts => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private ChartAnchor Anchor45(
    ChartAnchor startAnchor,
    ChartAnchor endAnchor,
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale)
  {
    return (ChartAnchor) null;
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

  public virtual IEnumerable<AlertConditionItem> GetAlertConditionItems()
  {
    // ISSUE: object of a compiler-generated type is created
    return (IEnumerable<AlertConditionItem>) new Line.\u003CGetAlertConditionItems\u003Ed__29(-2);
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

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnKeyDown(ChartControl chartControl, ChartPanel chartPanel, KeyEventArgs e)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void OnKeyUp(ChartControl chartControl, ChartPanel chartPanel, KeyEventArgs e)
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
  static Line()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }

  protected enum ChartLineType
  {
    ArrowLine,
    ExtendedLine,
    HorizontalLine,
    Line,
    Ray,
    VerticalLine,
  }
}
