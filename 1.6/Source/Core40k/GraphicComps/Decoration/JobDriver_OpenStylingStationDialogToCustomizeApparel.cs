using Verse;

namespace Core40k;

public class JobDriver_OpenStylingStationDialogToCustomizeApparel : JobDriver_CustomizeAtStation
{
    protected override Window MakeDialog()
    {
        return new Dialog_CustomizeApparel(pawn);
    }
}
