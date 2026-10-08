// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.Indicators.RSI
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

#nullable disable
namespace NinjaTrader.NinjaScript.Indicators;

public class RSI : Indicator
{
  private Series<double> avgDown;
  private Series<double> avgUp;
  private double constant1;
  private double constant2;
  private double constant3;
  private Series<double> down;
  private NinjaTrader.NinjaScript.Indicators.SMA smaDown;
  private NinjaTrader.NinjaScript.Indicators.SMA smaUp;
  private Series<double> up;

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnStateChange()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected virtual void OnBarUpdate()
  {
  }

  [XmlIgnore]
  [Browsable(false)]
  public Series<double> Avg => ((NinjaScriptBase) this).Values[1];

  [XmlIgnore]
  [Browsable(false)]
  public Series<double> Default => ((NinjaScriptBase) this).Values[0];

  [Display(ResourceType = typeof (Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
  [NinjaScriptProperty]
  [Range(1, 2147483647 /*0x7FFFFFFF*/)]
  public int Period { get; set; }

  [Display(ResourceType = typeof (Resource), Name = "Smooth", GroupName = "NinjaScriptParameters", Order = 1)]
  [NinjaScriptProperty]
  [Range(1, 2147483647 /*0x7FFFFFFF*/)]
  public int Smooth { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static RSI()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
