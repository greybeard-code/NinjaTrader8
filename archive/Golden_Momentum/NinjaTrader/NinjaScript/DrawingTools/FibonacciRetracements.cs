// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.FibonacciRetracements
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class FibonacciRetracements : FibonacciLevels
{
  public virtual object Icon => (object) Icons.DrawFbRetracement;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesRight", GroupName = "NinjaScriptLines")]
  public bool IsExtendedLinesRight { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsExtendLinesLeft", GroupName = "NinjaScriptLines")]
  public bool IsExtendedLinesLeft { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciRetracementsTextLocation", GroupName = "NinjaScriptGeneral")]
  public TextLocation TextLocation { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected bool CheckAlertRetracementLine(
    Condition condition,
    Point lineStartPoint,
    Point lineEndPoint,
    ChartControl chartControl,
    ChartScale chartScale,
    ChartAlertValue[] values)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected void DrawPriceLevelText(
    ChartPanel chartPanel,
    ChartScale _,
    double minX,
    double maxX,
    double y,
    double price,
    PriceLevel priceLevel)
  {
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
  protected Tuple<Point, Point> GetPriceLevelLinePoints(
    PriceLevel priceLevel,
    ChartControl chartControl,
    ChartScale chartScale,
    bool isInverted)
  {
    return (Tuple<Point, Point>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private string GetPriceString(double price, PriceLevel priceLevel) => (string) null;

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
  static FibonacciRetracements()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
