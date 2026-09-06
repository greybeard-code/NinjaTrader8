// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.VerticalLine
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui.Tools;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class VerticalLine : Line
{
  public override IEnumerable<ChartAnchor> Anchors
  {
    [MethodImpl(MethodImplOptions.NoInlining)] get => (IEnumerable<ChartAnchor>) null;
  }

  public override object Icon => (object) Icons.DrawVertLineTool;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  public override bool SupportsAlerts => false;

  [MethodImpl(MethodImplOptions.NoInlining)]
  static VerticalLine()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
