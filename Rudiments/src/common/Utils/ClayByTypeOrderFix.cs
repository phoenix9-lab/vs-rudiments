using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace Rudiments.Utils
{
    /// <summary>
    /// Moves the <c>"*"</c> catch-all to the end of every <c>*ByType</c> dictionary on the clay
    /// assets, restoring vanilla's own convention after other mods have appended to them.
    ///
    /// <para><b>Why this exists.</b> <c>RegistryObjectType.solveByType</c> walks a <c>*ByType</c>
    /// dictionary in insertion order and takes the <b>first</b> wildcard that matches — the loop
    /// literally ends with <c>break; // Replaces for first matched key only</c>. Vanilla therefore
    /// always writes <c>"*"</c> last. But a JSON patch's <c>addmerge</c> appends, and there is no
    /// patch operation that inserts at the front of an object, so any mod that adds a specific key
    /// to a dictionary someone else has already given a <c>"*"</c> entry silently loses to that
    /// catch-all. Which of the two mods wins comes down to load order.</para>
    ///
    /// <para>That is exactly the situation for <c>clay-porcelain</c>: Clayworks ends its
    /// <c>texturesByType</c> on <c>game:itemtypes/resource/clay</c> with a <c>"*"</c> pointing at a
    /// texture that has no porcelain variant, and vanilla's own <c>dirtypot</c> does the same thing
    /// unaided. Rather than depend on load order — which the JSON patch system deliberately does not
    /// let you control — this normalises the dictionaries after all patching is done and before any
    /// type is resolved.</para>
    ///
    /// <para>It names no mod and reads no mod's assets. "Specific before catch-all" is what
    /// <c>solveByType</c> wants in every case, so the fix is correct whether or not anything else is
    /// installed. Nothing is rewritten unless a dictionary was actually out of order.</para>
    ///
    /// <para>Runs at <c>ExecuteOrder</c> 0.1: after <c>JsonPatchLoader</c> (0.05) and before
    /// <c>RegistryObjectTypeLoader</c> (0.2). Server side only, matching the type loader — block and
    /// item definitions are resolved server side and synced to clients.</para>
    /// </summary>
    public class ClayByTypeOrderFix : ModSystem
    {
        private const string CatchAll = "*";

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override double ExecuteOrder() => 0.1;

        public override void AssetsLoaded(ICoreAPI api)
        {
            int reordered = 0;

            reordered += FixOne(api, new AssetLocation("game", "itemtypes/resource/clay.json"));
            reordered += FixOne(api, new AssetLocation("game", "itemtypes/resource/clayworkitem.json"));

            foreach (IAsset asset in api.Assets.GetMany("blocktypes/clay/", "game"))
            {
                reordered += Fix(api, asset);
            }

            if (reordered > 0)
            {
                api.Logger.VerboseDebug("[{0}] Clay assets: moved the \"*\" catch-all last in {1} *ByType dictionar(y/ies) so specific variants resolve first.", Mod.Info.Name, reordered);
            }
        }

        private int FixOne(ICoreAPI api, AssetLocation location)
        {
            IAsset asset = api.Assets.TryGet(location);
            return asset == null ? 0 : Fix(api, asset);
        }

        private int Fix(ICoreAPI api, IAsset asset)
        {
            JToken root;
            try
            {
                root = JToken.Parse(asset.ToText());
            }
            catch
            {
                return 0;   // not our business to report on someone else's malformed asset
            }

            int moved = MoveCatchAllsLast(root);
            if (moved == 0) return 0;

            asset.Data = Encoding.UTF8.GetBytes(root.ToString());
            return moved;
        }

        private static int MoveCatchAllsLast(JToken token)
        {
            int moved = 0;

            if (token is JObject obj)
            {
                foreach (JProperty prop in obj.Properties().ToList())
                {
                    if (prop.Name.EndsWith("byType", System.StringComparison.OrdinalIgnoreCase)
                        && prop.Value is JObject dict)
                    {
                        JProperty catchAll = dict.Property(CatchAll);
                        if (catchAll != null && !ReferenceEquals(dict.Properties().Last(), catchAll))
                        {
                            JToken value = catchAll.Value;
                            catchAll.Remove();
                            dict.Add(CatchAll, value);
                            moved++;
                        }
                    }

                    moved += MoveCatchAllsLast(prop.Value);
                }
            }
            else if (token is JArray array)
            {
                foreach (JToken child in array) moved += MoveCatchAllsLast(child);
            }

            return moved;
        }
    }
}
