using BepInEx.Configuration;

namespace VoidSaving
{
    internal class Config
    {
        internal static void Load(ConfigFile configFile)
        {
            SavesLocation = configFile.Bind("Settings", "SavesLocation", string.Empty);
            LastSave = configFile.Bind("Settings", "LastSave", string.Empty);
            AutoSavingEnabled = configFile.Bind("Settings", "AutoSaving", true, "Auto Saving Enabled/Disabled");
            AutoSaveLimit = configFile.Bind("Settings", "AutoSaveLimit", 10, "How many auto save IDs to loop through");
            DefaultIronMan = configFile.Bind("Settings", "DefaultIronManMode", true, "Default state for iron man setting.");
            ExtraMSUntilInterdiction = configFile.Bind("Settings", "ExtraMSUntilInterdiction", 10000, "Time in MS added to interdictions during load.");
            LastAutoSave = configFile.Bind("Data", "LastAutoSave", 0);
        }

        internal static ConfigEntry<string> SavesLocation;
        internal static ConfigEntry<string> LastSave;
        internal static ConfigEntry<bool> AutoSavingEnabled;
        internal static ConfigEntry<int> AutoSaveLimit;
        internal static ConfigEntry<int> LastAutoSave;
        internal static ConfigEntry<bool> DefaultIronMan;
        internal static ConfigEntry<int> ExtraMSUntilInterdiction;
    }
}
