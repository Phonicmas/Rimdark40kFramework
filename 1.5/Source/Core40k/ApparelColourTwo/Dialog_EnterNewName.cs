﻿using UnityEngine;
using Verse;

namespace Core40k
{
    public class Dialog_EnterNewName : Window
    {
        public override Vector2 InitialSize => new Vector2(300f, 120f);

        private GameComponent_SavedPresets gameComp;

        private ColourPreset colourPreset;

        private string textEntry = "";

        private bool showWarningText = false;
        
        public Dialog_EnterNewName(GameComponent_SavedPresets gameComp, ColourPreset colourPreset)
        {
            this.gameComp = gameComp;
            this.colourPreset = colourPreset;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            var viewRect = inRect.ContractedBy(5f);

            var labelRect = new Rect(viewRect)
            {
                height = viewRect.height * 0.3f
            };
            Widgets.Label(labelRect, "BEWH.Framework.ApparelColourTwo.EnterPresetName".Translate());
            
            var textEntryRect = new Rect(labelRect)
            {
                yMin = labelRect.yMax,
                height = labelRect.height,
            };
            var newName = Widgets.TextArea(textEntryRect, textEntry);
            textEntry = newName;
            
            var acceptRect = new Rect(textEntryRect)
            {
                yMin = textEntryRect.yMax + 5f,
                height = textEntryRect.height,
                width = textEntryRect.width / 4,
            };
            if (Widgets.ButtonText(acceptRect, "Accept".Translate()))
            {
                colourPreset.name = newName;
                if (gameComp.AddPreset(colourPreset))
                {
                    Close();
                }

                showWarningText = true;
            }

            if (showWarningText)
            {
                var warningRect = new Rect(acceptRect)
                {
                    xMin = acceptRect.xMax + viewRect.width / 50f,
                    width = textEntryRect.width * 0.46f,
                };
                
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(warningRect, "BEWH.Framework.ApparelColourTwo.PresetExists".Translate().Colorize(Color.red));
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
            }
            
            var closeRect = new Rect(acceptRect)
            {
                xMin = viewRect.xMax - acceptRect.width,
                width = acceptRect.width,
            };
            if (Widgets.ButtonText(closeRect, "Close".Translate()))
            {
                Close();
            }
        }
    }
}