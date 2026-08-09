using Rudiments.SRC.Common.BlockEntities;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Scutch board — an upright notched plank tenoned into a block, the manual step between breaking
    /// and hatcheling. Load it with broken bundles, then strike them with a
    /// <see cref="Rudiments.SRC.Common.Items.ItemScutchSword"/> to scrape the woody boon off the fibre.
    /// All the state and maths live on <see cref="BlockEntityScutchBoard"/>; this class only routes
    /// interactions to it.
    /// </summary>
    internal class BlockScutchBoard : Block
    {
        /// <summary>Brown woody shive particles, thrown while the blade still has boon to clear.
        /// Driven by the scutching sword, one burst per stroke.</summary>
        public static SimpleParticleProperties ShiveParticles = new();

        /// <summary>Pale fibre fluff, thrown once the worked side is clean and the blade is biting the
        /// line itself — the sensory tell that doubles as the cue to flip.</summary>
        public static SimpleParticleProperties TowParticles = new();

        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            ShiveParticles = new SimpleParticleProperties(
                8, 16,
                ColorUtil.ToRgba(255, 180, 160, 100),
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.8f, -0.2f, -0.8f), new Vec3f(0.8f, 0.9f, 0.8f),
                0.9f, 0.8f, 0.08f, 0.18f,
                EnumParticleModel.Quad);
            ShiveParticles.WithTerrainCollision = true;
            ShiveParticles.AddPos.Set(0.3f, 0, 0.3f);
            ShiveParticles.AddQuantity = 16;
            ShiveParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255);

            TowParticles = new SimpleParticleProperties(
                4, 10,
                ColorUtil.ToRgba(255, 214, 208, 168),
                new Vec3d(), new Vec3d(),
                new Vec3f(-0.4f, 0.05f, -0.4f), new Vec3f(0.4f, 0.5f, 0.4f),
                1.2f, 0.05f, 0.1f, 0.22f,
                EnumParticleModel.Quad);
            TowParticles.WithTerrainCollision = true;
            TowParticles.AddPos.Set(0.3f, 0, 0.3f);
            TowParticles.AddQuantity = 8;
            TowParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -160);

            if (api is not ICoreClientAPI) return;

            interactions = ObjectCacheUtil.GetOrCreate(api, "scutchBoardInteractions", () =>
            {
                List<ItemStack> brokenStacks = new List<ItemStack>();
                List<ItemStack> swordStacks = new List<ItemStack>();

                foreach (Item item in api.World.Items)
                {
                    if (item?.Code == null || item.Code.Domain != "rudiments") continue;
                    if (item.Variant?["type"] == "broken") brokenStacks.Add(new ItemStack(item));
                    if (item.Code.Path == "scutchsword") swordStacks.Add(new ItemStack(item));
                }

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutchboard-blockhelp-load",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = brokenStacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutchboard-blockhelp-strike",
                        MouseButton = EnumMouseButton.Left,
                        Itemstacks = swordStacks.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutchboard-blockhelp-flip",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak"
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "rudiments:scutchboard-blockhelp-collect",
                        MouseButton = EnumMouseButton.Right,
                        RequireFreeHand = true
                    }
                };
            });
        }

        /// <summary>
        /// Fetches the board's block entity, creating one on the fly if this board predates the
        /// entityClass. Adding entityClass to the block JSON does not retroactively spawn block
        /// entities for boards already placed in existing worlds, and without this the block would
        /// silently do nothing until re-placed.
        /// </summary>
        public static BlockEntityScutchBoard GetOrSpawnBE(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityScutchBoard be) return be;
            if (world.Side != EnumAppSide.Server) return null;

            world.BlockAccessor.SpawnBlockEntity("rudiments:BlockEntityScutchBoard", pos);
            return world.BlockAccessor.GetBlockEntity(pos) as BlockEntityScutchBoard;
        }

        private static bool IsBrokenBundle(ItemStack stack) =>
            stack?.Collectible?.Code != null
            && stack.Collectible.Code.Domain == "rudiments"
            && stack.Collectible.Variant?["type"] == "broken";

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityScutchBoard be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityScutchBoard;

            // A board placed before this version has no block entity yet. Only the server can create
            // one; the client claims the click so it isn't spent placing a block against the board,
            // and picks the real state up on the next sync.
            if (be == null)
            {
                if (world.Side != EnumAppSide.Server) return true;
                be = GetOrSpawnBE(world, blockSel.Position);
                if (be == null) return base.OnBlockInteractStart(world, byPlayer, blockSel);
            }

            ItemStack held = byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            bool sneaking = byPlayer.Entity.Controls.ShiftKey;

            // Sneak turns the bundle — but never at the cost of building against the board, so a
            // placeable block in hand still wins.
            if (sneaking && held?.Block == null)
            {
                if (be.IsEmpty) return base.OnBlockInteractStart(world, byPlayer, blockSel);
                return world.Side == EnumAppSide.Client || be.Flip(byPlayer);
            }

            if (held == null)
            {
                if (be.IsEmpty) return base.OnBlockInteractStart(world, byPlayer, blockSel);
                return world.Side == EnumAppSide.Client || be.Collect(byPlayer);
            }

            if (!IsBrokenBundle(held)) return base.OnBlockInteractStart(world, byPlayer, blockSel);

            // Claim the interaction on both sides even when the load is refused (wrong quality, full,
            // already part-scutched) — the block entity raises the in-game error itself.
            if (world.Side == EnumAppSide.Client) return true;
            be.TryLoad(byPlayer);
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            WorldInteraction[] baseInteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            return interactions == null ? baseInteractions : interactions.Append(baseInteractions);
        }
    }
}
