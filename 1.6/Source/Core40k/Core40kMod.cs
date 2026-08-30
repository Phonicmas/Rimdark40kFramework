using HarmonyLib;
using Verse;

namespace Core40k;

public class Core40kMod : CoreMod
{
    public static string CurrentVersion;
    
    public static Harmony harmony;
        
    private Core40kModSettings settings;
    public override ModSettings Settings => settings ??= GetSettings<Core40kModSettings>();
    public Core40kMod(ModContentPack content) : base(content)
    {
        harmony = new Harmony("Core40k.Mod");
        CurrentVersion = content.ModMetaData.ModVersion;
        
        harmony.PatchAll();

        // Optional integrations. These no-op when the mod they target is absent.
        SaveOurShip2Compat.Apply(harmony);
    }
    
    private readonly ModSettingTab_CoreMain coreMainSettings = new();
    private readonly ModSettingTab_CoreDebug coreDebugSettings = new();
    private readonly ModSettingTab_CoreCustomization coreCustomizationSettings = new();
    
    public override void InitializeTabs()
    {
        var mainTab = new TabRecord("BEWH.ModSettings.TabMain".Translate(), delegate
        {
            currentSettingTab = coreMainSettings;
        }, () => currentSettingTab == coreMainSettings);
        tabs.Add(mainTab);
        
        var customizationTab = new TabRecord("BEWH.ModSettings.TabCustomization".Translate(), delegate
        {
            currentSettingTab = coreCustomizationSettings;
        }, () => currentSettingTab == coreCustomizationSettings);
        tabs.Add(customizationTab);
        
        var debugTab = new TabRecord("BEWH.ModSettings.TabDebug".Translate(), delegate
        {
            currentSettingTab = coreDebugSettings;
        }, () => currentSettingTab == coreDebugSettings);
        tabs.Add(debugTab);


        currentSettingTab = coreMainSettings;
        base.InitializeTabs();
    }

    public override string SettingsCategory()
    {
        return "BEWH.Framework.ModSettings.ModName".Translate(CurrentVersion);
    }
}