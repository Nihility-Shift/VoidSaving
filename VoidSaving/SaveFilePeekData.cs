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

            if (PeekData.SaveDataVersion >= 3) //Post version 3 utilizes binary due to localization issues with string read/write and parsing
            {
                ShipName = PeekData.ShipName;
                JumpCounter = PeekData.PeekJumpCounter;
                TimePlayed = TimeSpan.FromHours(PeekData.TimePlayed);
                HealthPercent = PeekData.HealthPercent;
                IronMan = PeekData.IronManMode;
                ProgressDisabled = PeekData.ProgressionDisabled;
                return;
            }

            if (!PeekData.PeekInfo.IsNullOrEmpty())
            {
                string[] DataEntries;
                if (PeekData.SaveDataVersion >= 2)
                    DataEntries = PeekData.PeekInfo.Split(';');
                else
                    DataEntries = PeekData.PeekInfo.Split(',');

                ShipName = DataEntries[0];
                JumpCounter = int.Parse(DataEntries[1]);
                TimePlayed = TimeSpan.FromHours(Double.Parse(DataEntries[2]));
                if (DataEntries.Length > 3)
                {
                    ProgressDisabled = bool.Parse(DataEntries[3]);

                    if (DataEntries.Length > 4)
                    {
                        HealthPercent = float.Parse(DataEntries[4]);
                    }
                }
            }
            else
            {
                TimePlayed = TimeSpan.FromHours(-99.99);
                JumpCounter = -1;
                ShipName = "Couldn't peek file info";
                HealthPercent = -0.99f;
            }


            IronMan = PeekData.IronManMode;
        }

        public DateTime writeTime;

        public string ShipName;

        public int JumpCounter;

        public TimeSpan TimePlayed;

        public bool IronMan;

        public bool ProgressDisabled;

        public float HealthPercent;
    }
}
