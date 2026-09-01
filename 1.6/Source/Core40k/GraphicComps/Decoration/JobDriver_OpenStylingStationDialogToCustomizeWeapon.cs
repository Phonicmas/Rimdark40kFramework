using Verse;

namespace Core40k;

public class JobDriver_OpenStylingStationDialogToCustomizeWeapon : JobDriver_CustomizeAtStation
{
    protected override Window MakeDialog()
    {
        return new Dialog_CustomizeWeapon(pawn);
    }
}
