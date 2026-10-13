using HarmonyLib;
using Bindito.Core;
using ModSettings.Common;
using ModSettings.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Timberborn.BlockingSystem;
using Timberborn.BlockObjectModelSystem;
using Timberborn.GameWaterSourceSystem;
using Timberborn.HazardousWeatherSystem;
using Timberborn.MechanicalSystem;
using Timberborn.Modding;
using Timberborn.ModManagerScene;
using Timberborn.SettingsSystem;
using Timberborn.WaterSourceSystem;
using Timberborn.SingletonSystem;
using Timberborn.WorkSystem;

namespace TonWolfe.AllWeatherDrill
{
    public class ModStarter : IModStarter
    {
        void IModStarter.StartMod(IModEnvironment modEnvironment)
        {
            new Harmony("TonWolfe.AllWeatherDrill").PatchAll();
        }
    }

    [Context("MainMenu")]
    [Context("Game")]
    [Context("MapEditor")]
    public class ModMenuConfig : Configurator
    {
        protected override void Configure() { Bind<MSettings>().AsSingleton(); }
    }

    public class MSettings : ModSettingsOwner, IUnloadableSingleton
    {
        public static MSettings Instance { get; private set; }
        protected override string ModId { get; } = "TonWolfe.AllWeatherDrill";

        public ModSetting<float> NormalWeatherScale { get; } = new ModSetting<float>(0.7f, ModSettingDescriptor.CreateLocalized("ModSetting.TWAllWeatherDrill.NormalWeatherScale").SetLocalizedTooltip("ModSetting.TWAllWeatherDrill.NormalWeatherScaleDesc"));
        public ModSetting<float> BadtideWeatherScale { get; } = new ModSetting<float>(0.6f, ModSettingDescriptor.CreateLocalized("ModSetting.TWAllWeatherDrill.BadtideWeatherScale").SetLocalizedTooltip("ModSetting.TWAllWeatherDrill.BadtideWeatherScaleDesc"));
        public ModSetting<float> DroughtWeatherScale { get; } = new ModSetting<float>(0.4f, ModSettingDescriptor.CreateLocalized("ModSetting.TWAllWeatherDrill.DroughtWeatherScale").SetLocalizedTooltip("ModSetting.TWAllWeatherDrill.DroughtWeatherScaleDesc"));
        public ModSetting<float> OtherWeatherScale { get; } = new ModSetting<float>(0.6f, ModSettingDescriptor.CreateLocalized("ModSetting.TWAllWeatherDrill.OtherWeatherScale").SetLocalizedTooltip("ModSetting.TWAllWeatherDrill.OtherWeatherScaleDesc"));
        public ModSetting<float> PowerScale { get; } = new ModSetting<float>(1.5f, ModSettingDescriptor.CreateLocalized("ModSetting.TWAllWeatherDrill.PowerScale").SetLocalizedTooltip("ModSetting.TWAllWeatherDrill.PowerScaleDesc"));

        public MSettings(ISettings settings, ModSettingsOwnerRegistry registry, ModRepository repository)
            : base(settings, registry, repository) { }

        protected override void OnAfterLoad()
        {
            base.OnAfterLoad();
            Instance = this;
        }

        public void Unload() { Instance = null; }
    }

    internal enum DrillWeatherKind { Temperate, Drought, Badtide, Other }

    internal static class CurrentWeatherResolver
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static DrillWeatherKind Resolve(HazardousWeatherObserver observer)
        {
            // The game can query the drill while singleton initialization is still
            // in progress. Do not turn a missing observer into a game crash.
            if (observer == null) return DrillWeatherKind.Other;

            object weatherService = null;
            try { weatherService = GetMember(observer, "_weatherService"); }
            catch { /* A changed private field must not crash the simulation. */ }
            bool moddableService = IsModdableWeatherService(weatherService);
            string baseId;
            HashSet<string> mods;
            if (TryReadModdableWeather(observer, out baseId, out mods))
            {
                // Hazardous concepts win regardless of whether they are the base weather
                // or a Moddable Weathers modifier.
                if (Is(baseId, "BadtideWeather", "Badtide") || mods.Contains("Badtide")) return DrillWeatherKind.Badtide;
                if (Is(baseId, "DroughtWeather", "Drought") || mods.Contains("Drought")) return DrillWeatherKind.Drought;

                // Rain, Surprisingly Refreshing/Refreshing and Monsoon are all
                // treated as temperate for drill scaling. Monsoon + Badtide is
                // already caught by the Badtide rule above.
                bool knownTemperateBase = Is(baseId,
                    "TemperateWeather", "Temperate",
                    "Rain",
                    "Monsoon",
                    "SurprisinglyRefreshing", "Refreshing");

                bool onlyTemperateModifiers = true;
                foreach (string mod in mods)
                {
                    if (!Is(mod, "Rain", "Monsoon", "Refreshing"))
                    {
                        onlyTemperateModifiers = false;
                        break;
                    }
                }

                if (knownTemperateBase && onlyTemperateModifiers) return DrillWeatherKind.Temperate;
                return DrillWeatherKind.Other;
            }

            // A partially loaded / updated Moddable Weathers service is not
            // necessarily Temperate. Its current stage may be unavailable while
            // restoring a save. Fail closed to the configured Other multiplier
            // rather than silently using Temperate.
            if (moddableService) return DrillWeatherKind.Other;

            if (observer.IsBadtideWeather) return DrillWeatherKind.Badtide;
            if (observer.IsDroughtWeather) return DrillWeatherKind.Drought;

            bool hazardous;
            if (TryBool(GetMember(observer, "_weatherService"), "IsHazardousWeather", out hazardous) && hazardous)
                return DrillWeatherKind.Other;

            return DrillWeatherKind.Temperate;
        }

        static bool TryReadModdableWeather(HazardousWeatherObserver observer, out string baseId, out HashSet<string> mods)
        {
            baseId = null;
            mods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                object weatherService = GetMember(observer, "_weatherService");
                if (weatherService == null) return false;

                object cycle = GetMember(weatherService, "WeatherDayService");
                if (cycle == null) return false;

                baseId = ReadId(GetMember(cycle, "CurrentWeather"));
                if (string.IsNullOrEmpty(baseId)) return false;

                // Upstream WeatherCycleService exposes an ImmutableArray of
                // modifiers. It implements IEnumerable, including when boxed.
                // A missing/incompatible API must NOT be interpreted as "no
                // modifiers": that loses Badtide/Drought precedence on reload.
                IEnumerable currentMods = GetMember(cycle, "CurrentWeatherModifiers") as IEnumerable;
                if (currentMods == null) return false;
                foreach (object mod in currentMods)
                {
                    string id = ReadId(mod);
                    if (string.IsNullOrEmpty(id)) return false;
                    mods.Add(id);
                }
                return true;
            }
            catch
            {
                baseId = null;
                mods.Clear();
                return false;
            }
        }

        static bool Is(string value, params string[] values)
        {
            if (string.IsNullOrEmpty(value)) return false;
            foreach (string candidate in values)
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // Moddable Weathers 11.x replaces WeatherService with this subclass.
        // Use the runtime type name rather than a compiled dependency; the
        // optional mod can be absent, disabled or upgraded independently.
        static bool IsModdableWeatherService(object weatherService)
        {
            if (weatherService == null) return false;
            for (Type t = weatherService.GetType(); t != null; t = t.BaseType)
                if (t.FullName == "ModdableWeathers.Services.ModdableWeatherService")
                    return true;
            return false;
        }

        static string ReadId(object obj) { return GetMember(obj, "Id") as string; }

        static bool TryBool(object obj, string name, out bool value)
        {
            value = false;
            object member = GetMember(obj, name);
            if (member is bool)
            {
                value = (bool)member;
                return true;
            }
            return false;
        }

        static object GetMember(object obj, string name)
        {
            if (obj == null) return null;
            // GetField/GetProperty on the derived type do not reliably see
            // private members declared on base types (e.g. _weatherService).
            // Walk the inheritance chain explicitly, never assuming the
            // Moddable Weathers replacement uses the same declaring type.
            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                PropertyInfo prop = type.GetProperty(name, Flags | BindingFlags.DeclaredOnly);
                if (prop != null) return prop.GetValue(obj, null);
                FieldInfo field = type.GetField(name, Flags | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(obj);
            }
            return null;
        }
    }

    [HarmonyPatch]
    public static class AllWeatherDrillPatches
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UndergroundWaterSourceDrill), nameof(UndergroundWaterSourceDrill.OnEnterFinishedState))]
        [HarmonyAfter("ModdableTimberborn")]
        [HarmonyPriority(Priority.Last)]
        public static void OnFinished(
            UndergroundWaterSourceDrill __instance,
            ref bool ____isBlocking,
            BlockableObject ____blockableObject,
            UnderlyingWaterSource ____underlyingWaterSource,
            MechanicalNode ____mechanicalNode)
        {
            ____isBlocking = false;
            ____blockableObject.Unblock(__instance);
            PrepareWaterSource(____underlyingWaterSource);

            if (____mechanicalNode != null && MSettings.Instance != null)
                SetInputMultiplierCompat(____mechanicalNode, Clamp(MSettings.Instance.PowerScale.Value));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UndergroundWaterSourceDrill), nameof(UndergroundWaterSourceDrill.OnHazardousWeatherStarted))]
        [HarmonyBefore("ModdableTimberborn")]
        [HarmonyPriority(Priority.First)]
        public static bool OnHazard(UnderlyingWaterSource ____underlyingWaterSource, ref bool ____isBlocking)
        {
            PrepareWaterSource(____underlyingWaterSource);
            ____isBlocking = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UndergroundWaterSourceDrill), nameof(UndergroundWaterSourceDrill.GetStrengthModifier))]
        [HarmonyBefore("ModdableTimberborn")]
        [HarmonyPriority(Priority.First)]
        public static bool GetStrength(
            ref float __result,
            MechanicalNode ____mechanicalNode,
            HazardousWeatherObserver ____hazardousWeatherObserver,
            UnderlyingWaterSource ____underlyingWaterSource)
        {
            // Moddable Weathers' StopWaterIfBlockedOrBadwater prefix suppresses
            // production from contaminated sources. Our first-priority Harmony
            // prefix returns false and could skip that check. Preserve the
            // contamination gate here instead of relying on another prefix.
            if (IsWaterContaminated(____underlyingWaterSource))
            {
                __result = 0f;
                return false;
            }

            // Current Timberborn no longer stores a PausableBuilding on the drill.
            // MechanicalNode.ActiveAndPowered is the game's own gate for whether
            // the drill may produce anything (covers paused/unpowered/inactive).
            if (____mechanicalNode == null || !____mechanicalNode.ActiveAndPowered || MSettings.Instance == null)
            {
                __result = 0f;
                return false;
            }

            float factor = ScaleFor(CurrentWeatherResolver.Resolve(____hazardousWeatherObserver));
            __result = ____mechanicalNode.PowerEfficiency * Clamp(factor);
            return false;
        }

        static bool IsWaterContaminated(UnderlyingWaterSource underlying)
        {
            if (underlying == null || underlying.WaterSource == null) return false;
            // Avoid an additional hard assembly/method dependency. Source API:
            // Moddable Weathers checks WaterSource.Contamination > 0.
            try
            {
                return ReadWaterContamination(underlying.WaterSource);
            }
            catch { return false; }
        }

        static bool ReadWaterContamination(object waterSource)
        {
            for (Type t = waterSource.GetType(); t != null; t = t.BaseType)
            {
                var property = t.GetProperty("Contamination", Flags | BindingFlags.DeclaredOnly);
                if (property == null) continue;
                object value = property.GetValue(waterSource, null);
                if (value is float) return (float)value > 0f;
                if (value is double) return (double)value > 0d;
                if (value is int) return (int)value > 0;
                return false;
            }
            return false;
        }

        static float ScaleFor(DrillWeatherKind weather)
        {
            MSettings s = MSettings.Instance;
            switch (weather)
            {
                case DrillWeatherKind.Badtide: return s.BadtideWeatherScale.Value;
                case DrillWeatherKind.Drought: return s.DroughtWeatherScale.Value;
                case DrillWeatherKind.Other: return s.OtherWeatherScale.Value;
                default: return s.NormalWeatherScale.Value;
            }
        }

        static float Clamp(float value) { return Math.Min(20f, Math.Max(0f, value)); }

        static void SetInputMultiplierCompat(MechanicalNode node, float value)
        {
            try
            {
                MethodInfo method = node.GetType().GetMethod("SetInputMultiplier", Flags, null, new Type[] { typeof(float) }, null);
                if (method != null)
                {
                    method.Invoke(node, new object[] { value });
                    return;
                }

                FieldInfo field = node.GetType().GetField("_inputMultiplier", Flags);
                if (field != null) field.SetValue(node, value);
            }
            catch { }
        }

        static void PrepareWaterSource(UnderlyingWaterSource underlying)
        {
            if (underlying == null) return;

            // DisableDroughtInfluence() also hides the underlying aquifer model.
            // All Weather Drill intentionally disables drought influence but must
            // immediately restore the aquifer's visible model.
            underlying.DisableDroughtInfluence();

            if (underlying.WaterSource != null)
            {
                BlockObjectModel model = underlying.WaterSource.GetComponent<BlockObjectModel>();
                if (model != null)
                    model.UnhideFullModelPermanently();
            }
        }
    }
}
