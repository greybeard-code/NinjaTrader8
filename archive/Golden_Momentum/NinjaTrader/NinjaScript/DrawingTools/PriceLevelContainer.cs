// Decompiled with JetBrains decompiler
// Type: NinjaTrader.NinjaScript.DrawingTools.PriceLevelContainer
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

public abstract class PriceLevelContainer : DrawingTool
{
  [PropertyEditor("NinjaTrader.Gui.Tools.CollectionEditor")]
  [SkipOnCopyTo(true)]
  [Display(ResourceType = typeof (Resource), Name = "NinjaScriptDrawingToolsPriceLevels", Prompt = "NinjaScriptDrawingToolsPriceLevelsPrompt", GroupName = "NinjaScriptLines", Order = 99)]
  public List<PriceLevel> PriceLevels { get; set; }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public virtual void CopyTo(NinjaTrader.NinjaScript.NinjaScript ninjaScript)
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public void SetAllPriceLevelsRenderTarget()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  protected PriceLevelContainer()
  {
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  static PriceLevelContainer()
  {
    \u003CAgileDotNetRT\u003E.Initialize();
    \u003CAgileDotNetRT\u003E.PostInitialize();
  }
}
