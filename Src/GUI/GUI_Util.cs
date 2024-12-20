using Events;
using FG.Common.CMS;
using FGClient;
using MelonLoader;
using NOTFGT.Localization;
using NOTFGT.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Action = System.Action;
using Application = UnityEngine.Application;
using BuildInfo = NOTFGT.Logic.BuildInfo;
using Text = UnityEngine.UI.Text;
using static NOTFGT.GUI.ToolsMenu;

namespace NOTFGT.GUI
{

    [AttributeUsage(AttributeTargets.Field)]
    public class GUIReferenceAttribute : Attribute
    {
        public string Name { get; }
        public GUIReferenceAttribute(string target) => Name = target;
    }

    public class GUI_LogEntry
    {
        public LogType Type { get; set; }
        public string Msg { get; set; }
        public string Stacktrace { get; set; }
    }


    public class GUI_Util
    {
        const string BundleName = "not_fgtoolsGUI";

        const string GitHub = "github.com/floyzi";
        const string Discord = "discord.gg/PEysxvSE3x";
        const string Twitter = "twitter.com/@floyzi102";

        enum GUIHideState
        {
            Full,
            Hidden,
            Active
        }

        Color tabActiveCol = new(0.7028302f, 0.9941195f, 1f, 1f);
        Color InfoColor = new(0.7019608f, 1f, 0.9569892f, 1f);
        Color WarningColor = new(1f, 0.8809203f, 0.7019608f, 1f);
        Color ErrorColor = new(1f, 0.7028302f, 0.7028302f, 1f);

        AssetBundle GUI_Bundle;
        AssetBundleRequest theGUI;

        GameObject GUIObject;

        [GUIReference("StyleDefault")] readonly GameObject DefaultStyle;
        [GUIReference("StyleHidden")] readonly GameObject HiddenStyle;
        [GUIReference("StyleGameplay")] readonly GameObject GameplayStyle;
        [GUIReference("StyleRepair")] readonly GameObject RepairStyle;

        [GUIReference("DeleteConfig")] readonly Button DeleteConfig;
        [GUIReference("IgnoreThis")] readonly Button IgnoreThis;
        [GUIReference("ErrorDisplay")] readonly Text ErrorDisplay;

        [GUIReference("UnHideButton")] readonly Button UnHideButton;
        [GUIReference("HideButton")] readonly Button HideButton;
        [GUIReference("KillButton")] readonly Button KillButton;

        [GUIReference("ConfigDisplay")] readonly Transform configMenu;
        [GUIReference("WriteSave")] readonly Button applyChanges;
        [GUIReference("ResetConfig")] readonly Button deleteConfig;

        [GUIReference("PendingChangesAlert")] readonly GameObject PendingChanges;
        [GUIReference("ToggleReference")] readonly GameObject GUI_TogglePrefab;
        [GUIReference("FieldReference")] readonly GameObject GUI_TextFieldPrefab;
        [GUIReference("SliderReference")] readonly GameObject GUI_SliderPrefab;
        [GUIReference("MenuHeaderReference")] readonly GameObject GUI_HeaderPrefab;
        [GUIReference("MenuHeaderDescReference")] readonly GameObject GUI_HeaderDescPrefab;
        [GUIReference("ButtonReference")] readonly GameObject GUI_ButtonPrefab;

        [GUIReference("RoundInputField")] readonly InputField RoundIdInputField;
        [GUIReference("RoundLoadBtn")] readonly Button RoundLoadButton;
        [GUIReference("RandomRoundBtn")] readonly Button RoundLoadRandomButton;
        [GUIReference("RoundID_Entry")] readonly GameObject RoundIDEntry;
        [GUIReference("RoundID_EntryV2")] readonly Button RoundIDEntryV2;
        [GUIReference("RoundIDSView")] readonly Transform RoundIdsView;
        [GUIReference("RoundGenListBtn")] readonly Button RoundGenerateListButton;
        [GUIReference("RoundListCleanup")] readonly Button CleanupList;
        [GUIReference("RoundsDropDown")] readonly Dropdown RoundsDropdown;
        [GUIReference("RoundsIDSDropDown")] readonly Dropdown IdsDropdown;
        [GUIReference("ClickToCopyNote")] readonly Text ClickToCopy;

        [GUIReference("LogMessage")] readonly Button LogPrefab;
        [GUIReference("LogInfo")] readonly Text LogInfo;
        [GUIReference("LogDisplay")] readonly Transform LogContent;
        [GUIReference("ClearLogsBtn")] readonly Button ClearLogsBtn;
        [GUIReference("LogStats")] readonly Text LogStats;
        [GUIReference("LogDisabled")] readonly GameObject LogDisabledScreen;

        [GUIReference("GPActive")] readonly GameObject GameplayActive;
        [GUIReference("GPHidden")] readonly GameObject GameplayHidden;
        [GUIReference("OpenGPPanel")] readonly Button OpenGP;
        [GUIReference("HideGPPanel")] readonly Button HideGP;

        [GUIReference("InRoundLoader")] readonly GameObject GPRoundLoader;
        [GUIReference("InRealGame")] readonly GameObject GPRealGame;

        [GUIReference("RespawnButton")] readonly Button Respawn;
        [GUIReference("CheckpointButton")] readonly Button Checkpoint;

        [GUIReference("BTN_Discord")] readonly Button DiscordBtn;
        [GUIReference("BTN_Twitter")] readonly Button TwitterBtn;
        [GUIReference("BTN_GitHub")] readonly Button GitHubBtn;

        [GUIReference("HeaderTitle")] readonly Text Header;
        [GUIReference("HeaderSlogan")] readonly Text Slogan;

        readonly List<GameObject> Tabs = [];
        readonly List<GameObject> TabsButtons = [];

        readonly List<GUI_LogEntry> LogEntries = [];

        EventSystem.Handle _menuDisplayed;

        string BundlePath;
        string ReadyRound;

        bool HasGUIKilled = false;
        bool SuceedGUISetup = false;
        bool OnRepairScreen { get { return RepairStyle.gameObject != null && RepairStyle.gameObject.activeSelf; } }
        bool AllowGUIActions { get { return GUI_Bundle != null && GUIObject != null; } }


        public void ShowRepairGUI(Exception EX)
        {
            if (!AllowGUIActions)
            {
                MelonLogger.Msg("Can't show repair screen, bundle not loaded?");
                return;
            }
            ErrorDisplay.text = $"{EX.Message}";
            RepairStyle.gameObject.SetActive(true);
            DeleteConfig.onClick.AddListener(new Action(() => { NOTFGTools.Instance.SettingsMenu.DeleteConfig(); }));
            IgnoreThis.onClick.AddListener(new Action(() => { 
                RepairStyle.gameObject.SetActive(false);
                GameObject.Destroy(GUIObject);
                HasGUIKilled = true;
            }));
        }

        public void Register()
        {
            BundlePath = Path.Combine(NOTFGTools.AssetsDir, BundleName);
            MelonLogger.Msg($"EXPECTED BUNDLE PATH IS: {BundlePath}");
            GUI_Bundle = AssetBundle.LoadFromFile(BundlePath);
            _menuDisplayed = Broadcaster.Instance.Register<OnMainMenuDisplayed>(new Action<OnMainMenuDisplayed>(MenuEvent));

            MelonLogger.Msg($"[{base.GetType()}] Successful GUI_Util register. Is bundle loaded: {GUI_Bundle != null}");
            if (GUI_Bundle != null)
                TryToLoadGUI();
        }

        public void ProcessNewLog(GUI_LogEntry logEntry)
        {
            LogEntries.Add(logEntry);
            if (LogPrefab != null)
            {
                var newLog = GameObject.Instantiate(LogPrefab, LogContent);
                newLog.name = "ActiveLog";
                newLog.gameObject.SetActive(true);
                newLog.transform.SetAsFirstSibling();
                switch (logEntry.Type)
                {
                    case LogType.Log:
                        newLog.image.color = InfoColor;
                        break;
                    case LogType.Warning:
                        newLog.image.color = WarningColor;
                        break;
                    case LogType.Error:
                        newLog.image.color = ErrorColor;
                        break;
                }
                UpdateLogStatsText();
                string result = logEntry.Msg.Length > 55 ? result = logEntry.Msg.Substring(0, 55) + "..." : logEntry.Msg;
                newLog.gameObject.GetComponentInChildren<Text>().text = $" {result}";
                newLog.onClick.AddListener(new Action(() => { ShowAdvancedLog(logEntry); }));
            }
        }

        void ShowAdvancedLog(GUI_LogEntry logEntry)
        {
            LogInfo.text = LocalizationManager.LocalizedString("advanced_log", [logEntry.Type, logEntry.Msg, logEntry.Stacktrace]);
        }

        void ToggleGUI(GUIHideState toggle)
        {
            switch (toggle)
            {
                case GUIHideState.Full:
                    DefaultStyle.gameObject.SetActive(false);
                    HiddenStyle.gameObject.SetActive(false);
                    break;
                case GUIHideState.Hidden:
                    DefaultStyle.gameObject.SetActive(false);
                    HiddenStyle.gameObject.SetActive(true);
                    break;
                case GUIHideState.Active:
                    DefaultStyle.gameObject.SetActive(true);
                    HiddenStyle.gameObject.SetActive(false);
                    break;
            }
        }

        void MenuEvent(OnMainMenuDisplayed evt)
        {
            if (OnRepairScreen || HasGUIKilled)
                return;

            try
            {
                SetupGUI();
                ToggleGUI(GUIHideState.Full);
                ResetGPUI();
                void toggle(bool wasok)
                {
                    ToggleGUI(GUIHideState.Active);
                    ToggleTab(Tabs[1], TabsButtons[0].GetComponent<Button>());
                    SaveSettings();
                }
                NOTFGTools.Instance.HandlePlayerState(NOTFGTools.PlayerState.Menu);
                InternalTools.DoModal(LocalizationManager.LocalizedString("welcome_title", [BuildInfo.Name]), LocalizationManager.LocalizedString("welcome_desc", new object[] { BuildInfo.Name }), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default, new Action<bool>(toggle));
            }
            catch (Exception e)
            {
                InternalTools.DoModal(LocalizationManager.LocalizedString("failed_title", [BuildInfo.Name]), InternalTools.FormatException(e), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default);
            }
        }

        void UpdateLogStatsText() => LogStats.text = LocalizationManager.LocalizedString("errors_display", [LogEntries.FindAll(x => x.Type == LogType.Error).Count, LogEntries.FindAll(x => x.Type == LogType.Warning).Count, LogEntries.FindAll(x => x.Type == LogType.Log).Count, LogEntries.Count]);

        void SaveSettings()
        {
            var settings = NOTFGTools.Instance.SettingsMenu;
            settings.RollChanges();
            LogDisabledScreen.SetActive(!settings.GetValue<bool>(TrackGameDebug));
            NOTFGTools.Instance.ApplyChanges();
        }

        void SetupGUI()
        {
            if (SuceedGUISetup)
                return;

            try
            {
                ClearLogsBtn.onClick.AddListener(new Action(() =>
                {
                    CleanupScreen(LogContent, true);
                    LogEntries.Clear();
                    UpdateLogStatsText();
                }));
                applyChanges.onClick.AddListener(new Action(() => { SaveSettings(); }));
                UnHideButton.onClick.AddListener(new Action(() => { ToggleGUI(GUIHideState.Active); }));
                HideButton.onClick.AddListener(new Action(() => { ToggleGUI(GUIHideState.Hidden); }));
                KillButton.onClick.AddListener(new Action(() =>
                {
                    HasGUIKilled = true;
                    GameObject.Destroy(GUIObject);
                    InternalTools.DoModal(LocalizationManager.LocalizedString("gui_destroyed_title"), LocalizationManager.LocalizedString("gui_destroyed_desc"), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Default);
                }));
                RoundIdInputField.onValueChanged.AddListener(new Action<string>((str) => { ReadyRound = str; }));
                RoundLoadButton.onClick.AddListener(new Action(() =>
                {
                    NOTFGTools.Instance.LoadRound(ReadyRound);
                }));
                CleanupList.onClick.AddListener(new Action(() =>
                {
                    ClickToCopy.gameObject.SetActive(false);
                    CleanupScreen(RoundIdsView, true);
                }));
                RoundGenerateListButton.onClick.AddListener(new Action(() =>
                {
                    CleanupScreen(RoundIdsView, true);
                    NOTFGTools.Instance.RoundLoader.GenerateCMSList(RoundIdsView, RoundIDEntryV2);
                    ClickToCopy.gameObject.SetActive(true);
                }));
                RoundLoadRandomButton.onClick.AddListener(new Action(() => { NOTFGTools.Instance.RoundLoader.LoadRandomCms(); }));
                DiscordBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{Discord}"); }));
                TwitterBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{Twitter}"); }));
                GitHubBtn.onClick.AddListener(new Action(() => { Application.OpenURL($"https://{GitHub}"); }));
                HideGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, false); }));
                OpenGP.onClick.AddListener(new Action(() => { UpdateGPUI(true, true); }));
                deleteConfig.onClick.AddListener(new Action(() => {
                    InternalTools.DoModal(LocalizationManager.LocalizedString("delete_config_alert_title"), LocalizationManager.LocalizedString("delete_config_alert_desc"), FGClient.UI.UIModalMessage.ModalType.MT_OK_CANCEL, FGClient.UI.UIModalMessage.OKButtonType.Disruptive, new Action<bool>((val) => {
                        if (val)
                            NOTFGTools.Instance.SettingsMenu.DeleteConfig();
                    } ));
                
                }));
                RoundIDEntry.SetActive(false);
                Header.text = $"{BuildInfo.Name} V{BuildInfo.Version}";
                Slogan.text = $"{BuildInfo.Description}";
                ConfigureTabs();
                CreateConfigMenu(configMenu, NOTFGTools.Instance.SettingsMenu.GetAllEntries());
                InitRoundsDropdown();
                GUIObject.gameObject.SetActive(true);
                SuceedGUISetup = true;
            }
            catch (Exception e)
            {

            }
        }
        
        void TryToLoadGUI()
        {
            if (HasGUIKilled)
                return;

            if (GUIObject != null)
                return;

            var assetName = "NOT_FGToolsGUI";

            if (GUI_Bundle == null)
            {
                TriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_bundle", [BundleName, BundlePath]));
                return;
            }

            theGUI = GUI_Bundle.LoadAssetAsync<GameObject>(assetName);

            if (theGUI.asset == null)
            {
                TriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_asset", [assetName]));
                return;
            }

            var obj2 = GameObject.Instantiate(theGUI.asset);
            GUIObject = GameObject.Find($"{assetName}(Clone)");
            if (GUIObject != null)
            {
                GameObject.DontDestroyOnLoad(GUIObject);
                GUIObject.GetComponent<Canvas>().sortingOrder = 9999;
                GUIObject.transform.localPosition = new Vector3(GUIObject.transform.position.x + 1000, GUIObject.transform.position.y, GUIObject.transform.position.z);
                ConfigureObjects();
                GUIObject.gameObject.SetActive(false);
                RepairStyle.gameObject.SetActive(false);
            }
            else
                TriggerFailedToLoadUIModal(LocalizationManager.LocalizedString("init_fail_null_object", new object[] { $"{assetName}(Clone)" }));
        }

        void InitRoundsDropdown()
        {
            if (RoundsDropdown != null && IdsDropdown != null)
            {
                RoundsDropdown.onValueChanged.RemoveAllListeners();
                IdsDropdown.onValueChanged.RemoveAllListeners();

                RoundsDropdown.ClearOptions();
                IdsDropdown.ClearOptions();

                var rounds = CMSLoader.Instance.CMSData.Rounds;

                Dictionary<string, string> uniqRounds = [];

                foreach (var round in rounds)
                {
                    var scene = round.Value.GetSceneName();
                    if (scene == null || round.Value == null || round.Value.DisplayName == null)
                        continue;

                    if (!uniqRounds.ContainsKey(round.Value.GetSceneName()))
                        uniqRounds.Add(scene, InternalTools.CleanStr(round.Value.DisplayName.Text));
                }

                Il2CppSystem.Collections.Generic.List<string> roundNames = new();
                foreach (var round in uniqRounds.Values)
                    roundNames.Add(round);
                roundNames.Sort();
                RoundsDropdown.AddOptions(roundNames);

                IdsDropdown.onValueChanged.AddListener(new Action<int>((val) => {
                    ReadyRound = IdsDropdown.options[val].text;
                }));

                RoundsDropdown.onValueChanged.AddListener(new Action<int>((val) =>
                {
                    var scene = string.Empty;

                    foreach (var round in rounds)
                    {
                        if (round.Value.DisplayName != null && InternalTools.CleanStr(round.Value.DisplayName.Text) == RoundsDropdown.options[val].text)
                        {
                            scene = round.Value.GetSceneName();
                            break;
                        }
                    }

                    if (scene != null)
                    {
                        Il2CppSystem.Collections.Generic.List<string> ids = new();

                        foreach (var round in rounds)
                            if (round.Value.GetSceneName() == scene)
                                ids.Add(round.Key);
                        
                        IdsDropdown.ClearOptions();
                        IdsDropdown.AddOptions(ids);
                        ReadyRound = IdsDropdown.options[0].text;
                    }
                }));

                RoundsDropdown.onValueChanged.Invoke(0);
            }
        }



        public void CleanupScreen(Transform screen, bool includeOnlyActive)
        {
            for (int i = screen.childCount - 1; i >= 0; i--)
            {
                var child = screen.GetChild(i);
                if (!includeOnlyActive || child.gameObject.activeSelf)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }


        public void UpdateGPButtonActions(Action[] actions = null)
        {
            if (actions != null)
            {
                Respawn.onClick.AddListener(actions[0]);
                Checkpoint.onClick.AddListener(actions[1]);
            }
            else
            {
                Respawn.onClick.RemoveAllListeners();   
                Checkpoint.onClick.RemoveAllListeners();
            }
        }

        void TriggerFailedToLoadUIModal(string addotionalMsg) => InternalTools.DoModal(LocalizationManager.LocalizedString("gui_init_fail_generic_title"), LocalizationManager.LocalizedString("gui_init_fail_generic_desc", [addotionalMsg]), FGClient.UI.UIModalMessage.ModalType.MT_OK, FGClient.UI.UIModalMessage.OKButtonType.Disruptive);

        void ConfigureObjects()
        {
            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<GUIReferenceAttribute>() != null);

            var a = GUIObject.transform.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in a)
            {
                foreach (var field in fields)
                {
                    var refrerence = field.GetCustomAttribute<GUIReferenceAttribute>();
                    if (refrerence != null && refrerence.Name == t.name)
                    {
                        object component = null;

                        if (field.FieldType == typeof(Button))
                            component = t.GetComponent<Button>();
                        else if (field.FieldType == typeof(Text))
                            component = t.GetComponent<Text>();
                        else if (field.FieldType == typeof(InputField))
                            component = t.GetComponent<InputField>();
                        else if (field.FieldType == typeof(Dropdown))
                            component = t.GetComponent<Dropdown>();
                        else if (field.FieldType == typeof(GameObject))
                            component = t.gameObject;
                        else if (field.FieldType == typeof(Transform))
                            component = t;

                        if (component != null)
                        {
                            try{field.SetValue(this, component);}
                            catch {}
                        }
                    }
                }

                if (t.gameObject.name.StartsWith("NavTab_") && !TabsButtons.Exists(target => target.name == t.gameObject.name))
                    TabsButtons.Add(t.gameObject);

                if (t.gameObject.name.StartsWith("TAB_") && !Tabs.Exists(target => target.name == t.gameObject.name))
                    Tabs.Add(t.gameObject);
            }
        }

        void ConfigureTabs()
        {
            foreach (var navTab in TabsButtons)
            {
                Button btn = navTab.GetComponent<Button>();

                if (btn != null)
                {
                    GameObject tabOfBtn = Tabs.FirstOrDefault(tab => tab.name == navTab.name.Replace("NavTab_", "TAB_"));

                    if (tabOfBtn != null)
                    {
                        btn.onClick.AddListener(new System.Action(click));
                        void click() => ToggleTab(tabOfBtn, btn);
                    }
                }
            }
        }

        void ToggleTab(GameObject activeTab, Button btn)
        {
            foreach (var tab in Tabs)
            {
                tab.SetActive(false);
            }

            foreach (var tab in TabsButtons)
            {
                var block = tab.gameObject.GetComponent<Button>().colors;
                block.normalColor = Color.white;
                block.selectedColor = Color.white;
                block.highlightedColor = Color.white;
                tab.gameObject.GetComponent<Button>().colors = block;
            }

            var block2 = btn.colors;
            block2.normalColor = tabActiveCol;
            block2.selectedColor = tabActiveCol;
            block2.highlightedColor = tabActiveCol;
            btn.colors = block2;

            activeTab.SetActive(true);
        }

        public void TriggerPendingChanges(bool on)
        {
            PendingChanges.SetActive(on);
        }

        Transform GetTransformFromGUI(string name)
        {
            var a = GUIObject.transform.GetComponentsInChildren<Transform>(true).ToList();
            return a.Find(x => x.name == name);
        }

        public void UpdateGPUI(bool keepGUIOn, bool active)
        {
            GameplayStyle.SetActive(keepGUIOn);
            GameplayHidden.SetActive(!active);
            GameplayActive.SetActive(active);
            switch (NOTFGTools.Instance.ActivePlayerState)
            {
                case NOTFGTools.PlayerState.RealGame:
                    GPRoundLoader.SetActive(false);
                    GPRealGame.SetActive(true);
                    break;
                case NOTFGTools.PlayerState.RoundLoader:
                    GPRoundLoader.SetActive(true);
                    GPRealGame.SetActive(false);
                    break;
            }
        }

        void ResetGPUI()
        {
            GameplayActive.SetActive(false);
            GameplayHidden.SetActive(true);
            GameplayStyle.SetActive(false);
        }

        void CreateConfigMenu(Transform ConfigTransform, List<MenuEntry> configEntries)
        {
            HashSet<string> categories = [];

            foreach (var entry in configEntries.OrderBy(entry => entry.Category).ToList())
            {
                if (!string.IsNullOrEmpty(entry.Category) && !categories.Contains(entry.Category))
                {
                    GameObject haderInst = GameObject.Instantiate(GUI_HeaderPrefab, ConfigTransform);
                    haderInst.SetActive(true);
                    haderInst.name = $"Header_{entry.Category}";

                    var headerText = haderInst.GetComponentInChildren<Text>();
                    if (headerText != null)
                        headerText.text = headerText.text.Replace("{0}", LocalizationManager.LocalizedString(entry.Category));

                    categories.Add(entry.Category);
                }

                var entryDesc = LocalizationManager.LocalizedString(entry.Description);

                switch (entry.ValueType)
                {
                    case MenuEntry.Type.Bool:
                        GameObject toggleInst = GameObject.Instantiate(GUI_TogglePrefab, ConfigTransform);
                        toggleInst.SetActive(true);
                        toggleInst.name = entry.InternalName;

                        var toggle = toggleInst.transform.Find("Toggle").GetComponent<Toggle>();
                        var toggleTitle = toggleInst.transform.Find("Toggle").GetComponentInChildren<Text>();
                        var toggleDesc = toggleInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(entryDesc))
                            toggleDesc.text = $"*{LocalizationManager.LocalizedString(entryDesc)}";
                        else
                            toggleDesc.gameObject.SetActive(false);

                        if (toggleTitle != null)
                            toggleTitle.text = LocalizationManager.LocalizedString(entry.DisplayName);

                        toggle.isOn = Convert.ToBoolean(entry.Value);

                        toggle.onValueChanged.AddListener(new Action<bool>(act));
                        void act(bool val)
                        {
                            NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);
                        }
                        break;

                    case MenuEntry.Type.Int:
                    case MenuEntry.Type.Float:
                    case MenuEntry.Type.String:
                        GameObject fieldInst = GameObject.Instantiate(GUI_TextFieldPrefab, ConfigTransform);
                        fieldInst.SetActive(true);
                        fieldInst.name = entry.InternalName;

                        var inputField = fieldInst.transform.Find("InputField").GetComponent<InputField>();
                        var fieldTitle = fieldInst.transform.Find("FieldTitle").GetComponent<Text>();
                        var fieldDesc = fieldInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(entry.Description))
                            fieldDesc.text = $"*{LocalizationManager.LocalizedString(entry.Description)}";
                        else
                            fieldDesc.gameObject.SetActive(false);

                        if (fieldTitle != null)
                            fieldTitle.text = LocalizationManager.LocalizedString(entry.DisplayName);

                        inputField.text = entry.Value.ToString();
                        if (entry.AdditionalData != null && entry.AdditionalData is object[] v)
                            inputField.characterLimit = Convert.ToInt32(v[0]);

                        if (entry.ValueType == MenuEntry.Type.Int)
                        {
                            inputField.contentType = InputField.ContentType.IntegerNumber;
                            inputField.onValueChanged.AddListener(new Action<string>(act2));
                            void act2(string val)
                            {
                                if (int.TryParse(val, out int intVal))
                                {
                                    NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, intVal);
                                }
                            }
                        }
                        else if (entry.ValueType == MenuEntry.Type.Float)
                        {
                            inputField.contentType = InputField.ContentType.DecimalNumber;
                            inputField.onValueChanged.AddListener(new Action<string>(act3));
                            void act3(string val)
                            {
                                if (float.TryParse(val, out float floatVal))
                                {
                                    NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, floatVal);
                                }
                            }
                        }
                        else if (entry.ValueType == MenuEntry.Type.String)
                        {
                            inputField.contentType = InputField.ContentType.Standard;
                            inputField.onValueChanged.AddListener(new Action<string>(act4));
                            void act4(string val)
                            {
                                NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);
                            }
                        }
                        break;
                    case MenuEntry.Type.Slider:
                        GameObject sliderInst = GameObject.Instantiate(GUI_SliderPrefab, ConfigTransform);
                        sliderInst.SetActive(true);
                        sliderInst.name = entry.InternalName;

                        var slider = sliderInst.transform.Find("Slider").GetComponent<Slider>();
                        var sliderTitle = sliderInst.transform.Find("SliderTitle").GetComponent<Text>();
                        var sliderDesc = sliderInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(entryDesc))
                            sliderDesc.text = $"*{entryDesc}";
                        else
                            sliderDesc.gameObject.SetActive(false);

                        if (sliderTitle != null)
                            sliderTitle.GetComponentInChildren<Text>().text = LocalizationManager.LocalizedString(entry.DisplayName);

                        var sliderValue = slider.transform.Find("SliderValue").GetComponent<Text>();
                        if (entry.AdditionalData != null && entry.AdditionalData is object[] a)
                        {
                            slider.minValue = float.Parse(a[0].ToString());
                            slider.maxValue = float.Parse(a[1].ToString());
                        }
                        else
                            Debug.LogWarning("");

                        slider.value = float.Parse(entry.Value.ToString());
                        sliderValue.text = $"{Convert.ToInt32(entry.Value)} / {Convert.ToInt32(slider.maxValue)}";


                        slider.onValueChanged.AddListener(new Action<float>(act5));
                        void act5(float val)
                        {
                            NOTFGTools.Instance.SettingsMenu.UpdateValue(entry.InternalName, val);
                            sliderValue.text = $"{Convert.ToInt32(val)} / {Convert.ToInt32(slider.maxValue)}";
                        }
                        break;
                    case MenuEntry.Type.Button:
                        GameObject buttonInst = GameObject.Instantiate(GUI_ButtonPrefab, ConfigTransform);
                        buttonInst.SetActive(true);
                        buttonInst.name = entry.InternalName;

                        var button = buttonInst.transform.Find("Button").GetComponent<Button>();
                        var buttonDesc = buttonInst.transform.Find("FieldDesc").GetComponent<Text>();

                        if (!string.IsNullOrEmpty(entryDesc))
                            buttonDesc.text = $"*{entryDesc}";
                        else
                            buttonDesc.gameObject.SetActive(false);

                        button.GetComponentInChildren<Text>().text = LocalizationManager.LocalizedString(entry.DisplayName);

                        button.onClick.AddListener(new Action(() => {
                            MethodInfo theMethod = typeof(NOTFGTools).GetMethod(entry.Value.ToString());
                            theMethod.Invoke(NOTFGTools.Instance, null);
                        }));

                        break;
                    default:
                        Debug.LogWarning($"Fallback on: {entry.InternalName}");
                        break;
                }
            }
        }

    }
}
