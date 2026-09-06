// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.GannAngleTypeConverter
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public class GannAngleTypeConverter : TypeConverter
{
  [MethodImpl(MethodImplOptions.NoInlining)]
  public override PropertyDescriptorCollection GetProperties(
    ITypeDescriptorContext context,
    object component,
    Attribute[] attrs)
  {
    return (PropertyDescriptorCollection) null;
  }

  public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;

  [MethodImpl(MethodImplOptions.NoInlining)]
  static GannAngleTypeConverter()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
