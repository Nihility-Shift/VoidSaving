using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidSaving
{
    internal class SaveHandler
    {
        public static SaveGameData ActiveData { get; internal set; }

        public static bool StartedAsHost { get; internal set; }

        public static string SavesLocation
        {
            get
            {
                return Config.SavesLocation.Value;
            }
            set
            {
                Config.SavesLocation.Value = value;
            }
        }

        public static bool LoadSave(string SavePath)
        {
            SaveGameData data = new SaveGameData();

            ActiveData = data;
            return false;
        }

        public static bool WriteSave(string SavePath)
        {
            try
            {
                FileStream fileStream = File.Create(SavePath);
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {

                }
            }
            catch(Exception ex)
            {
                BepinPlugin.Log.LogError(ex);
            }
            return false;
        }
    }
}
