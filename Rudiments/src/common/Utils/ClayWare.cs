using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Everything that both breakage paths — per-use wear (<c>CollectibleBehaviorFragile</c>) and
    /// impact on landing (<c>EntityBehaviorClayFragile</c>) — need to agree on: what counts as
    /// breakable fired clay, and what a break looks and sounds like.
    ///
    /// The "is it breakable pottery" test is three predicates, not a list of item codes, so it
    /// covers Clayworks, Primitive Survival and anything else with no per-mod patching:
    ///
    ///   1. the block material is <c>Ceramic</c>            — 60 vanilla blocktypes, all Clayworks ware
    ///   2. it is not greenware                             — greenware deforms, it does not shatter
    ///   3. it is not flagged <c>unbreakableOnDrop</c>       — bricks, tiles, shingles, kiln parts
    ///
    /// Predicate 2 is generic on purpose: greenware is exactly "still fire-smeltable", which no
    /// fired ware is. A hardcoded list of raw ware codes would miss every modded greenware.
    /// </summary>
    public static class ClayWare
    {
        public static readonly AssetLocation BreakSound = new AssetLocation("game", "sounds/block/ceramicbreak");

        private const string ShardNormal = "game:clayshattered-normal";
        private const string ShardSingle = "game:clayshattered-singlecenter";

        /// <summary>Item attribute a blocktype sets to opt out of drop breakage entirely.</summary>
        public static bool IsUnbreakableOnDrop(ItemStack stack)
        {
            return stack?.ItemAttributes?["rudiments"]?["unbreakableOnDrop"].AsBool(false) == true;
        }

        /// <summary>
        /// Unfired clay: still has fire-type combustible properties, i.e. it is waiting for a kiln.
        /// Read off the JSON-resolved <c>CombustibleProps</c> rather than
        /// <c>GetCombustibleProperties</c>, because the glaze override in Parcel 2 clones the latter
        /// per stack and we only want the static "is this greenware" fact.
        /// </summary>
        public static bool IsGreenware(ItemStack stack)
        {
            return stack?.Collectible?.CombustibleProps?.SmeltingType == EnumSmeltType.Fire;
        }

        /// <summary>Fired pottery that is allowed to shatter. See the class summary for the three predicates.</summary>
        public static bool IsBreakablePottery(ItemStack stack)
        {
            if (stack?.Block == null) return false;                          // items (bricks, tiles) are exempt for free
            if (stack.Block.BlockMaterial != EnumBlockMaterial.Ceramic) return false;
            if (IsGreenware(stack)) return false;
            if (IsUnbreakableOnDrop(stack)) return false;
            return true;
        }

        /// <summary>
        /// The shard block that matches how this item sat on the ground — a big single-centre item
        /// leaves the single-centre shard pile, everything else leaves the quadrant pile.
        /// </summary>
        public static ItemStack ShardsFor(IWorldAccessor world, ItemStack stack, int quantity)
        {
            var props = stack?.Collectible?.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
            bool single = props != null && props.Layout == EnumGroundStorageLayout.SingleCenter;

            Block shard = world.GetBlock(new AssetLocation(single ? ShardSingle : ShardNormal));
            if (shard == null) return null;

            return new ItemStack(shard, GameMath.Clamp(quantity, 1, shard.MaxStackSize));
        }

        /// <summary>
        /// Solid contents of a broken vessel, so nothing edible is ever voided. Liquid portions are
        /// deliberately not returned — a shattered jug spills its water on the ground, it does not
        /// hand you back loose water items.
        /// </summary>
        public static List<ItemStack> SpillableContents(IWorldAccessor world, ItemStack stack)
        {
            var spill = new List<ItemStack>();
            if (stack?.Collectible is not BlockContainer container) return spill;

            ItemStack[] contents = container.GetContents(world, stack);
            if (contents == null) return spill;

            foreach (ItemStack content in contents)
            {
                if (content == null || content.StackSize <= 0) continue;
                if (BlockLiquidContainerBase.GetContainableProps(content) != null) continue;   // liquid — spills away
                spill.Add(content.Clone());
            }

            return spill;
        }

        /// <summary>
        /// The break itself: vanilla's ceramic-break sound, two bursts of block-broken particles
        /// (vanilla does it twice — see BEToolMold.ShatterMold), the spilled contents and the
        /// shards, all dropped at <paramref name="pos"/>. Server side only; caller checks.
        /// </summary>
        public static void Shatter(IWorldAccessor world, Vec3d pos, ItemStack brokenStack, int quantity)
        {
            world.PlaySoundAt(BreakSound, pos.X, pos.Y, pos.Z, null, true, 16f);

            BlockPos blockPos = pos.AsBlockPos;
            brokenStack.Block?.SpawnBlockBrokenParticles(blockPos);
            brokenStack.Block?.SpawnBlockBrokenParticles(blockPos);

            foreach (ItemStack spilled in SpillableContents(world, brokenStack))
            {
                world.SpawnItemEntity(spilled, pos);
            }

            ItemStack shards = ShardsFor(world, brokenStack, quantity);
            if (shards != null) world.SpawnItemEntity(shards, pos);
        }
    }
}
