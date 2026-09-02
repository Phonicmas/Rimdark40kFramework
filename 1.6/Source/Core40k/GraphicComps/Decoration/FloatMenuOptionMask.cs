using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Core40k;

public class FloatMenuOptionMask : FloatMenuOption
{
    public FloatMenuOptionMask(string label, Action action, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) : base(label, action, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
    {
        Label = label;
        this.action = action;
        priorityInt = priority;
        this.revalidateClickTarget = revalidateClickTarget;
        this.mouseoverGuiAction = mouseoverGuiAction;
        this.extraPartWidth = extraPartWidth;
        this.extraPartOnGUI = extraPartOnGUI;
        this.revalidateWorldClickTarget = revalidateWorldClickTarget;
        this.playSelectionSound = playSelectionSound;
        this.orderInPriority = orderInPriority;
    }

    public FloatMenuOptionMask(string label, Action action, ThingDef shownItemForIcon, ThingStyleDef thingStyle = null, bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null) : base(label, action, shownItemForIcon, thingStyle, forceBasicStyle, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride)
    {
        shownItem = shownItemForIcon;
        this.thingStyle = thingStyle;
        this.forceBasicStyle = forceBasicStyle;
        this.graphicIndexOverride = graphicIndexOverride;
        if (shownItemForIcon == null)
        {
            drawPlaceHolderIcon = true;
        }
    }

    public FloatMenuOptionMask(string label, Action action, ThingDef shownItemForIcon, Texture2D iconTex, ThingStyleDef thingStyle = null, bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null) : base(label, action, shownItemForIcon, iconTex, thingStyle, forceBasicStyle, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, graphicIndexOverride)
    {
        this.iconTex = iconTex;
        shownItem = shownItemForIcon;
        this.thingStyle = thingStyle;
        this.forceBasicStyle = forceBasicStyle;
        this.graphicIndexOverride = graphicIndexOverride;
        if (shownItemForIcon == null && iconTex == null)
        {
            drawPlaceHolderIcon = true;
        }
    }

    public FloatMenuOptionMask(string label, Action action, Texture2D iconTex, Color iconColor, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0, HorizontalJustification iconJustification = HorizontalJustification.Left, bool extraPartRightJustified = false) : base(label, action, iconTex, iconColor, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority, iconJustification, extraPartRightJustified)
    {
        this.iconTex = iconTex;
        this.iconColor = iconColor;
        this.iconJustification = iconJustification;
        this.extraPartRightJustified = extraPartRightJustified;
    }

    public FloatMenuOptionMask(string label, Action action, Thing iconThing, Color iconColor, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, bool playSelectionSound = true, int orderInPriority = 0) : base(label, action, iconThing, iconColor, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget, playSelectionSound, orderInPriority)
    {
        this.iconThing = iconThing;
        this.iconColor = iconColor;
    }
    
    private MenuOptionPriority priorityInt = MenuOptionPriority.Default;
    
    private bool playSelectionSound = true;
    
    private Texture2D iconTex;
    private ThingDef shownItem;
    
    private bool drawPlaceHolderIcon;
    
    private HorizontalJustification iconJustification;
    
    private FloatMenuSizeMode sizeMode;
	
    public void SetMaskSizeMode(FloatMenuSizeMode mode)
    {
        sizeMode = mode;
    }
    
    private GameFont CurrentFont
    {
	    get
	    {
		    if (sizeMode != FloatMenuSizeMode.Normal)
		    {
			    return GameFont.Tiny;
		    }
		    return GameFont.Small;
	    }
    }
    
    private float CurIconSize
    {
	    get
	    {
		    if (sizeMode != FloatMenuSizeMode.Tiny)
		    {
			    return 27f;
		    }
		    return 16f;
	    }
    }
    
    private float HorizontalMargin
    {
	    get
	    {
		    if (sizeMode != FloatMenuSizeMode.Normal)
		    {
			    return 3f;
		    }
		    return 6f;
	    }
    }

    private float IconOffset
    {
	    get
	    {
		    if (shownItem == null && !drawPlaceHolderIcon && !(iconTex != null) && iconThing == null)
		    {
			    return 0f;
		    }
		    return CurIconSize;
	    }
    }

    public bool DoGUI(Rect rect, Rect rectExtra, bool colonistOrdering, FloatMenu floatMenu)
    {
	    var rect2 = rect;
		rect2.height--;
		var flag = !Disabled && Mouse.IsOver(rect2);
		var flag2 = false;
		Text.Font = CurrentFont;
		if (tooltip.HasValue)
		{
			TooltipHandler.TipRegion(rect, tooltip.Value);
		}
		var rect3 = rect;
		if (iconJustification == HorizontalJustification.Left)
		{
			rect3.xMin += 4f;
			rect3.xMax = rect.x + CurIconSize;
			rect3.yMin += 4f;
			rect3.yMax = rect.y + CurIconSize;
			if (flag)
			{
				rect3.x += 4f;
			}
		}
		var rect4 = rect;
		rect4.xMin += HorizontalMargin;
		rect4.xMax -= HorizontalMargin;
		rect4.xMax -= 4f;
		rect4.xMax -= extraPartWidth + IconOffset;
		if (iconJustification == HorizontalJustification.Left)
		{
			rect4.x += IconOffset;
		}
		if (flag)
		{
			rect4.x += 4f;
		}
		var num = Mathf.Min(Text.CalcSize(Label).x, rect4.width - 4f);
		var num2 = rect4.xMin + num;
		if (iconJustification == HorizontalJustification.Right)
		{
			rect3.x = num2 + 4f;
			rect3.width = CurIconSize;
			rect3.yMin += 4f;
			rect3.yMax = rect.y + CurIconSize;
			num2 += CurIconSize;
		}
		var rect5 = default(Rect);
		if (extraPartWidth != 0f)
		{
			if (extraPartRightJustified)
			{
				num2 = rect.xMax;
			}
			rect5 = new Rect(num2, rect4.yMin, extraPartWidth, extraPartWidth);
			flag2 = Mouse.IsOver(rect5);
		}
		if (!Disabled)
		{
			MouseoverSounds.DoRegion(rect2);
		}
		var color = GUI.color;
		if (Disabled)
		{
			GUI.color = ColorBGDisabled * color;
		}
		else if (flag && !flag2)
		{
			GUI.color = ColorBGActiveMouseover * color;
		}
		else
		{
			GUI.color = ColorBGActive * color;
		}
		GUI.DrawTexture(rect, BaseContent.WhiteTex);
		GUI.color = ((!Disabled) ? ColorTextActive : ColorTextDisabled) * color;
		if (sizeMode == FloatMenuSizeMode.Tiny)
		{
			rect4.y += 1f;
		}
		Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
		Text.Anchor = TextAnchor.MiddleLeft;
		Widgets.Label(rect4, Label);
		Text.Anchor = TextAnchor.UpperLeft;
		GUI.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * GUI.color.a);
		var thingStyleDef = thingStyle ?? ((shownItem == null || Find.World == null) ? null : Faction.OfPlayer.ideos?.PrimaryIdeo?.GetStyleFor(shownItem));
		if (shownItem != null || drawPlaceHolderIcon)
		{
			if (forceBasicStyle)
			{
				thingStyleDef = null;
			}
			var value = forceThingColor ?? (shownItem == null ? Color.white : shownItem.MadeFromStuff ? shownItem.GetColorForStuff(GenStuff.DefaultStuffFor(shownItem)) : shownItem.uiIconColor);
			value.a *= color.a;
			Widgets.DefIcon(rect3, shownItem, null, 1f, thingStyleDef, drawPlaceHolderIcon, value, null, graphicIndexOverride);
		}
		else if ((bool)iconTex)
		{
			Widgets.DrawTextureFitted(rect3, iconTex, 1f, new Vector2(1f, 1f), iconTexCoords);
		}
		else if (iconThing != null)
		{
			Widgets.ThingIcon(rect3, iconThing, color.a);
		}
		GUI.color = color;
		if (extraPartOnGUI != null)
		{
			GUI.DrawTexture(rectExtra, BaseContent.WhiteTex);
			GUI.color = ((!Disabled) ? ColorTextActive : ColorTextDisabled) * color;
			Widgets.DrawAtlas(rectExtra, TexUI.FloatMenuOptionBG);
			GUI.color = new Color(iconColor.r, iconColor.g, iconColor.b, iconColor.a * GUI.color.a);
			var num3 = extraPartOnGUI(rectExtra);
			GUI.color = color;
			if (num3)
			{
				return true;
			}
		}
		if (flag && mouseoverGuiAction != null)
		{
			mouseoverGuiAction(rect5);
		}
		if (tutorTag != null)
		{
			UIHighlighter.HighlightOpportunity(rect, tutorTag);
		}
		if (Widgets.ButtonInvisible(rect2))
		{
			if (tutorTag != null && !TutorSystem.AllowAction(tutorTag))
			{
				return false;
			}
			Chosen(colonistOrdering, floatMenu);
			if (tutorTag != null)
			{
				TutorSystem.Notify_Event(tutorTag);
			}
			return true;
		}
		return false;
    }
}