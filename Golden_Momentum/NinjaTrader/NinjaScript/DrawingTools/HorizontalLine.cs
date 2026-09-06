// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.HorizontalLine
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Media;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class HorizontalLine : Line
{
  public override IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  public override object Icon => (object) Icons.DrawHorizLineTool;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  [Display(ResourceType = typeof (Resource), GroupName = "NinjaScriptGeneral", Name = "NinjaScriptDrawingToolPriceMarker", Order = 1000)]
  public bool IsPriceMarkerVisible { get; set; }

  public virtual bool GetPriceMarkersSupported() => this.IsPriceMarkerVisible;

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual Dictionary<double, Brush> GetPriceMarkers(
    ChartControl chartControl,
    ChartPanel chartPanel)
  {
    return (Dictionary<double, Brush>) null;
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static HorizontalLine()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
