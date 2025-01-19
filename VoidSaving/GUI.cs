using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VoidManager.CustomGUI;
using VoidManager.Utilities;
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

        bool FailedToLoadLastSave;


        string ToSaveFileName;

        string ToDeleteFileName;

        bool ConfirmedDelete;


        public override void Draw()
        {
            if (GameSessionManager.InHub)
            {
                SaveScrollPosition = BeginScrollView(SaveScrollPosition);
                foreach (KeyValuePair<string, DateTime> KVP in SaveNames)
                {
                    BeginHorizontal();
                    if (GUITools.DrawButtonSelected($"{KVP.Key} - {KVP.Value.ToLocalTime()}", SelectedSaveName == KVP.Key))
                    {
                        SelectedSaveName = KVP.Key;
                    }
                    if (Button("X", MaxWidth(25f)))
                    {
                        ToDeleteFileName = KVP.Key;
                    }
                    EndHorizontal();

                    if (ToDeleteFileName == KVP.Key)
                    {
                        if (Button("Confirm delete file?"))
                        {
                            ConfirmedDelete = true;
                        }
                    }
                }
                EndScrollView();

                if (ConfirmedDelete)
                {
                    ConfirmedDelete = false;
                    SaveNames.Remove(ToDeleteFileName);
                    SaveHandler.DeleteSaveFile(ToDeleteFileName + SaveHandler.SaveExtension);
                    ToDeleteFileName = null;
                }


                if (SelectedSaveName == null)
                {
                    Label("Select a save");
                }
                else if (Button(SaveHandler.LoadSavedData ? $"Loading {SelectedSaveName} on next session start" : "Load Save"))
                {
                    FailedToLoadLastSave = !SaveHandler.LoadSave(SelectedSaveName + SaveHandler.SaveExtension);
                }

                if (FailedToLoadLastSave)
                {
                    Label($"<color=red>Failed to load {SelectedSaveName}</color>");
                }

                if (SaveHandler.LoadSavedData)
                {
                    if (Button("Cancel")) { SaveHandler.CancelOrFinalzeLoad(); }
                }
                else
                {
                    Label(string.Empty);
                }
            }
            else if(GameSessionManager.HasActiveSession)
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
            else
            {
                Label("Must be in hub");
            }
        }

        public override void OnOpen()
        {
            FailedToLoadLastSave = false;
            SelectedSaveName = SaveHandler.ActiveData?.FileName;
            ToDeleteFileName = null;
            SaveNames = SaveHandler.GetSaveFileNames();
        }
    }
}
