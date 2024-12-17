using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoidSaving
{
    public class SaveGameData
    {
        public SaveGameData(int alloys, int biomass, List<GUIDUnion> relics, List<GUIDUnion> looseItems, List<GUIDUnion> unlockedBPs)
        {
            Relics = relics;
            LooseItems = looseItems;
            UnlockedBPs = unlockedBPs;
        }

        public int Alloy;

        public int Biomass;

        public List<GUIDUnion> Relics;

        public List<GUIDUnion> LooseItems;

        public List<GUIDUnion> UnlockedBPs;

        public QuestData QuestData;

        public ShipData ShipData;
    }

    public class ShipData
    {

    }

    public class QuestData
    {
        public QuestData(GameSession session)
        {
            GameSessionManager.ActiveSector.
            session.ActiveQuest.
        }
    }
}
