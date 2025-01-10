using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

        Dictionary<string, DateTime> SaveNames;

        string SelectedSaveName;

        Vector2 SaveScrollPosition;


        string ToSaveFileName;


        public override void Draw()
        {
            if (GameSessionManager.InHub)
            {
                BeginScrollView(SaveScrollPosition);
                foreach (KeyValuePair<string, DateTime> KVP in SaveNames)
                {
                    if (VoidManager.Utilities.GUITools.DrawButtonSelected($"{KVP.Key} - {KVP.Value.ToLocalTime()}", SelectedSaveName == KVP.Key))
                    {
                        SelectedSaveName = KVP.Key;
                    }
                }
                EndScrollView();

                if (SelectedSaveName == null)
                {
                    Label("Select a save");
                }
                else if (Button(SaveHandler.LoadSavedData ? $"Loading {SelectedSaveName} on next session start" : "Load Save"))
                {
                    SaveHandler.LoadSave(Path.Combine(SaveHandler.SaveLocation, SelectedSaveName + SaveHandler.SaveExtension));
                }


                if (SaveHandler.LoadSavedData)
                {
                    if (Button("Cancel")) { SaveHandler.CancelLoad(); }
                }
                else
                {
                    Label(string.Empty);
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
                    ToSaveFileName = TextField(ToSaveFileName);

                    if(Button("Save Game"))
                    {
                        SaveHandler.WriteSave(Path.Combine(SaveHandler.SaveLocation, ToSaveFileName));
                    }
                }
            }
        }

        public override void OnOpen()
        {
            SelectedSaveName = null;
            SaveNames = SaveHandler.GetSaveFileNames();
        }
    }
}
