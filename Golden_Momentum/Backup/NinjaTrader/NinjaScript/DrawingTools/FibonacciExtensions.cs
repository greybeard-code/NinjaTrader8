// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.FibonacciExtensions
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class FibonacciExtensions : FibonacciRetracements
{
  private Point anchorExtensionPoint;

  [Display(Order = 3)]
  public ChartAnchor ExtensionAnchor { get; set; }

  public override IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected new Tuple<Point, Point> GetPriceLevelLinePoints(
    PriceLevel priceLevel,
    ChartControl chartControl,
    ChartScale chartScale,
    bool isInverted)
  {
    return (Tuple<Point, Point>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private new void DrawPriceLevelText(
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
  public override Cursor GetCursor(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    Point point)
  {
    return (Cursor) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Point GetEndLineMidpoint(ChartControl chartControl, ChartScale chartScale) => new Point();

  [MethodImpl(MethodImplOptions.NoInlining)]
  public sealed override Point[] GetSelectionPoints(
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return (Point[]) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private string GetPriceString(double price, PriceLevel priceLevel, ChartPanel _) => (string) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  private Tuple<Point, Point> GetTranslatedExtensionYLine(
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return (Tuple<Point, Point>) null;
  }

  public override object Icon => (object) Icons.DrawFbExtensions;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override bool IsAlertConditionTrue(
    AlertConditionItem conditionItem,
    Condition condition,
    ChartAlertValue[] values,
    ChartControl chartControl,
    ChartScale chartScale)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnMouseDown(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnMouseMove(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale,
    ChartAnchor dataPoint)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static FibonacciExtensions()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
