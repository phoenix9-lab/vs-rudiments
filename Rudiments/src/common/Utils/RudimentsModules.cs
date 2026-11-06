using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Rudiments.Utils
{
    /// <summary>
    /// The effective on/off state of each feature group, after requirements are applied.
    ///
    /// <para>Fixed once in <c>StartPre</c>. The server resolves it from <c>rudiments.json</c> and
    /// copies it into the world config, which JSON patch conditions read and which clients receive
    /// before their mods load. A client therefore follows the server's choice, and only falls back
    /// to its own config when there is no world config yet. All group checks go through here, never
    /// through <c>Config.XxxEnabled</c>, so a <c>/rudimentsreload</c> cannot apply half a change.</para>
    /// </summary>
    public static class RudimentsModules
    {
        public const string KeyPrefix = "Rudiments.";

        public static bool Fibre { get; private set; } = true;
        public static bool Nettle { get; private set; } = true;
        public static bool WoolCarding { get; private set; } = true;
        public static bool ReedSpread { get; private set; } = true;
        public static bool SeedsOnlyWhenMature { get; private set; } = true;
        public static bool WareTiers { get; private set; } = true;
        public static bool ClayBreakage { get; private set; } = true;
        public static bool ClaySeepage { get; private set; } = true;
        public static bool Kilns { get; private set; } = true;
        public static bool Porcelain { get; private set; } = true;
        public static bool Glazes { get; private set; } = true;

        /// <summary>Any clay group is on, so the shared class swaps on vanilla pottery stay attached.</summary>
        public static bool Ware => WareTiers || ClayBreakage || ClaySeepage || Glazes;

        /// <summary>Salt glaze is thrown into a kiln, so it needs both groups.</summary>
        public static bool SaltGlaze => Glazes && Kilns;

        /// <summary>"bloom", "legacy" or "off" — which flax drop table applies.</summary>
        public static string FlaxHarvest { get; private set; } = "bloom";

        private static readonly (string Name, System.Func<bool> Get, System.Action<bool> Set)[] Switches =
        {
            ("Fibre", () => Fibre, v => Fibre = v), ("Nettle", () => Nettle, v => Nettle = v),
            ("WoolCarding", () => WoolCarding, v => WoolCarding = v), ("ReedSpread", () => ReedSpread, v => ReedSpread = v),
            ("SeedsOnlyWhenMature", () => SeedsOnlyWhenMature, v => SeedsOnlyWhenMature = v),
            ("WareTiers", () => WareTiers, v => WareTiers = v), ("ClayBreakage", () => ClayBreakage, v => ClayBreakage = v),
            ("ClaySeepage", () => ClaySeepage, v => ClaySeepage = v), ("Kilns", () => Kilns, v => Kilns = v),
            ("Porcelain", () => Porcelain, v => Porcelain = v), ("Glazes", () => Glazes, v => Glazes = v),
        };

        /// <summary>
        /// A switch or derived group by name (as used in handbook tags and content groups), or null
        /// if there is no such name. The one place names map to switches.
        /// </summary>
        public static bool? IsOn(string name)
        {
            foreach (var (n, get, _) in Switches) if (n == name) return get();
            return name switch
            {
                "Ware" => Ware,
                "SaltGlaze" => SaltGlaze,
                _ => null,
            };
        }

        /// <summary>Resolves from a config, applying requirements. Returns warnings to log.</summary>
        public static List<string> Resolve(RudimentsConfig cfg)
        {
            var warnings = new List<string>();

            Fibre = cfg.FibreEnabled;
            Nettle = Require(cfg.NettleEnabled, Fibre, "NettleEnabled", "FibreEnabled", warnings);
            WoolCarding = cfg.WoolCardingEnabled;
            ReedSpread = cfg.ReedSpreadEnabled;
            SeedsOnlyWhenMature = cfg.SeedsOnlyWhenMature;
            WareTiers = cfg.WareTiersEnabled;
            ClayBreakage = cfg.ClayBreakageEnabled;
            ClaySeepage = cfg.ClaySeepageEnabled;
            Kilns = Require(cfg.KilnsEnabled, WareTiers, "KilnsEnabled", "WareTiersEnabled", warnings);
            Porcelain = Require(cfg.PorcelainEnabled, WareTiers, "PorcelainEnabled", "WareTiersEnabled", warnings);
            Glazes = cfg.GlazesEnabled;
            FlaxHarvest = !Fibre ? "off" : cfg.FlaxBloomHarvest ? "bloom" : "legacy";

            if (ClaySeepage && !WareTiers && !Glazes)
            {
                warnings.Add("ClaySeepageEnabled is on but neither WareTiersEnabled nor GlazesEnabled is, so nothing can ever be sealed and all liquid containers will seep.");
            }
            return warnings;
        }

        private static bool Require(bool wanted, bool requirement, string name, string needs, List<string> warnings)
        {
            if (wanted && !requirement) warnings.Add($"{name} is on but {needs} is off, so {name} is treated as off.");
            return wanted && requirement;
        }

        /// <summary>Server: publish the effective state for patches and clients.</summary>
        public static void Publish(ITreeAttribute worldConfig, RudimentsConfig cfg)
        {
            foreach (var (name, get, _) in Switches) worldConfig.SetBool(KeyPrefix + name, get());
            worldConfig.SetBool(KeyPrefix + "Ware", Ware);
            worldConfig.SetBool(KeyPrefix + "SaltGlaze", SaltGlaze);
            worldConfig.SetString(KeyPrefix + "FlaxHarvest", FlaxHarvest);
            worldConfig.SetBool(RudimentsModSystem.LeadPoisoningWorldKey, Glazes && cfg.LeadPoisoningEnabled);
        }

        /// <summary>Client: adopt the server's state. Keys that are missing keep the local value.</summary>
        public static void Adopt(ITreeAttribute worldConfig)
        {
            if (worldConfig == null || !worldConfig.HasAttribute(KeyPrefix + "Fibre")) return;

            foreach (var (name, get, set) in Switches) set(worldConfig.GetBool(KeyPrefix + name, get()));
            FlaxHarvest = worldConfig.GetString(KeyPrefix + "FlaxHarvest", FlaxHarvest);
        }

        private static string startupFingerprint;

        /// <summary>The settings that are only read at startup, as one comparable string.</summary>
        private static string Fingerprint(RudimentsConfig c) =>
            string.Join(",", c.FibreEnabled, c.NettleEnabled, c.WoolCardingEnabled, c.WareTiersEnabled,
                c.ClayBreakageEnabled, c.ClaySeepageEnabled, c.KilnsEnabled, c.PorcelainEnabled,
                c.GlazesEnabled, c.ReedSpreadEnabled, c.SeedsOnlyWhenMature, c.FlaxBloomHarvest);

        public static void RememberStartup(RudimentsConfig cfg) => startupFingerprint = Fingerprint(cfg);

        /// <summary>True if a reloaded config changed something that needs a restart to apply.</summary>
        public static bool NeedsRestart(RudimentsConfig cfg) =>
            startupFingerprint != null && startupFingerprint != Fingerprint(cfg);

        public static string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var (name, get, _) in Switches) sb.Append(name).Append('=').Append(get() ? "on" : "off").Append("  ");
            sb.Append("FlaxHarvest=").Append(FlaxHarvest);
            return sb.ToString();
        }
    }
}
