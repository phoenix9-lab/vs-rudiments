using System;
using Rudiments.Utils;
using Vintagestory.API.Common;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// The cheap, early, unreliable route to porcelain.
    ///
    /// An updraft kiln is hottest at the bottom next to the firemouths and cools toward the crown.
    /// That unevenness is why potters packed ware into fireclay saggars, and why losses stayed
    /// routine even in the industrial era — it is where the ~30% figure comes from, and it is not
    /// arbitrary. The downdraft beehive exists <i>because</i> of that problem: flame rises, deflects
    /// off the dome and is pulled back down through the ware to floor flues, and that even heat is
    /// the entire reason downdraft displaced updraft.
    ///
    /// So the two kilns are a real choice rather than a strict upgrade: this one is cheap and wastes
    /// your clay, the beehive is expensive and does not. A reason to build both, in that order.
    ///
    /// Updraft is a draft <i>direction</i>, not a temperature class — the Staffordshire bottle oven
    /// is an updraft kiln and fired biscuit and glost at 1200–1300 °C over two- and three-day
    /// firings. This one reaches porcelain temperature; what it cannot do is reach it evenly.
    /// </summary>
    public class BlockEntityUpdraftKiln : BlockEntityKilnBase
    {
        /// <summary>Matched with <c>Code.Path.Contains</c>, the bloomery's own one-line trick.</summary>
        private const string ChimneyCode = "updraftkilnchimney";

        protected override string InvKey => "updraftkiln";
        protected override string LangPrefix => "rudiments:updraftkiln";
        protected override int WareSlots => 8;

        /// <summary>Two ground-storage tiles: two large pieces, eight small ones, or an honest mix.</summary>
        protected override int WareCapacityUnits => 8;

        protected override float BurnHours => RudimentsModSystem.Config.UpdraftKilnBurnHours;

        /// <summary>
        /// Needs a chimney directly above and nothing else. Validated the way the bloomery validates
        /// its own chimney — a <c>Code.Path.Contains</c> on the block above — rather than with a
        /// MultiblockStructure. Flat ground; there is no slope requirement anywhere in this design.
        /// </summary>
        protected override bool CanIgnite(IPlayer byPlayer)
        {
            Block above = Api.World.BlockAccessor.GetBlock(Pos.UpCopy());
            if (above?.Code?.Path.Contains(ChimneyCode) == true) return true;

            Refuse(byPlayer, "nochimney", LangPrefix + "-nochimney");
            return false;
        }

        /// <summary>
        /// Same as the base for ordinary ware, but porcelain gets a per-item roll instead of the
        /// automatic shards its own combustible properties would give it. On a success the output is
        /// the raw block's own <c>beehivekiln["0"]</c> entry — the canonical perfect-firing result,
        /// read off the block so the mapping is never duplicated in code.
        /// </summary>
        protected override void FireSlot(ItemSlot slot)
        {
            ItemStack raw = slot.Itemstack;

            if (!WareTier.IsPorcelain(raw.Collectible))
            {
                base.FireSlot(slot);
                return;
            }

            ItemStack perfect = PerfectFiringOf(raw);
            if (perfect == null)
            {
                base.FireSlot(slot);   // no perfect-firing entry declared: fall back to the shards
                return;
            }

            double failChance = RudimentsModSystem.Config.UpdraftKilnPorcelainFailChance;
            int total = raw.StackSize;
            int survived = 0;
            for (int i = 0; i < total; i++)
            {
                if (Api.World.Rand.NextDouble() >= failChance) survived++;
            }

            int lost = total - survived;
            slot.Itemstack = null;

            if (survived > 0)
            {
                ItemStack good = perfect.Clone();
                good.StackSize = survived;
                WareTier.CarryGlaze(raw, good);
                slot.Itemstack = good;
            }

            if (lost > 0)
            {
                ItemStack shards = ClayWare.ShardsFor(Api.World, raw, lost);
                if (survived > 0) PutOrDrop(shards);
                else slot.Itemstack = shards;
            }

            slot.MarkDirty();
        }

        /// <summary>
        /// The block's own "fired in a perfectly sealed kiln" output — the <c>"0"</c> entry of its
        /// <c>beehivekiln</c> map, which is where porcelain's white result is declared. Returns null
        /// if the ware declares no such entry.
        /// </summary>
        private ItemStack PerfectFiringOf(ItemStack raw)
        {
            var kiln = raw.Collectible?.Attributes?["beehivekiln"];
            if (kiln?.Exists != true) return null;

            var entry = kiln["0"].AsObject<JsonItemStack>(null, raw.Collectible.Code.Domain);
            if (entry?.Resolve(Api.World, "rudiments updraft kiln", false) != true) return null;

            return entry.ResolvedItemstack;
        }
    }
}
