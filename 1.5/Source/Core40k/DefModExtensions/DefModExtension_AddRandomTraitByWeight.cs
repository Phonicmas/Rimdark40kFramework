﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DefModExtension_AddRandomTraitByWeight : DefModExtension
{
    public Dictionary<Dictionary<TraitDef, int>, float> possibleTraitsToGive = new Dictionary<Dictionary<TraitDef, int>, float>();

    public int chanceToGrantTrait = 100;
}