using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

namespace Rudiments.Utils
{
    /// <summary>
    /// Moves the <c>"*"</c> catch-all to the end of every <c>*ByType</c> dictionary on the assets
    /// this mod patches, restoring vanilla's own convention after anything has appended to them.
    ///
    /// <para><b>Why this exists.</b> <c>RegistryObjectType.solveByType</c> walks a <c>*ByType</c>
    /// dictionary in insertion order and takes the <b>first</b> wildcard that matches — the loop
    /// literally ends with <c>break; // Replaces for first matched key only</c>. Vanilla therefore
    /// always writes <c>"*"</c> last. But a JSON patch's <c>addmerge</c> appends, and there is no
    /// patch operation that inserts at the front of an object, so a specific key added to a
    /// dictionary that already ends in <c>"*"</c> silently loses to that catch-all — no error, no
    /// warning, the entry simply never applies.</para>
    ///
    /// <para>Three places in this mod hit that: <c>clay-porcelain</c>'s texture (Clayworks ends its
    /// <c>texturesByType</c> with a <c>"*"</c>), vanilla's own <c>dirtypot</c>, which does the same
    /// unaided, and the glaze applicator on <c>nugget-galena</c>, because <c>nugget.json</c>'s
    /// <c>behaviorsByType</c> ends in a catch-all too. All three would otherwise be a coin-toss on
    /// mod load order, which the JSON patch system deliberately gives no way to control.</para>
    ///
    /// <para>It names no mod and reads no mod's assets. "Specific before catch-all" is what
    /// <c>solveByType</c> wants in every case, so the normalisation is correct whether or not
    /// anything else is installed, and nothing is rewritten unless a dictionary was genuinely out of
    /// order.</para>
    ///
    /// <para>Runs at <c>ExecuteOrder</c> 0.1: after <c>JsonPatchLoader</c> (0.05) and before
    /// <c>RegistryObjectTypeLoader</c> (0.2). Server side only, matching the type loader — block and
    /// item definitions are resolved server side and synced to clients.</para>
    /// </summary>
    public class ByTypeCatchAllOrderFix : ModSystem
    {
        private const string CatchAll = "*";

        /// <summary>Individual assets we add a specific <c>*ByType</c> key to.</summary>
        private static readonly string[] SingleAssets =
        {
            "itemtypes/resource/clay.json",
            "itemtypes/resource/clayworkitem.json",
            "itemtypes/resource/nugget.json",
        };

        /// <summary>Whole trees we patch broadly enough that listing files would go stale.</summary>
        private static readonly string[] AssetTrees =
        {
            "blocktypes/clay/",
        };

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override double ExecuteOrder() => 0.1;

        public override void AssetsLoaded(ICoreAPI api)
        {
            int reordered = 0;

            foreach (string path in SingleAssets)
            {
                IAsset asset = api.Assets.TryGet(new AssetLocation("game", path));
                if (asset != null) reordered += Fix(asset);
            }

            foreach (string tree in AssetTrees)
            {
                foreach (IAsset asset in api.Assets.GetMany(tree, "game")) reordered += Fix(asset);
            }

            if (reordered > 0)
            {
                api.Logger.VerboseDebug("[{0}] Moved the \"*\" catch-all last in {1} *ByType dictionar(y/ies) so specific variants resolve first.", Mod.Info.Name, reordered);
            }
        }

        private static int Fix(IAsset asset)
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
