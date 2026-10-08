// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.TextFixedBase
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui.Chart;
using System;
using System.Runtime.CompilerServices;
using System.Windows;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class TextFixedBase : Text
{
  [MethodImpl(MethodImplOptions.NoInlining)]
  public override void OnCalculateMinMax()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected int PaddingMultiplier(ChartControl chartControl, ChartPanel panel, bool top) => 0;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override Rect GetCurrentRect(Rect layoutRect, double outlinePadding) => new Rect();

  public override bool IsVisibleOnChart(
    ChartControl chartControl,
    ChartScale chartScale,
    DateTime firstTimeOnChart,
    DateTime lastTimeOnChart)
  {
    return true;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static TextFixedBase()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
