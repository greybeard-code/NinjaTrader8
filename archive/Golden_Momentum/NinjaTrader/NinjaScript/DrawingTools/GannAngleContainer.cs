// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.GannAngleContainer
// Assembly: Golden_Momentum, Version=1.0.0.1, Culture=neutral
// MVID: 30C5374A-A9DE-4A07-AF19-85AD2A8DF058
// Assembly location: C:\Users\dcjon\Downloads\Golden_Momentum\Golden_Momentum.dll

using NinjaTrader.Custom;
using NinjaTrader.Gui;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

#nullable disable
namespace NinjaTrader.NinjaScript.DrawingTools;

public abstract class GannAngleContainer : DrawingTool
{
  [SkipOnCopyTo(true)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolsGannAngles", Prompt = "NinjaScriptDrawingToolsGannAnglesPrompt", GroupName = "NinjaScriptGeneral", Order = 99)]
  [PropertyEditor("NinjaTrader.Gui.Tools.CollectionEditor")]
  public List<GannAngle> GannAngles { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected GannAngleContainer()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static GannAngleContainer()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
