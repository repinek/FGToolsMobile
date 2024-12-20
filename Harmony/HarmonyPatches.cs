using FGClient;
using FGClient.UI;
using FGDebug;
using NOTFGT.GUI;
using NOTFGT.Localization;
using NOTFGT.Logic;
using System;
using System.IO;
using TMPro;

namespace NOTFGT.Harmony
{
    public class HarmonyPatches
    {
        public class CaptureTools
        {
            [HarmonyLib.HarmonyPostfix]
            [HarmonyLib.HarmonyPatch(typeof(CaptureToolsManager), "CanUseCaptureTools", HarmonyLib.MethodType.Getter)]
            public static void CanUseCaptureTools(ref bool __result)
            {
                __result = NOTFGTools.CaptureTools;
            }
        }

        public class GUITweaks
        {
            [HarmonyLib.HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleMinimalisticFPSCounter)), HarmonyLib.HarmonyPrefix]
            static bool ToggleMinimalisticFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleMinimalisticFPSCounter toggleEvent)
            {
                var target = NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.FPSCoutner);
                if (target && !NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.WholeFGDebug))
                {
                    if (!__instance.gameObject.activeSelf)
                    {
                        __instance.gameObject.SetActive(true);
                        __instance._keepActive = true;
                    }

                    foreach (TextMeshProUGUI TMP in __instance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        if (TMP != __instance.fpsText)
                        {
                            TMP.gameObject.SetActive(false);
                        }
                        else
                            TMP.gameObject.SetActive(target);

                    }
                }
                return false;
            }

            [HarmonyLib.HarmonyPatch(typeof(GvrFPS), nameof(GvrFPS.ToggleFPSCounter)), HarmonyLib.HarmonyPrefix]
            static bool ToggleFPSCounter(GvrFPS __instance, GlobalDebug.DebugToggleFPSCounter toggleEvent)
            {
                var target = NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.WholeFGDebug);
                if (target && !NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.FPSCoutner))
                {
                    __instance.gameObject.SetActive(target);
                    foreach (TextMeshProUGUI TMP in __instance.GetComponentsInChildren<TextMeshProUGUI>(true))
                    {
                        TMP.gameObject.SetActive(target);
                    }
                    __instance._keepActive = __instance.gameObject.activeSelf;
                }
                return false;
            }

            [HarmonyLib.HarmonyPatch(typeof(StateMainMenu), nameof(StateMainMenu.HandleConnectEvent)), HarmonyLib.HarmonyPrefix]
            static bool HandleConnectEvent(StateMainMenu __instance, ConnectEvent evt)
            {
                if (NOTFGTools.Instance.SettingsMenu.GetValue<bool>(ToolsMenu.DisableMonitorCheck))
                    InternalTools.DoModal(LocalizationManager.LocalizedString("fgcc_alert_title"), LocalizationManager.LocalizedString("fgcc_alert_desc"), UIModalMessage.ModalType.MT_OK_CANCEL, UIModalMessage.OKButtonType.Disruptive, new Action<bool>(Go));
                else
                    Go(true);

                void Go(bool wasok)
                {
                    if (wasok)
                    {
                        ServerSettings serverSettings = evt.playerProfile.serverSettings;
                        __instance.StartConnecting(serverSettings.ServerAddress, serverSettings.ServerPort, serverSettings.MatchmakingEnv);
                        NOTFGTools.Instance.HandlePlayerState(NOTFGTools.PlayerState.RealGame);
                    }
                }
                return false;
            }
            [HarmonyLib.HarmonyPatch(typeof(LoadingScreenViewModel), nameof(LoadingScreenViewModel.Awake)), HarmonyLib.HarmonyPrefix]
            static bool ShowScreen(LoadingScreenViewModel __instance)
            {
                __instance._canvasFader = __instance.GetComponent<CanvasGroupFader>();
                if (File.Exists(NOTFGTools.MobileSplash))
                {
                    var spr = InternalTools.SetSpriteFromFile(NOTFGTools.MobileSplash, 1920, 1080);
                    __instance.gameObject.transform.FindChild("SplashScreen_Image").gameObject.GetComponent<UnityEngine.UI.Image>().sprite = spr;
                    __instance.SplashLoadingScreenSprite = spr;    
                }
                return false;
            }
        }
    }
}
