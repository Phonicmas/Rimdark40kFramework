namespace Core40k;

public class WeaponDecorationTab : DecorationBaseTab
{
    protected override bool OnlyEditDefaultDrawData => true;
    
    protected override void SetupHook()
    {
        decorativeComps.Add(selPawn.equipment.Primary.GetComp<CompDecorativeBase>());
    }
}