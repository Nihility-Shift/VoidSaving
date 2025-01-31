using System;
using WebSocketSharp;

namespace VoidSaving
{
    internal class SaveFilePeekData
    {
        public SaveFilePeekData(string FileName, DateTime LastWriteTime)
        {
            this.writeTime = LastWriteTime;

            SaveGameData PeekData = SaveHandler.PeekSaveFile(FileName);

            if (!PeekData.PeekInfo.IsNullOrEmpty())
            {
                string[] DataEntries = PeekData.PeekInfo.Split(',');
                ShipName = DataEntries[0];
                JumpCounter = int.Parse(DataEntries[1]);
                TimePlayed = TimeSpan.FromHours(Double.Parse(DataEntries[2]));
                if (DataEntries.Length > 3)
                {
                    ProgressDisabled = bool.Parse(DataEntries[3]);
                }
            }
            else
            {
                TimePlayed = TimeSpan.FromHours(-99.99);
                JumpCounter = -1;
                ShipName = "Couldn't peek file info";
            }


            IronMan = PeekData.IronManMode;
        }

        public DateTime writeTime;

        public string ShipName;

        public int JumpCounter;

        public TimeSpan TimePlayed;

        public bool IronMan;

        public bool ProgressDisabled;
    }
}
