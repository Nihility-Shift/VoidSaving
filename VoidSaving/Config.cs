using BepInEx.Configuration;

namespace VoidSaving
{
    internal class Config
    {
        internal static void Load(ConfigFile configFile)
        {
            SavesLocation = configFile.Bind("Settings", "SavesLocation", string.Empty);
            LastSave = configFile.Bind("Settings", "LastSave", string.Empty);
            AutoSavingEnabled = configFile.Bind("Settings", "AutoSaving", true);
            AutoSaveLimit = configFile.Bind("Settings", "AutoSaveLimit", 10);
            LastAutoSave = configFile.Bind("Data", "LastAutoSave", 0);
        }

        internal static ConfigEntry<string> SavesLocation;
        internal static ConfigEntry<string> LastSave;
        internal static ConfigEntry<bool> AutoSavingEnabled;
        internal static ConfigEntry<int> AutoSaveLimit;
        internal static ConfigEntry<int> LastAutoSave;
    }
}
