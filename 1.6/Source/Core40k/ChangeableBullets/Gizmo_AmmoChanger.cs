using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Core40k;

[StaticConstructorOnStartup]
public class Gizmo_AmmoChanger : Command
{
    private float Width => 75f;

    private Comp_AmmoChanger compAmmoChanger;

    private ThingDef tooltipFor;
    private string tooltip;
    
    public Gizmo_AmmoChanger(Comp_AmmoChanger compAmmoChanger)
    {
        this.compAmmoChanger = compAmmoChanger;
    }

    private string TooltipFor(ThingDef projectile)
    {
        if (tooltipFor != projectile)
        {
            tooltipFor = projectile;
            tooltip = "BEWH.Framework.AmmoChanger.GizmoInfo".Translate(projectile.LabelCap, AmmoDescription(projectile));
        }

        return tooltip;
    }
    
    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);

        var selectedProjectile = compAmmoChanger?.CurrentlySelectedProjectile;
        if (selectedProjectile == null)
        {
            return new GizmoResult(GizmoState.Clear, null);
        }

        TooltipHandler.TipRegion(rect, TooltipFor(selectedProjectile));
        Widgets.DrawWindowBackground(rect);
        
        var color = Color.white;
        if (Mouse.IsOver(rect))
        {
            if (!disabled)
            {
                color = GenUI.MouseoverColor;
            }
        }
        var material = parms.lowLight ? TexUI.GrayscaleGUI : null;
        GUI.color = parms.lowLight ? LowLightBgColor : color;
        GenUI.DrawTextureWithMaterial(rect, parms.shrunk ? BGTextureShrunk : BGTexture, material);
        GUI.color = Color.white;
        
        if (Widgets.ButtonInvisible(rect))
        {
            var list = new List<FloatMenuOption>();
            foreach (var availableProjectile in compAmmoChanger.AvailableProjectiles)
            {
                var menuOption = new FloatMenuOption(availableProjectile.label.CapitalizeFirst(), delegate
                {
                    var wielder = compAmmoChanger.pawn;
                    if (wielder?.jobs == null)
                    {
                        return;
                    }

                    if (wielder.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Core40kDefOf.BEWH_ChangeAmmo, compAmmoChanger.Weapon), JobTag.Misc))
                    {
                        compAmmoChanger.SetNextProjectile(availableProjectile);
                    }
                }, Widgets.PlaceholderIconTex, Color.white);
                
                if (availableProjectile == selectedProjectile)
                {
                    menuOption.Disabled = true;
                    menuOption.tooltip = "BEWH.Framework.AmmoChanger.CurrentlyEquipped".Translate();
                }
                else if (!compAmmoChanger.HasResearchForAmmo(availableProjectile, out var researchDef))
                {
                    menuOption.Disabled = true;
                    menuOption.tooltip = "BEWH.Framework.Common.MissingResearch".Translate(researchDef.label.CapitalizeFirst());
                }
                else
                {
                    menuOption.tooltip = AmmoDescription(availableProjectile);
                }
                list.Add(menuOption);
            }
            if (list.NullOrEmpty())
            {
                var menuOptionNone = new FloatMenuOption("NoneBrackets".Translate(), null);
                list.Add(menuOptionNone);
            }
            
            Find.WindowStack.Add(new FloatMenu(list));
        }
        
        Widgets.DrawTextureFitted(rect, selectedProjectile.uiIcon, 1f);
        
        //Text on bottom
        Text.Font = GameFont.Tiny;
        var num = Text.CalcHeight(selectedProjectile.LabelCap, rect.width + 0.1f);
        var rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
        GUI.DrawTexture(rect3, TexUI.GrayTextBG);
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(rect3, selectedProjectile.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        
        return new GizmoResult(GizmoState.Clear, null);
    }
    
    public override float GetWidth(float maxWidth)
    {
        return Width;
    }

    private static string AmmoDescription(ThingDef projectile)
    {
        var description = projectile.description.CapitalizeFirst();
        var statSummary = projectile.GetModExtension<DefModExtension_AmmoChanger>()?.StatSummary();

        return statSummary.NullOrEmpty() ? description : description + "\n\n" + statSummary;
    }
}