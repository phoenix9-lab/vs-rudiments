using System.Collections.Generic;
using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Thin router for any <see cref="BlockEntityKilnBase"/>. No GUI.
    ///
    /// Right-click loads ware and fuel and takes one slot back out. Lighting is the bloomery's
    /// gesture and not the kiln's own: hold a torch or firestarter, sneak, and hold right-click.
    /// That is what <c>IIgnitable</c> is for, and it is the only gesture that can work — a sneaking
    /// right-click is routed by the client to block placement or to the held item, in that order,
    /// and only reaches the block last and only when neither of those took it. A kiln that wanted
    /// sneak + right-click for itself was therefore unlightable with anything in your hand.
    ///
    /// A kiln whose blocktype declares a <c>chimneyCode</c> attribute also places that chimney on
    /// itself when you right-click it holding one, exactly as the bloomery does, and takes it down
    /// with it when broken. The chimney is <c>Unplaceable</c> for the same reason the bloomery's is:
    /// there is one correct place for it and this is how you get told so.
    ///
    /// One class serves both kilns because neither has any block-level behaviour that differs. It is
    /// registered under both <c>rudiments:BlockSmallBrickKiln</c> and <c>rudiments:BlockUpdraftKiln</c>
    /// so each blocktype still names the class it means.
    /// </summary>
    internal class BlockKiln : Block, IIgnitable
    {
        /// <summary>Bloomery parity: four seconds of held right-click before it catches.</summary>
        private const float IgniteSeconds = 4f;

        private string ChimneyCode => Attributes?["chimneyCode"]?.AsString(null);

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (TryPlaceChimney(world, byPlayer, blockSel)) return true;

            if (world.Side == EnumAppSide.Client) return true;

            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityKilnBase be) return false;

            return be.OnInteract(byPlayer);
        }

        /// <summary>
        /// Holding this kiln's chimney and clicking the kiln puts it on top. Lifted from
        /// <c>BlockBloomery</c> so the two behave identically, including doing nothing quietly when
        /// the space above is occupied.
        /// </summary>
        private bool TryPlaceChimney(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            string chimneyCode = ChimneyCode;
            if (chimneyCode == null) return false;

            ItemStack held = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (held?.Class != EnumItemClass.Block || !held.Collectible.Code.PathStartsWith(chimneyCode)) return false;

            BlockPos above = blockSel.Position.UpCopy();
            if (world.BlockAccessor.GetBlock(above).IsReplacableBy(held.Block))
            {
                held.Block.DoPlaceBlock(world, byPlayer, new BlockSelection { Position = above, Face = BlockFacing.UP }, held);
                if (Sounds != null) world.PlaySoundAt(Sounds.Place, blockSel.Position, 0.5, byPlayer);
                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
                }
            }

            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            string chimneyCode = ChimneyCode;
            if (chimneyCode != null)
            {
                Block above = world.BlockAccessor.GetBlock(pos.UpCopy());
                if (above?.Code?.Path == chimneyCode) above.OnBlockBroken(world, pos.UpCopy(), byPlayer, dropQuantityMultiplier);
            }

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        // ── IIgnitable ───────────────────────────────────────────────────────────────

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return EnumIgniteState.NotIgnitable;
        }

        /// <summary>
        /// Called on every tick of a held right-click with a torch or firestarter, on both sides, so
        /// it stays silent and side-effect free. Why it will not light is on the block's own info
        /// panel, which is where a player is already looking.
        /// </summary>
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            if (byEntity.World.BlockAccessor.GetBlockEntity(pos) is not BlockEntityKilnBase be) return EnumIgniteState.NotIgnitable;
            if (!be.CanLight(out _)) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > IgniteSeconds ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            // PreventDefault on both sides or the igniter drops a fire block on the face we just used.
            handling = EnumHandling.PreventDefault;
            if (byEntity.World.Side != EnumAppSide.Server) return;

            var be = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityKilnBase;
            be?.TryIgnite((byEntity as EntityPlayer)?.Player);
        }

        // ── Interaction help ─────────────────────────────────────────────────────────

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityKilnBase;
            if (be != null && be.IsBurning) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            var help = new List<WorldInteraction>
            {
                new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-kiln-load",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-kiln-ignite",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true).ToArray()
                }
            };

            // Cached: this runs every frame the player is looking at a kiln.
            ItemStack[] salts = ObjectCacheUtil.GetOrCreate(api, "rudimentsKilnSalts", () =>
            {
                var found = new List<ItemStack>();
                foreach (CollectibleObject obj in world.Collectibles)
                {
                    var stack = new ItemStack(obj);
                    if (BlockEntityKilnBase.IsKilnSalt(stack)) found.Add(stack);
                }
                return found.ToArray();
            });

            if (salts.Length > 0)
            {
                help.Add(new WorldInteraction
                {
                    ActionLangCode = "rudiments:blockhelp-kiln-salt",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = salts
                });
            }

            string chimneyCode = ChimneyCode;
            if (chimneyCode != null)
            {
                Block chimney = world.GetBlock(new AssetLocation(Code.Domain, chimneyCode));
                if (chimney != null)
                {
                    help.Add(new WorldInteraction
                    {
                        ActionLangCode = "rudiments:blockhelp-kiln-chimney",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = new[] { new ItemStack(chimney) }
                    });
                }
            }

            return help.ToArray().Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
