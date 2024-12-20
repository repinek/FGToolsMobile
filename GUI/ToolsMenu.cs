using MelonLoader;
using Newtonsoft.Json;
using NOTFGT.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using File = System.IO.File;
using Path = System.IO.Path;

namespace NOTFGT.GUI
{
    public class ToolsMenu
    {
        const string TargetCfgName = "ConfigV2.json";

        const string FGCC_Cat = "menu_fgcc_section";
        const string FPS_Cat = "menu_fps_section";
        const string Def_Cat = "menu_section";
        const string Debug_Cat = "menu_debug_section";
        const string Gameplay_Cat = "menu_gp_section";

        public const string GUI = "GUI";
        public const string JoinAsSpectator = "JoinAsSpectator";
        public const string UseCaptureTools = "UseCaptureTools";
        public const string TrackGameDebug = "TrackGameDebug";
        public const string FPSCoutner = "FPSCoutner";
        public const string WholeFGDebug = "WholeFGDebug";
        public const string UnlockFPS = "UnlockFPS";
        public const string TargetFPS = "TargetFPS";
        public const string RunSpeedModifier = "RunSpeedModifier";
        public const string JumpYModifier = "JumpYModifier";
        public const string Watermark = "Watermark";
        public const string DiveSens = "DiveSens";
        public const string DisableMonitorCheck = "DisableMonitorCheck";
        public const string ToFinish = "ToFinish";
        public const string DisableAFK = "DisableAFK";
        public const string GravityChange = "GravityChange";
        public const string DiveForce = "DiveForce";
        public const string DiveInAirForce = "DiveInAirForce";
        public const string ToSafe = "ToSafe";
        public const string ToRandomPlayer = "ToRandomPlayer";
        public const string HidePlayers = "HidePlayers";
        public const string ForceMenu = "ForceMenu";
        public const string FGDebugScale = "FGDebugScale";

        private static readonly Dictionary<string, (string Category, string DisplayName, string Description, object Value, object[] AdditionalData, MenuEntry.Type ValueType)> EditableMenu = new()
        {
            { GUI, (Def_Cat, "cheat_entry_gui_title", "cheat_entry_gui_desc", false, null, MenuEntry.Type.Bool) },
            { TrackGameDebug, (Debug_Cat, "cheat_entry_track_debug_title", "cheat_entry_track_debug_desc", false, null, MenuEntry.Type.Bool) },
            { FPSCoutner, (FPS_Cat, "cheat_entry_fps_counter_title", "cheat_entry_fps_counter_desc", true, null, MenuEntry.Type.Bool) },
            { WholeFGDebug, (Debug_Cat, "cheat_entry_fg_debug_title", "cheat_entry_fg_debug_desc", false, null, MenuEntry.Type.Bool) },
            { UnlockFPS, (FPS_Cat, "cheat_entry_unlock_fps_title", "cheat_entry_unlock_fps_desc", true, null, MenuEntry.Type.Bool) },
            { TargetFPS, (FPS_Cat, "cheat_entry_target_fps_title", "cheat_entry_target_fps_desc", 60, new object[] { 3 }, MenuEntry.Type.Int) },
            { Watermark, (Def_Cat, "cheat_entry_watermark_title", "cheat_entry_watermark_desc", true, null, MenuEntry.Type.Bool) },
            { FGDebugScale, (Debug_Cat, "cheat_entry_fg_debug_scale_title", "cheat_entry_fg_debug_scale_desc", 0.602f, new object[] { 0f, 1f }, MenuEntry.Type.Slider) },

#if CHEATS
            { UseCaptureTools, (Def_Cat, "cheat_entry_capture_tools_title", "cheat_entry_capture_tools_desc", false, null, MenuEntry.Type.Bool) },
            { RunSpeedModifier, (FGCC_Cat, "cheat_entry_run_speed_title", "cheat_entry_run_speed_desc", 0.0f, new object[] { 1f, 999f }, MenuEntry.Type.Slider) },
            { JumpYModifier, (FGCC_Cat, "cheat_entry_jump_y_title", "cheat_entry_jump_y_desc", 0.0f, null, MenuEntry.Type.Float) },
            { DiveSens, (FGCC_Cat, "cheat_entry_dive_sens_title", "cheat_entry_dive_sens_desc", 70.0f, null, MenuEntry.Type.Float) },
            { DisableMonitorCheck, (FGCC_Cat, "cheat_entry_fgcc_check_title", "cheat_entry_fgcc_check_desc", true, null, MenuEntry.Type.Bool) },
            { DisableAFK, (Gameplay_Cat, "cheat_entry_afk_title", "cheat_entry_afk_desc", false, null, MenuEntry.Type.Bool) },
            { GravityChange, (FGCC_Cat, "cheat_entry_gravity_vel_title", "cheat_entry_gravity_vel_desc", 60f, new object[] { 0f, 100f }, MenuEntry.Type.Slider) },
            { DiveForce, (FGCC_Cat, "cheat_entry_dive_force_title", "cheat_entry_dive_force_desc", 0.0f, new object[] { 1f, 999f }, MenuEntry.Type.Slider) },
            { DiveInAirForce, (FGCC_Cat, "cheat_entry_air_dive_force_title", "cheat_entry_air_dive_force_desc", 0.0f, new object[] { 1f, 999f }, MenuEntry.Type.Slider) },
            { JoinAsSpectator, (Def_Cat, "cheat_entry_spectator_title", "cheat_entry_spectator_desc", false, null, MenuEntry.Type.Bool) },
#endif
        };

        public class MenuEntry
        {
            public enum Type
            {
                Bool,
                Int,
                Float,
                String,
                Slider,
                Button
            }

            public string InternalName { get; set; }
            public string Category { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; } 
            public object Value { get; set; }
            public object AdditionalData { get; set; }
            public Type ValueType { get; set; }
        }

        public void LoadConfig()
        {
            string path = Path.Combine(NOTFGTools.AssetsDir, TargetCfgName);

            MelonLogger.Msg($"[{base.GetType()}] Loading config at path {path}");
            var definitions = new HashSet<string>
            {
                GUI, TrackGameDebug, FPSCoutner, WholeFGDebug, UnlockFPS, TargetFPS, 
                FGDebugScale, Watermark,
#if CHEATS
                JoinAsSpectator, UseCaptureTools,
                RunSpeedModifier, JumpYModifier, DiveSens, 
                DisableMonitorCheck, DisableAFK, GravityChange, DiveForce, DiveInAirForce,
#endif

            };

            if (!File.Exists(path))
            {
                MelonLogger.Msg($"[{base.GetType()}] Config not found, creating a new one...");
                foreach (var entry in EditableMenu)
                {
                    RegisterEntry(entry.Key, entry.Value.Category, entry.Value.DisplayName, entry.Value.Description, entry.Value.Value, entry.Value.AdditionalData, entry.Value.ValueType);
                }
            }
            else
            {
                var knownDefinitions = new Dictionary<string, MenuEntry>();

                MelonLogger.Msg($"[{base.GetType()}] Found existing config, it will be loaded");

                foreach (var entry in JsonConvert.DeserializeObject<List<MenuEntry>>(File.ReadAllText(path)))
                {
                    if (definitions.Contains(entry.InternalName))
                    {
                        knownDefinitions[entry.InternalName] = entry;
                    }
                }

                MelonLogger.Msg($"[{base.GetType()}] Loading config...");

                foreach (var entry2 in definitions)
                {

                    EditableMenu.TryGetValue(entry2, out var defaultEntry);
                    if (!knownDefinitions.ContainsKey(entry2))
                        RegisterEntry(entry2, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, defaultEntry.Value, defaultEntry.AdditionalData, defaultEntry.ValueType);
                    else
                    {
                        var knownEntry = knownDefinitions[entry2];
                        if (knownEntry.ValueType == MenuEntry.Type.Button)
                            RegisterEntry(knownEntry.InternalName, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, defaultEntry.Value, defaultEntry.AdditionalData, defaultEntry.ValueType);
                        else
                            RegisterEntry(knownEntry.InternalName, defaultEntry.Category, defaultEntry.DisplayName, defaultEntry.Description, knownEntry.Value, defaultEntry.AdditionalData, defaultEntry.ValueType);
                    }
                }
            }

            MelonLogger.Msg($"[{base.GetType()}] Config loaded!");

            var json = JsonConvert.SerializeObject(_entries);
            File.WriteAllText(path, json);

            CreateStaticConfig();

            MelonLogger.Msg($"[{base.GetType()}] All done!");
        }

        public void DeleteConfig()
        {
            string path = Path.Combine(NOTFGTools.AssetsDir, TargetCfgName);
            if (File.Exists(path))
                File.Delete(path);
            Application.Quit();
        }

        public void CreateStaticConfig()
        {
            Dictionary<string, (string Category, string DisplayName, string Description, object Value, object[] AdditionalData, MenuEntry.Type ValueType)> StaticConfig = new()
            {
#if CHEATS
                { ToFinish, (Gameplay_Cat, "cheat_entry_to_finish_title", "cheat_entry_to_finish_desc", "TeleportToFinish", null, MenuEntry.Type.Button) },
                { ToSafe, (Gameplay_Cat, "cheat_entry_to_safe_title", "cheat_entry_to_safe_desc", "ToSafeZone", null, MenuEntry.Type.Button) },
                { ToRandomPlayer, (Gameplay_Cat, "cheat_entry_to_random_player_title", "cheat_entry_to_random_player_desc", "TeleportToRandomPlayer", null, MenuEntry.Type.Button) },
                { HidePlayers, (Gameplay_Cat, "cheat_entry_toggle_players_title", "cheat_entry_toggle_players_desc", "TogglePlayers", null, MenuEntry.Type.Button) },
#endif
                { ForceMenu, (Gameplay_Cat, "cheat_entry_force_menu_title", "cheat_entry_force_menu_desc", "ForceMainMenu", null, MenuEntry.Type.Button) },
            };

            MelonLogger.Msg($"[{base.GetType()}] Creating static section...");

            foreach (var entry in StaticConfig)
                RegisterEntry(entry.Key, entry.Value.Category, entry.Value.DisplayName, entry.Value.Description, entry.Value.Value, entry.Value.AdditionalData, entry.Value.ValueType);
        }

        private List<MenuEntry> _entries = [];
        private List<MenuEntry> _changedEntries = [];

        public void RollSave()
        {
            string path = Path.Combine(NOTFGTools.AssetsDir, TargetCfgName);
            var json = JsonConvert.SerializeObject(_entries);
            File.WriteAllText(path, json);
            MelonLogger.Msg($"[{base.GetType()}] Saved config...");
        }

        public void RegisterEntry(string internalName, string category, string displayName, string desc, object value, object AdditionalData, MenuEntry.Type type)
        {
            _entries.Add(new MenuEntry {
                InternalName = internalName, 
                Category = category, 
                DisplayName = displayName, 
                Description = desc,
                Value = value, 
                AdditionalData = AdditionalData,
                ValueType = type 
            });
        }

        public void UpdateValue(string name, object value)
        {
            var entry = _entries.FirstOrDefault(e => e.InternalName == name);
            if (entry != null)
            {
                var changedEntry = _changedEntries.FirstOrDefault(e => e.InternalName == name);

                NOTFGTools.Instance.GUIUtil.TriggerPendingChanges(true);

                if (changedEntry == null)
                {
                    changedEntry = new MenuEntry
                    {
                        InternalName = entry.InternalName,
                        Description = entry.Description,
                        DisplayName= entry.DisplayName,
                        AdditionalData = entry.AdditionalData,
                        Category = entry.Category,
                        ValueType = entry.ValueType,
                        Value = value,
                    };
                    _changedEntries.Add(changedEntry);
                    return;
                }

                changedEntry.Value = value;
            }
        }

        public void RollChanges()
        {
            foreach (var changedEntry in _changedEntries)
            {
                var entry = _entries.FirstOrDefault(e => e.InternalName == changedEntry.InternalName);

                if (entry != null)
                    entry.Value = changedEntry.Value;
            }
            NOTFGTools.Instance.GUIUtil.TriggerPendingChanges(false);
            _changedEntries.Clear();
        }

        public T GetValue<T>(string name)
        {
            var entry = _entries.FirstOrDefault(e => e.InternalName == name);
            if (entry == null || entry.Value == null)
                return default;

            if (entry.Value is T value)
                return value;

            throw new InvalidCastException($"Tried to get type {typeof(T)} of value {name} but value's type is {entry.Value.GetType()}");
        }


        public MenuEntry GetEntry(string name)
        {
            return _entries.FirstOrDefault(e => e.InternalName == name);
        }

        public List<MenuEntry> GetAllEntries() => _entries;
    }
}
