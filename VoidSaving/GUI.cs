using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using System.IO;
using VoidManager.CustomGUI;
using static UnityEngine.GUILayout;

namespace VoidSaving
{
    internal class GUI : ModSettingsMenu
    {
        public override string Name()
        {
            return MyPluginInfo.USERS_PLUGIN_NAME;
        }

        public override void Draw()
        {
            if (GameSessionManager.InHub)
            {
                if (Button("Load Save"))
                {
                    SaveHandler.LoadSave(Path.Combine(SaveHandler.SaveLocation, "autosave_1.voidsave"));
                    SaveHandler.LoadSavedData = true;
                }
            }
            else 
            {
                VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
                VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
                if (voidJumpState == null || (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable))
                {
                    Label("Not in Void Jump");
                }
                else
                {
                    Label("Not Implimented yet");
                    if(Button("Save Game"))
                    {

                    }
                }
            }
        }
    }
}
