using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidSaving
{
    internal class Config
    {
        internal static void Load(ConfigFile configFile)
        {
            SavesLocation = configFile.Bind("Settings", "SavesLocation", string.Empty);
            LastSave = configFile.Bind("Settings", "LastSave", string.Empty);
            AutoSaving = configFile.Bind("Settings", "AutoSaving", true);
            AutoSaveLimit = configFile.Bind("Settings", "AutoSaveLimit", 10);
        }

        internal static ConfigEntry<string> SavesLocation;
        internal static ConfigEntry<string> LastSave;
        internal static ConfigEntry<bool> AutoSaving;
        internal static ConfigEntry<int> AutoSaveLimit;
    }
}
