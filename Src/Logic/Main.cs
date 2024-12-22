using Events;
using FG.Common;
using FG.Common.Character;
using FGClient;
using FGClient.Rendering.XRay;
using FGClient.UI;
using FGClient.UI.Core;
using FGDebug;
using MelonLoader;
using NOTFGT.GUI;
using NOTFGT.Harmony;
using NOTFGT.Loader;
using NOTFGT.Localization;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FG.Common.CommonEvents;
using static FG.Common.GameStateMachine;
using static FGClient.GlobalGameStateClient;
using static GameStateEvents;
using static MelonLoader.MelonLogger;

namespace NOTFGT.Logic
{
    public static class BuildInfo
    {
        public const string Name = "NOT FGTools";
        public const string Description = "NOT FallGuys level loader by @floyzi102 on twitter";
        public const string Author = "Floyzi";
        public const string Company = null;
        public const string Version = "1.0.0";
        public const string DownloadLink = null;
    }

    public class NOTFGTools : MelonMod
    {
        public static NOTFGTools Instance { get; private set; }

        public enum PlayerState
        {
            Unknown, Loading, Menu, RealGame, RoundLoader
        }
        public PlayerState ActivePlayerState = PlayerState.Unknown;
        public PlayerState PreviousPlayerState = PlayerState.Unknown;

        public static string MainDir;
        public static string LogDir;
        public static string AssetsDir;
        public static string MobileSplash;

        readonly float guiPosBase = 280;
        Color BuildInfoColor = new(0.3764f, 0.0156f, 0.0156f, 1f);

        CharacterControllerData ActiveFGCCData;
        object[] DefaultFGCCData = null;

        public GUI_Util GUIUtil = new();
        public ToolsMenu SettingsMenu = new();
        public RoundLoaderService RoundLoader = new();

        readonly string NextLogDate = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string allLogs = string.Empty;
        bool playersHidden = false;

        Action<string, string, LogType> _logAction;
        EventSystem.Handle GameplayBegin = null;
        EventSystem.Handle IntroStart = null;
        EventSystem.Handle IntroEnd = null;
        EventSystem.Handle GuiInit = null;
        EventSystem.Handle OnSpectator = null;
        EventSystem.Handle OnRoundOver = null;

        public static bool CaptureTools { get { return Instance.SettingsMenu.GetValue<bool>(ToolsMenu.UseCaptureTools); } }


        public override void OnInitializeMelon()
        {
            Msg("Startup...");
            
            try
            {
                MainDir = Path.Combine(Application.persistentDataPath, "NOT_FGTools/");
                LogDir = Path.Combine(MainDir, "Logs");
                AssetsDir = Path.Combine(MainDir, "Assets");
                MobileSplash = Path.Combine(AssetsDir, "FGToolsMSplash.png");

                Instance = this;

                LocalizationManager.Setup();

                GUIUtil.Register();

                SettingsMenu.LoadConfig(false);

                ClassInjector.RegisterTypeInIl2Cpp<FallGuyBehaviour>();

                Msg("Starting common setup.");

                HarmonyInstance.PatchAll(typeof(HarmonyPatches.CaptureTools));
                HarmonyInstance.PatchAll(typeof(HarmonyPatches.GUITweaks));

                if (SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug))
                {
                    _logAction = new Action<string, string, LogType>(HandleLog);
                    Application.add_logMessageReceived(_logAction);
                }

                GameplayBegin = Broadcaster.Instance.Register<IntroCountdownEndedEvent>(new Action<IntroCountdownEndedEvent>(OnGameplayBegin));
                IntroStart = Broadcaster.Instance.Register<IntroCameraSequenceStartedEvent>(new Action<IntroCameraSequenceStartedEvent>(OnIntroStart));
                IntroEnd = Broadcaster.Instance.Register<IntroCameraSequenceEndedEvent>(new Action<IntroCameraSequenceEndedEvent>(OnIntroEnd));
                GuiInit = Broadcaster.Instance.Register<InitialiseClientOverlayEvent>(new Action<InitialiseClientOverlayEvent>(OnGUIInit));
                OnSpectator = Broadcaster.Instance.Register<ClientGameManagerSpectatorModeChanged>(new Action<ClientGameManagerSpectatorModeChanged>(OnSpectatorEvent));
                OnRoundOver = Broadcaster.Instance.Register<OnRoundOver>(new Action<OnRoundOver>(OnRoundOverEvent));

                HandlePlayerState(PlayerState.Loading);

                Msg("Startup successful.");
            }
            catch (Exception e)
            {
                Error($"Startup failed! Error: {e.Message}\n StackTrace: {e.StackTrace}");
                GUIUtil.ShowRepairGUI(e);
            }
        }

        public void HandlePlayerState(PlayerState playerState)
        {
            PreviousPlayerState = ActivePlayerState;
            ActivePlayerState = playerState;
        }

        void OnGameplayBegin(IntroCountdownEndedEvent evt)
        {
            playersHidden = false;
#if CHEATS
            SetupFGCCData();
            RollFGCCSettings();
#endif
            GUIUtil.UpdateGPUI(true, false);

            if (ActivePlayerState == PlayerState.RoundLoader)
            {
                FallGuyBehaviour.FGBehaviour.LoadGPActions();
                RoundLoader.RoundLoadingAllowed = true;
                FallGuyBehaviour.FGBehaviour.FallGuy.GetComponent<Rigidbody>().isKinematic = false;
                FallGuyBehaviour.FGBehaviour.spawnpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
                GameObject.Destroy(FallGuyBehaviour.FGBehaviour.spawnpoint.GetComponent<BoxCollider>());
                FallGuyBehaviour.FGBehaviour.spawnpoint.name = "Checkpoint";
                FallGuyBehaviour.FGBehaviour.spawnpoint.transform.SetPositionAndRotation(FallGuyBehaviour.FGBehaviour.FallGuy.transform.position, FallGuyBehaviour.FGBehaviour.FallGuy.transform.rotation);
            }
            else
            {
                //something for future
                GUIUtil.UpdateGPActions(null);
            }
        }

        void OnIntroStart(IntroCameraSequenceStartedEvent evt)
        {
#if CHEATS
            ResetFGCCData();
#endif
            GlobalGameStateClient.Instance.GameStateView.GetCharacterDataMonitor()._timeToRunNextCharacterControllerDataCheck = float.MaxValue;
            if (ActivePlayerState == PlayerState.RoundLoader)
            {

            }
        }

        void OnIntroEnd(IntroCameraSequenceEndedEvent evt)
        {
            if (RoundLoaderService.CGM == null || ActivePlayerState != PlayerState.RoundLoader)
                return;

            var gameLoading = RoundLoaderService.GameLoading;
            gameLoading.SetPlayerReadyIfNecessary();
            if (RoundLoaderService.CGM.CameraDirector != null && RoundLoaderService.CGM.CameraDirector.IsUsingIntroShots)
                gameLoading._clientGameManager.CameraDirector.UseCloseShot();
            gameLoading._clientGameManager.FinishPreparationPhase();

            RoundLoaderService.UIM = gameLoading._clientGameManager._inGameUiManager;
            gameLoading._clientGameManager.FinishPreparationPhase();
            Resources.FindObjectsOfTypeAll<PhysicsSimulator>().FirstOrDefault().ToggleRunningPhysicsAutomatically();
            gameLoading._clientGameManager.SetReady(PlayerReadinessState.IntroComplete, null, null);
            RoundLoader.RoundCamera?.SnapCameraNextFrame();
            RoundLoader.RoundCamera?.ForceRecenterToHeading();
            RoundLoader.RoundCamera?.AddCloseCameraTarget(FallGuyBehaviour.FGBehaviour.FallGuy, true);
            FallGuyBehaviour.FGBehaviour.FGCC.SetupOnClient(GlobalGameStateClient.Instance.NetObjectManager, FallGuyBehaviour.FGBehaviour.FGMPG);
            RoundLoaderService.UIM.SwitchToState(InGameUiManager.InGameState.Countdown);
            RoundLoaderService.CGM._gameSession.SetSessionState(GameSession.SessionState.Countdown);
            RoundLoaderService.UIM._inGameCountdownState._countdownViewModel.PlayAnimation();
            SpeedBoostManager SPM = FallGuyBehaviour.FGBehaviour.FGCC.SpeedBoostManager;
            SPM.SetAuthority(true);
            SPM.SetCharacterController(FallGuyBehaviour.FGBehaviour.FGCC);
            Resources.FindObjectsOfTypeAll<XRayMeshRendererTracker>().FirstOrDefault().AddXRayControllerForCharacter(FallGuyBehaviour.FGBehaviour.FGCC);
            RoundLoader.RoundLoadingAllowed = true;
            RoundLoaderService.GameLoading.HandleGameServerStartGame(new GameMessageServerStartGame(0, RoundLoaderService.CGM.CurrentGameSession.EndRoundTime, 0, 1, RoundLoaderService.CGM.GameRules.NumPerVsGroup, 1, 0));

        }

        void OnGUIInit(InitialiseClientOverlayEvent evt)
        {
            if (ActivePlayerState == PlayerState.RoundLoader)
            {
                var gameLoading = RoundLoaderService.GameLoading;
                RoundLoaderService.CGM = gameLoading._clientGameManager;
                RoundLoaderService.PTM = gameLoading._clientGameManager._playerTeamManager;

                gameLoading._clientGameManager.GameRules.PreparePlayerStartingPositions(1);

                foreach (CameraDirector cam in Resources.FindObjectsOfTypeAll<CameraDirector>())
                {
                    if (!cam.gameObject.activeSelf)
                        UnityEngine.Object.Destroy(cam.gameObject);
                }

                RoundLoader.RoundCamera = gameLoading._clientGameManager.CameraDirector;
                RoundLoader.SpawnFallGuy();
                FallGuyBehaviour.FGBehaviour.Init();
                RoundLoader.HideLoadingScreens();

                gameLoading.OnServerRequestStartIntroCameras();
            }
        }

        void OnSpectatorEvent(ClientGameManagerSpectatorModeChanged evt)
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
        }

        void OnRoundOverEvent(OnRoundOver evt)
        {
#if CHEATS
            ForceUnHidePlayers();
#endif
        }

        public void LoadRound(string roundId)
        {
            try
            {
                RoundLoader.LoadCmsRound(roundId, LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_round_loader_generic"), LocalizationManager.LocalizedString("error_round_loader_generic", [roundId, LoadSceneMode.Single, e.Message]), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default);
            }
        }

        public void ApplyChanges()
        {
            try
            {
                var debug = SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug);
                if (debug && _logAction == null)
                {
                    _logAction = new Action<string, string, LogType>(HandleLog);
                    Application.add_logMessageReceived(_logAction);
                }
                else if (_logAction != null && !debug)
                {
                    Application.remove_logMessageReceived(_logAction);
                }

                if (SettingsMenu.GetValue<bool>(ToolsMenu.UnlockFPS))
                    Application.targetFrameRate = Convert.ToInt32(SettingsMenu.GetValue<object>(ToolsMenu.TargetFPS));

                else
                    Application.targetFrameRate = GraphicsSettings.DefaultTargetFrameRate;

                var fgdebug = Resources.FindObjectsOfTypeAll<GvrFPS>().FirstOrDefault();
                if (fgdebug != null)
                {
                    fgdebug.gameObject.SetActive(false);
                    fgdebug._keepActive = false;
                    var scale = Convert.ToSingle(SettingsMenu.GetValue<object>(ToolsMenu.FGDebugScale));
                    fgdebug.transform.localScale = new Vector3(scale, scale, scale);
                }

#if CHEATS
                foreach (var afk in Resources.FindObjectsOfTypeAll<AFKManager>())
                    afk.enabled = SettingsMenu.GetValue<bool>(ToolsMenu.DisableAFK);

                GlobalDebug.DebugJoinAsSpectatorEnabled = SettingsMenu.GetValue<bool>(ToolsMenu.JoinAsSpectator);

                RollFGCCSettings();
#endif

                Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleMinimalisticFPSCounter());

                Broadcaster.Instance.Broadcast(new GlobalDebug.DebugToggleFPSCounter());

                SettingsMenu.RollSave();

                AudioManager.Instance.PlayOneShot(AudioManager.EventMasterData.SettingsAccept, null, default);
            }
            catch (Exception ex)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_settings_save_title"), LocalizationManager.LocalizedString("error_settings_save_desc", [ex.Message]), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
            }
        }

        void WatermarkGUI()
        {
            string watermark = $"<b>{BuildInfo.Name} V{BuildInfo.Version} {BuildInfo.Description.Substring(BuildInfo.Description.IndexOf("by"))}</b>";

            GUIStyle upper = new(UnityEngine.GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.014f * Screen.height),
            };
            GUIStyle bottom = new(UnityEngine.GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = (int)(0.014f * Screen.height),
                normal = { textColor = BuildInfoColor },
            };

            float labelWidth = 500f;
            float labelHeight = 25f;
            float labelX = (Screen.width - labelWidth) / 2f;
            float labelY = Screen.height - labelHeight;

            UnityEngine.GUI.Label(new Rect(labelX, labelY, labelWidth, labelHeight), watermark, bottom);
            UnityEngine.GUI.Label(new Rect(labelX, labelY - 2f, labelWidth, labelHeight), watermark, upper);
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (SettingsMenu.GetValue<bool>(ToolsMenu.TrackGameDebug))
            {
                var c = new GUI_LogEntry()
                {
                    Msg = logString,
                    Stacktrace = stackTrace,
                    Type = type,
                };

                GUIUtil.ProcessNewLog(c);

                string FileEntry = "[" + type + "] : " + logString + " \n[STACK TRACE] " + stackTrace;

                /*switch (type)
                {
                    case LogType.Error:
                        MelonLogger.Error(logString);
                        break;
                    case LogType.Warning:
                        MelonLogger.Warning(logString);
                        break;
                    case LogType.Log:
                        MelonLogger.Msg(logString);
                        break;
                    default:
                        MelonLogger.Msg(logString);
                        break;
                }*/

                allLogs += "\n" + FileEntry;

                string a = Path.Combine(LogDir, $"Log_{NextLogDate}.log");
                if (!File.Exists(a))
                    File.Create(a);

                File.WriteAllText(a, allLogs);
            }
        }
#if CHEATS
        public void SetupFGCCData()
        {

            ActiveFGCCData = Resources.FindObjectsOfTypeAll<CharacterControllerData>().FirstOrDefault();

            if (ActiveFGCCData != null && DefaultFGCCData == null)
            {
                DefaultFGCCData = new object[255];
                DefaultFGCCData[0] = ActiveFGCCData.normalMaxSpeed;
                DefaultFGCCData[1] = ActiveFGCCData.jumpForceUltimateParty;
                DefaultFGCCData[2] = ActiveFGCCData.divePlayerSensitivity;
                DefaultFGCCData[3] = ActiveFGCCData.maxGravityVelocity;
                DefaultFGCCData[4] = ActiveFGCCData.diveForce;
                DefaultFGCCData[5] = ActiveFGCCData.airDiveForce;
            }
    }

        void ResetFGCCData()
        {
            if (ActiveFGCCData != null && DefaultFGCCData != null)
            {
                ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0];
                ActiveFGCCData.jumpForceUltimateParty = (Vector3)DefaultFGCCData[1];
                ActiveFGCCData.divePlayerSensitivity = (float)DefaultFGCCData[2];
                ActiveFGCCData.maxGravityVelocity = (float)DefaultFGCCData[3];
                ActiveFGCCData.diveForce = (float)DefaultFGCCData[4];
                ActiveFGCCData.airDiveForce = (float)DefaultFGCCData[5];
            }
    }
        public void RollFGCCSettings()
        {
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            if (player != null)
            {
                var motorAgent = player.MotorAgent;

                if (SettingsMenu.GetValue<bool>(ToolsMenu.DisableMonitorCheck))
                {
                    Vector3 defJump = (Vector3)DefaultFGCCData[1];
                    ActiveFGCCData.normalMaxSpeed = (float)DefaultFGCCData[0] + float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.RunSpeedModifier).ToString());
                    ActiveFGCCData.jumpForceUltimateParty = new Vector3(defJump.x, defJump.y + float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.JumpYModifier).ToString()), defJump.z); ;
                    ActiveFGCCData.divePlayerSensitivity = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveSens).ToString());
                    ActiveFGCCData.maxGravityVelocity = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.GravityChange).ToString());
                    ActiveFGCCData.diveForce = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveForce).ToString());
                    ActiveFGCCData.airDiveForce = float.Parse(SettingsMenu.GetValue<object>(ToolsMenu.DiveInAirForce).ToString());
                }
                else
                    ResetFGCCData();

                motorAgent.GetMotorFunction<MotorFunctionJump>()._jumpForce = ActiveFGCCData.jumpForceUltimateParty;
            }
        }


        public void TeleportToFinish()
        {
            if (!DefaultCheck())
                return;

            var finish = Resources.FindObjectsOfTypeAll<COMMON_ObjectiveReachEndZone>().FirstOrDefault();
            if (finish == null)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_no_finish"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return;
            }

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);
            player.transform.SetPositionAndRotation(finish.transform.position + new Vector3(0, 10f, 0), finish.transform.rotation);
        }

        public void TeleportToSafeZone()
        {
            if (!DefaultCheck())
                return;

            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var safeZone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safeZone.name = "Safe";
            safeZone.transform.localScale = new Vector3(200, 5, 200);
            safeZone.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 150, player.transform.position.z);
            player.transform.position = safeZone.transform.position + new Vector3(0, 10, 0);
            safeZone.GetComponent<MeshRenderer>().enabled = false;
        }

        public void TeleportToRandomPlayer()
        {
            if (!DefaultCheck())
                return;

            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            var player = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().Find(a => a.IsLocalPlayer == true);

            var target = players[UnityEngine.Random.RandomRange(0, players.Count)];
            player.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
        }

        public void TogglePlayers()
        {
            if (!DefaultCheck())
                return;

            playersHidden = !playersHidden;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }

        bool DefaultCheck()
        {
            if (GlobalGameStateClient.Instance.IsInMainMenu)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_in_menu"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            if (!GlobalGameStateClient.Instance.GameStateView.IsGamePlaying)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("error_generic_action_title"), LocalizationManager.LocalizedString("error_game_not_active"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);
                return false;
            }
            
            return true;
        }

        void ForceUnHidePlayers()
        {
            playersHidden = false;
            var players = Resources.FindObjectsOfTypeAll<FallGuysCharacterController>().ToList().FindAll(a => a.IsLocalPlayer == false);
            foreach (var player in players)
                player.gameObject.SetActive(!playersHidden);
        }
#endif

        public void ForceMainMenu()
        {
            UIManager.Instance.RemoveAllScreens();
            GlobalGameStateClient.Instance.ResetGame();
            GlobalGameStateClient.Instance._gameStateMachine.ReplaceCurrentState(new StateMainMenu(GlobalGameStateClient.Instance._gameStateMachine, GlobalGameStateClient.Instance.CreateClientGameStateData(), false, false).Cast<IGameState>());
        }

        public override void OnGUI()
        {
            if (SettingsMenu.GetValue<bool>(ToolsMenu.GUI))
            {
                var debugLabel = 
                $"<b>DEBUG</b>\n\n" +
                $"Active state: {ActivePlayerState}\n" + 
                $"Prev state: {PreviousPlayerState}\n" +
                $"Version: {BuildInfo.Version}\n" +
                $"Game Version: {Application.version}";
                var size = UnityEngine.GUI.skin.label.CalcSize(new(debugLabel));

                UnityEngine.GUI.Box(new Rect(10f, guiPosBase + 10, size.x + 10f, size.y + 10f), "");
                UnityEngine.GUI.Label(new Rect(15f, guiPosBase + 15, size.x + 10f, size.y + 10f), debugLabel);
            
            }

            if (SettingsMenu.GetValue<bool>(ToolsMenu.Watermark)) 
                WatermarkGUI();
        }
    }
}