using CG.Game;
using CG.Game.SpaceObjects.Controllers;
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
                if (GUITools.DrawButtonSelected($"{KVP.Key}{(data.IronMan ? " (IronMan)" : string.Empty)}{(data.ProgressDisabled ? " (Progress Disabled)" : string.Empty)} - {data.writeTime} | {data.ShipName}, {data.JumpCounter} Jumps. {(int)data.TimePlayed.TotalHours}:{data.TimePlayed.Minutes.ToString("00")} Played", SaveName == KVP.Key))
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

        private void DrawAutoSaveSelection()
        {
            BeginHorizontal();
            GUITools.DrawCheckbox("Auto Saves Enabled", ref Config.AutoSavingEnabled);
            FlexibleSpace(); FlexibleSpace();
            GUITools.DrawTextField("Auto Save Limit", ref Config.AutoSaveLimit);
            EndHorizontal();
        }


        public override void Draw()
        {
            if (!GameSessionManager.HasActiveSession)
            {
                DrawSaveFileList();
                Label("Must be in hub to load a save file");
                return;
            }

            if (GameSessionManager.InHub)
            {
                BeginHorizontal();
                GUITools.DrawCheckbox("Default Iron Man Mode", ref Config.DefaultIronMan);
                if (GUITools.DrawCheckbox("Iron Man Mode for next game", ref IronManMode))
                {
                    SaveHandler.IsIronManMode = IronManMode;
                }
                EndHorizontal();

                DrawAutoSaveSelection();

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

                if (ErrorMessage != null)
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
            else
            {
                DrawSaveFileList();

                VoidJumpSystem voidJumpSystem = ClientGame.Current?.PlayerShip?.transform?.GetComponent<VoidJumpSystem>();
                VoidJumpState voidJumpState = voidJumpSystem?.ActiveState;
                if (!SaveHandler.StartedAsHost)
                {
                    Label("Must be the original host of the session.");
                }
                else if (voidJumpState == null || (voidJumpState is not VoidJumpTravellingStable && voidJumpState is not VoidJumpTravellingUnstable))
                {
                    Label("Cannot save outside void jump.");
                }
                else
                {
                    SaveName = TextField(SaveName);

                    if (ErrorMessage != null)
                    {
                        Label(ErrorMessage);
                    }

                    DrawAutoSaveSelection();

                    if (SaveHandler.IsIronManMode)
                    {
                        if (Button("Save Game"))
                        {
                            if (SaveName.IsNullOrEmpty())
                            {
                                ErrorMessage = $"<color=red>Cannot save without a file name.</color>";
                                return;
                            }
                            if (SaveHandler.WriteIronManSave(SaveName))
                            {
                                SaveNames = SaveHandler.GetPeekedSaveFiles();
                                ErrorMessage = $"Successfully wrote {SaveName}";
                            }
                        }
                    }
                    else if (Button("Save Game"))
                    {
                        if (SaveName.IsNullOrEmpty())
                        {
                            ErrorMessage = $"<color=red>Cannot save without a file name.</color>";
                            return;
                        }
                        if (SaveHandler.WriteSave(SaveName))
                        {
                            SaveNames = SaveHandler.GetPeekedSaveFiles();
                            ErrorMessage = $"Successfully wrote {SaveName}";
                        }
                    }
                }
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
