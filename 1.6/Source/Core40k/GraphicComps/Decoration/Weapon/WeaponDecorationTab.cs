namespace Core40k;

public class WeaponDecorationTab : DecorationBaseTab
{
    protected override bool OnlyEditDefaultDrawData => true;

    protected override void SetupHook()
    {
        var decorativeComp = selPawn?.equipment?.Primary?.GetComp<CompDecorativeBase>();
        if (decorativeComp != null)
        {
            decorativeComps.Add(decorativeComp);
        }
    }
}
