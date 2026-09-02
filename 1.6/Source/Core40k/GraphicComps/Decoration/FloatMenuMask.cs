using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Core40k;

public class FloatMenuMask : FloatMenu
{
    public FloatMenuMask(List<FloatMenuOption> options) : base(options)
    {
        if (options.NullOrEmpty())
        {
            Log.Error("Created FloatMenu with no options. Closing.");
            Close();
        }
        this.options = (from op in options
            orderby op.Priority descending, op.orderInPriority descending
            select op).ToList();
        foreach (var t in options)
        {
	        t.SetSizeMode(SizeMode);
	        (t as FloatMenuOptionMask)?.SetMaskSizeMode(SizeMode);
        }
        layer = WindowLayer.Super;
        closeOnClickedOutside = true;
        doWindowBackground = false;
        drawShadow = false;
        preventCameraMotion = false;
        SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
    }

    public FloatMenuMask(List<FloatMenuOption> options, string title, bool needSelection = false) : base(options, title, needSelection)
    {
        this.title = title;
        this.needSelection = needSelection;
    }

    private string title;

	private bool needSelection;

	private Color baseColor = Color.white;

	private static readonly Vector2 InitialPositionShift = new Vector2(4f, 0f);

	public override Vector2 InitialSize => new(TotalWidth, TotalWindowHeight);

	private float MaxWindowHeight => UI.screenHeight * 0.9f;

	private float TotalWindowHeight => Mathf.Min(TotalViewHeight, MaxWindowHeight) + 1f;

	private float MaxViewHeight
	{
		get
		{
			if (UsingScrollbar)
			{
				var num = 0f;
				var num2 = 0f;
				foreach (var t in options)
				{
					var requiredHeight = t.RequiredHeight;
					if (requiredHeight > num)
					{
						num = requiredHeight;
					}
					num2 += requiredHeight + -1f;
				}
				var columnCount = ColumnCount;
				num2 += columnCount * num;
				return num2 / columnCount;
			}
			return MaxWindowHeight;
		}
	}

	private float TotalViewHeight
	{
		get
		{
			var num = 0f;
			var num2 = 0f;
			var maxViewHeight = MaxViewHeight;
			foreach (var t in options)
			{
				var requiredHeight = t.RequiredHeight;
				if (num2 + requiredHeight + -1f > maxViewHeight)
				{
					if (num2 > num)
					{
						num = num2;
					}
					num2 = requiredHeight;
				}
				else
				{
					num2 += requiredHeight + -1f;
				}
			}
			return Mathf.Max(num, num2);
		}
	}

	private float TotalWidth
	{
		get
		{
			float num = ColumnCount * ColumnWidth;
			if (UsingScrollbar)
			{
				num += 16f;
			}
			return num;
		}
	}

	private float ColumnWidth
	{
		get
		{
			var num = 70f;
			foreach (var option in options)
			{
				var requiredWidth = option.RequiredWidth;
				if (requiredWidth >= 300f)
				{
					return 300f;
				}
				if (requiredWidth > num)
				{
					num = requiredWidth;
				}
			}
			return Mathf.Round(num);
		}
	}

	private int MaxColumns => Mathf.FloorToInt((UI.screenWidth - 16f) / ColumnWidth);

	private bool UsingScrollbar => ColumnCountIfNoScrollbar > MaxColumns;

	private int ColumnCount => Mathf.Min(ColumnCountIfNoScrollbar, MaxColumns);

	private int ColumnCountIfNoScrollbar
	{
		get
		{
			if (options == null)
			{
				return 1;
			}
			Text.Font = GameFont.Small;
			var num = 1;
			var num2 = 0f;
			var maxWindowHeight = MaxWindowHeight;
			foreach (var t in options)
			{
				var requiredHeight = t.RequiredHeight;
				if (num2 + requiredHeight + -1f > maxWindowHeight)
				{
					num2 = requiredHeight;
					num++;
				}
				else
				{
					num2 += requiredHeight + -1f;
				}
			}
			return num;
		}
	}
	
	protected override void SetInitialSizeAndPosition()
	{
		var vector = UI.MousePositionOnUIInverted + InitialPositionShift;
		if (vector.x + InitialSize.x > UI.screenWidth)
		{
			vector.x = UI.screenWidth - InitialSize.x;
		}
		if (vector.y + InitialSize.y > UI.screenHeight)
		{
			vector.y = UI.screenHeight - InitialSize.y;
		}
		
		windowRect = new Rect(vector.x, vector.y, InitialSize.x, InitialSize.y);
	}

    
    public override void DoWindowContents(Rect rect)
    {
        if (needSelection && Find.Selector.SingleSelectedThing == null)
        {
            Find.WindowStack.TryRemove(this);
            return;
        }
        UpdateBaseColor();
        GUI.color = baseColor;
        Text.Font = GameFont.Small;
        var zero = Vector2.zero;
        var maxViewHeight = MaxViewHeight;
        var columnWidth = ColumnWidth;

        foreach (var floatMenuOption in options)
        {
	        if (floatMenuOption is not FloatMenuOptionMask floatMenuOptionMask)
	        {
		        continue;
	        }
	        var requiredHeight = floatMenuOptionMask.RequiredHeight;
	        if (zero.y + requiredHeight + -1f > maxViewHeight)
	        {
		        zero.y = 0f;
		        zero.x += columnWidth + -1f;
	        }
	        var rect2 = new Rect(zero.x, zero.y, columnWidth, requiredHeight);
	        var rect3 = new Rect(rect2.xMax, rect2.yMin, floatMenuOptionMask.extraPartWidth, floatMenuOptionMask.extraPartWidth);
	        zero.y += requiredHeight + -1f;
	        if (floatMenuOptionMask.DoGUI(rect2, rect3, givesColonistOrders, this))
	        {
		        Find.WindowStack.TryRemove(this);
		        break;
	        }
        }

        if (Event.current.type == EventType.MouseDown)
        {
            Event.current.Use();
            Close();
        }
        GUI.color = Color.white;
    }

    private void UpdateBaseColor()
    {
        baseColor = Color.white;
        if (!vanishIfMouseDistant)
        {
            return;
        }
        var r = new Rect(0f, 0f, TotalWidth, TotalWindowHeight).ContractedBy(-5f);
        if (!r.Contains(Event.current.mousePosition))
        {
            var num = GenUI.DistFromRect(r, Event.current.mousePosition);
            baseColor = new Color(1f, 1f, 1f, 1f - num / 95f);
            if (num > 95f)
            {
                Close(doCloseSound: false);
                Cancel();
            }
        }
    }
    
}