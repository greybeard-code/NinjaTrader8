// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.TextPositionFine
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using System.ComponentModel;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

[TypeConverter("NinjaTrader.Custom.ResourceEnumConverter")]
public enum TextPositionFine
{
  BottomLeft,
  BottomMiddle,
  BottomRight,
  MiddleLeft,
  MiddleRight,
  TopLeft,
  TopMiddle,
  TopRight,
}
