﻿using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Core40k;

public class DefModExtension_BoostQualityCreatedByPawn : DefModExtension
{
    public Dictionary<SkillDef, int> qualityBoostLevel;
}