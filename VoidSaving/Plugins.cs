using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using VoidManager;
using VoidManager.MPModChecks;

namespace VoidSaving
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.USERS_PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Void Crew.exe")]
    [BepInDependency(VoidManager.MyPluginInfo.PLUGIN_GUID)]
    public class BepinPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "N/A")]
        private void Awake()
        {
            Log = Logger;
            VoidSaving.Config.Load(Config);
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }


    public class VoidManagerPlugin : VoidPlugin
    {
        public override MultiplayerType MPType => MultiplayerType.Host;

        public override string Author => MyPluginInfo.PLUGIN_AUTHORS;

        public override string Description => MyPluginInfo.PLUGIN_DESCRIPTION;

        public override string ThunderstoreID => MyPluginInfo.PLUGIN_THUNDERSTORE_ID;

        public override SessionChangedReturn OnSessionChange(SessionChangedInput input)
        {
            SaveHandler.StartedAsHost = input.StartedSessionAsHost;
            switch (input.CallType)
            {
                case CallType.Joining;
                    //Reset latest data doesn't carry into next run
                    SaveHandler.LatestData = null;
                    break;
                case CallType.HostCreateRoom:
                    return new SessionChangedReturn() { SetMod_Session = true };
                case CallType.HostChange:
                case CallType.HostStartSession:
                    if (input.IsHost && GameSessionManager.HasActiveSession && GameSessionManager.InHub)
                        return new SessionChangedReturn() { SetMod_Session = true };
                    break;
            }
            return base.OnSessionChange(input);
        }
    }
}