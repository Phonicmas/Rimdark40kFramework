using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Core40k;

public class CustomizerTabDrawer
{
    public virtual void Setup(Pawn pawn)
    {

    }

    public virtual void DrawTab(Rect rect, Pawn pawn, ref Vector2 scrollPosition)
    {

    }

    public virtual void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {

    }

    public virtual void OnReset(Pawn pawn)
    {

    }

    //Comps this drawer edits, so the dialog can capture and commit them exactly once.
    public virtual IEnumerable<CompGraphicParent> Comps => [];
}
