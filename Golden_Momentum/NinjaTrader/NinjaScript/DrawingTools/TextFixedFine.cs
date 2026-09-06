// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.TextFixedFine
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class TextFixedFine : TextFixedBase
{
  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override Point GetTextDrawingPosition(
    ChartControl chartControl,
    ChartPanel chartPanel,
    ChartScale chartScale)
  {
    return new Point();
  }

  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptTextPosition", GroupName = "NinjaScriptIndicatorVisualGroup", Order = 70)]
  public TextPositionFine TextPositionFine { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static TextFixedFine()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
