// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.Indicators.SMA
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.Indicators;

public class SMA : Indicator
{
  private double priorSum;
  private double sum;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnBarUpdate()
  {
  }

  [Display(ResourceType = typeof (Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
  [NinjaScriptProperty]
  [Range(1, 2147483647 /*0x7FFFFFFF*/)]
  public int Period { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static SMA()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
