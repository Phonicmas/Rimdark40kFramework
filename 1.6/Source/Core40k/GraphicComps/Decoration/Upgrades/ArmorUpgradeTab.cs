namespace Core40k;

//Same grid, search, colouring and preset machinery as the armour Decoration tab; it just shows the
//other half of the content. See DecorationDef.IsUpgrade.
public class ArmorUpgradeTab : ArmorDecorationTab
{
    protected override bool ShowUpgrades => true;
}
