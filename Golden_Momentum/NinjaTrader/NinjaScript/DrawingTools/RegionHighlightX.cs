// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.RegionHighlightX
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Gui.Tools;
using System;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

[CLSCompliant(false)]
public class RegionHighlightX : RegionHighlightBase
{
  public virtual object Icon => (object) Icons.DrawRegionHighlightX;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected override void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static RegionHighlightX()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
