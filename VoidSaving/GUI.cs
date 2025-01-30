using CG.Game;
using CG.Game.SpaceObjects.Controllers;
using System;
using System.Collections.Generic;
using UnityEngine;
using VoidManager.CustomGUI;
using VoidManager.Utilities;
using WebSocketSharp;
using static UnityEngine.GUILayout;

namespace VoidSaving
{
    internal class GUI : ModSettingsMenu
    {
        public override string Name()
        {
            return MyPluginInfo.USERS_PLUGIN_NAME;
        }

        Dictionary<string, SaveFilePeekData> SaveNames;

        Vector2 SaveScrollPosition;

        bool FailedToLoadLastSave;

        bool IronManMode = true;


        string ToDeleteFileName;

        bool ConfirmedDelete;

        //Used for Loading and Saving.
        string SaveName;

        string ErrorMessage;


        private void DrawSaveFileList()
        {
            SaveScrollPosition = BeginScrollView(SaveScrollPosition);
            foreach (KeyValuePair<string, SaveFilePeekData> KVP in SaveNames)
            {
                BeginHorizontal();
                SaveFilePeekData data = KVP.Value;
                if (GUITools.DrawButtonSelected($"{KVP.Key}{(data.IronMan ? "(IronMan)" : string.Empty)} - {data.writeTime} | {data.ShipName}, {data.JumpCounter} Jumps. {data.TimePlayed.ToString(@"hh\:mm")} Played", SaveName == KVP.Key))
                {
                    SaveName = KVP.Key;
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
                SaveHandler.DeleteSaveFile(ToDeleteFileName);
                ToDeleteFileName = null;
            }
        }


        public override void Draw()
        {
            if (GameSessionManager.InHub)
            {
                GUITools.DrawCheckbox("Default Iron Man Mode", ref Config.DefaultIronMan);
                if (GUITools.DrawCheckbox("Iron Man Mode for next save", ref IronManMode))
                {
                    SaveHandler.IsIronManMode = IronManMode;
                }

                DrawSaveFileList();


                if (SaveName == null)
                {
                    Label("Select a save");
                }
                else if (Button(SaveHandler.LoadSavedData ? $"Loading {SaveName} on next session start" : "Load Save"))
                {
                    if (!SaveHandler.LoadSave(SaveName))
                    {
                        ErrorMessage = $"<color=red>Failed to load {SaveName}</color>";
                    }
                }

                if(ErrorMessage != null)
                {
                    Label(ErrorMessage);
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
                DrawSaveFileList();

                VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
                VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
                if (voidJumpState == null || (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable))
                {
                    Label("Not in Void Jump");
                }
                else if(!SaveHandler.StartedAsHost)
                {
                    Label("Must be the original host of the session.");
                }
                else
                {
                    SaveName = TextField(SaveName);

                    if (ErrorMessage != null)
                    {
                        Label(ErrorMessage);
                    }

                    if (SaveHandler.IsIronManMode && Button("Save Game"))
                    {
                        if (SaveName.IsNullOrEmpty())
                        {
                            ErrorMessage = $"<color=red>Cannot save without a file name.</color>";
                            return;
                        }
                        SaveHandler.WriteIronManSave(SaveName);
                    }
                    else if(Button("Save Game"))
                    {
                        if (SaveName.IsNullOrEmpty())
                        {
                            ErrorMessage = $"<color=red>Cannot save without a file name.</color>";
                            return;
                        }
                        SaveHandler.WriteSave(SaveName);
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
            IronManMode = SaveHandler.IsIronManMode;
            FailedToLoadLastSave = false;
            ToDeleteFileName = null;
            SaveNames = SaveHandler.GetPeekedSaveFiles();

            if (SaveHandler.ActiveData != null)
            {
                SaveName = SaveHandler.ActiveData?.FileName;
            }
            else
            {
                SaveName = SaveHandler.LastSaveName;
            }

            ErrorMessage = null;
        }
    }
}
