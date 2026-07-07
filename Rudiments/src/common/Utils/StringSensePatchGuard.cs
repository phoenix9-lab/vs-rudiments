using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace Rudiments.Utils
{
    /// <summary>
    /// Runs just before the json patch loader (ExecuteOrder 0.05) to defuse String Sense's flax
    /// crop-drop patch. String Sense replaces the vanilla stage-9 flaxfibers drop (index 2) with
    /// its flax strands, guarding against other flax-chain mods via inverted dependson entries
    /// (ageofflax, agricultureex) — but it doesn't know about Rudiments. Our crop-flax patch has
    /// already replaced dropsByType with a two-entry stage-9 array, so their index-2 path fails
    /// with a load [Error] every boot; and if it ever did apply, it would drop flaxstrands, which
    /// our stringsense compat disables. The patch loader deserializes every patch file before
    /// applying any, so this can't be fixed with a json patch — instead the patch asset is
    /// rewritten in memory here, adding an inverted rudiments dependson so the loader skips it
    /// as a clean unmet condition.
    /// </summary>
    public class StringSensePatchGuard : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side) => true;

        // Must run after assets exist but before JsonPatchLoader (0.05) deserializes them.
        public override double ExecuteOrder() => 0.04;

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);
            if (!api.ModLoader.IsModEnabled("stringsense")) return;

            IAsset asset = api.Assets.TryGet(new AssetLocation("stringsense", "patches/cropdrops.json"));
            if (asset == null) return;

            JArray patches;
            try
            {
                patches = JArray.Parse(asset.ToText());
            }
            catch
            {
                return;
            }

            int guarded = 0;
            foreach (JToken entry in patches)
            {
                if (entry is not JObject patch) continue;
                string file = patch["file"]?.ToString();
                if (file == null || !file.Contains("plant/crop/flax")) continue;

                // String Sense writes the key all-lowercase; the deserializer is case-insensitive,
                // so match whichever casing is present rather than adding a duplicate key.
                string key = patch["dependson"] != null ? "dependson" : "dependsOn";
                if (patch[key] is not JArray deps)
                {
                    deps = new JArray();
                    patch[key] = deps;
                }
                deps.Add(new JObject { ["modid"] = "rudiments", ["invert"] = true });
                guarded++;
            }

            if (guarded == 0) return;
            asset.Data = Encoding.UTF8.GetBytes(patches.ToString());
            api.Logger.Notification("[Rudiments] String Sense compat: disabled {0} flax crop-drop patch(es) — Rudiments already replaces the flax crop drops.", guarded);
        }
    }
}
