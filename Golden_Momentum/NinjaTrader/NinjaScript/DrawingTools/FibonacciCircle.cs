// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.FibonacciCircle
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter("NinjaTrader.NinjaScript.DrawingTools.FibonacciCircleTimeTypeConverter")]
public class FibonacciCircle : FibonacciRetracements
{
  public override object Icon => (object) Icons.DrawFbCircle;

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciTimeExtensionsShowText", GroupName = "NinjaScriptGeneral")]
  public bool IsTextDisplayed { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolFibonacciTimeCircleDivideTimeSeparately", GroupName = "NinjaScriptGeneral", Order = 1)]
  public bool IsTimePriceDividedSeparately { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private void DrawPriceLevelText(
    float textX,
    float textY,
    PriceLevel priceLevel,
    double yVal,
    ChartControl chartControl)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  private string GetPriceString(double yVal, PriceLevel priceLevel) => (string) null;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual IEnumerable<Condition> GetValidAlertConditions() => (IEnumerable<Condition>) null;

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
  public override bool IsVisibleOnChart(
    ChartControl chartControl,
    ChartScale chartScale,
    DateTime firstTimeOnChart,
    DateTime lastTimeOnChart)
  {
    return false;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnRender(ChartControl chartControl, ChartScale chartScale)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static FibonacciCircle()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
