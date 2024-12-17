using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidSaving
{
    internal class SaveHandler
    {
        public static SaveGameData ActiveData { get; internal set; }

        public static bool LoadSave(string SavePath)
        {
            return false;
        }

        public static bool WriteSave(string SavePath)
        {
            return false;
        }
    }
}
