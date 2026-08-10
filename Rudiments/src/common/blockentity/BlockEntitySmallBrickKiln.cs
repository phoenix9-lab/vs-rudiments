using System;
using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// A single-block brick kiln: four ware slots and one fuel slot, hot enough to vitrify.
    ///
    /// It sits between the pit kiln and the beehive. The pit kiln is free and gives you porous
    /// earthenware; the beehive is a multi-block build. This is the rung in between — you make it
    /// out of the bricks the pit kiln already gave you, so nothing about the vanilla progression
    /// changes to reach it.
    ///
    /// The fuel gate is the bloomery's, verbatim: <c>BurnTemperature &gt;= 1200 &amp;&amp;
    /// BurnDuration &gt; 30</c>. That admits charcoal, coke, bituminous and anthracite coal, and
    /// refuses lignite, peat and every wood. Cold fuel is refused **at insertion** with an ingame
    /// error rather than accepted and then disappointing, which is how every vanilla fuel gate
    /// behaves — nothing in vanilla lets a player wait ten hours for a worse result.
    ///
    /// Ware admission is the generic <c>SmeltingType == Fire</c> contract rather than a list of
    /// codes, so Clayworks greenware and any other mod's fires here with no compat file at all.
    /// </summary>
    public class BlockEntitySmallBrickKiln : BlockEntity
    {
        public const int WareSlots = 4;
        public const int FuelSlotIndex = WareSlots;

        protected InventoryGeneric inventory;

        /// <summary>Total game hours at which the current burn finishes. 0 = not lit.</summary>
        protected double burnUntilTotalHours;
        protected bool burning;

        private AssetLocation insertSound;

        protected virtual string InvKey => "smallbrickkiln";
        protected virtual int SlotCount => WareSlots + 1;
        protected virtual float BurnHours => RudimentsModSystem.Config.SmallBrickKilnBurnHours;
        protected virtual string LangPrefix => "rudiments:smallbrickkiln";

        public ItemSlot FuelSlot => inventory[FuelSlotIndex];
        public bool IsBurning => burning;

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null) inventory = new InventoryGeneric(SlotCount, InvKey + "-" + Pos, api);

            base.Initialize(api);
            inventory.LateInitialize(InvKey + "-" + Pos, api);

            insertSound = new AssetLocation("game", "sounds/block/ceramicplace");
            RegisterGameTickListener(OnGameTick, 1000);
        }

        // ── Admission ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Anything a kiln is supposed to fire. The generic contract rather than a code list, so
        /// third-party greenware qualifies automatically.
        /// </summary>
        public static bool IsFirableWare(ItemStack stack)
        {
            return stack?.Collectible?.CombustibleProps?.SmeltingType == EnumSmeltType.Fire;
        }

        /// <summary>The bloomery's fuel gate. See the class summary for what it admits.</summary>
        public static bool IsHotFuel(ItemStack stack)
        {
            CombustibleProperties props = stack?.Collectible?.CombustibleProps;
            if (props == null) return false;
            return props.BurnTemperature >= RudimentsModSystem.Config.KilnMinFuelTemperature && props.BurnDuration > 30;
        }

        /// <summary>True for fuel that burns but nowhere near hot enough — firewood, peat, lignite.</summary>
        private static bool IsColdFuel(ItemStack stack)
        {
            CombustibleProperties props = stack?.Collectible?.CombustibleProps;
            return props != null && props.BurnTemperature > 0 && props.BurnDuration > 0;
        }

        // ── Interaction ──────────────────────────────────────────────────────────────

        public virtual bool OnInteract(IPlayer byPlayer)
        {
            if (burning)
            {
                Refuse(byPlayer, "burning", LangPrefix + "-burning");
                return true;
            }

            ItemSlot heldSlot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            ItemStack held = heldSlot?.Itemstack;

            // Empty hand: unload everything, ware first.
            if (held == null) return TryUnload(byPlayer);

            if (IsHotFuel(held)) return TryInsert(byPlayer, heldSlot, FuelSlot);
            if (IsFirableWare(held)) return TryInsertWare(byPlayer, heldSlot);

            if (IsColdFuel(held))
            {
                Refuse(byPlayer, "coldfuel", LangPrefix + "-coldfuel");
                return true;
            }

            Refuse(byPlayer, "notware", LangPrefix + "-notware");
            return true;
        }

        private bool TryInsertWare(IPlayer byPlayer, ItemSlot heldSlot)
        {
            for (int i = 0; i < WareSlots; i++)
            {
                ItemSlot slot = inventory[i];
                if (slot.Empty || slot.Itemstack.Collectible == heldSlot.Itemstack.Collectible)
                {
                    if (TryInsert(byPlayer, heldSlot, slot)) return true;
                }
            }

            Refuse(byPlayer, "full", LangPrefix + "-full");
            return true;
        }

        private bool TryInsert(IPlayer byPlayer, ItemSlot heldSlot, ItemSlot target)
        {
            int moved = heldSlot.TryPutInto(Api.World, target, byPlayer.Entity.Controls.CtrlKey ? heldSlot.StackSize : 1);
            if (moved <= 0) return false;

            heldSlot.MarkDirty();
            target.MarkDirty();
            MarkDirty(true);
            Api.World.PlaySoundAt(insertSound, Pos, 0, byPlayer, false);
            return true;
        }

        private bool TryUnload(IPlayer byPlayer)
        {
            bool any = false;
            for (int i = 0; i < inventory.Count; i++)
            {
                ItemSlot slot = inventory[i];
                if (slot.Empty) continue;
                if (!byPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack.Clone())) continue;
                slot.Itemstack = null;
                slot.MarkDirty();
                any = true;
            }

            if (any)
            {
                MarkDirty(true);
                Api.World.PlaySoundAt(insertSound, Pos, 0, byPlayer, false);
            }
            return any;
        }

        /// <summary>Light it. Refuses with a reason rather than failing silently.</summary>
        public virtual bool TryIgnite(IPlayer byPlayer)
        {
            if (burning) return false;

            if (FuelSlot.Empty || !IsHotFuel(FuelSlot.Itemstack))
            {
                Refuse(byPlayer, "nofuel", LangPrefix + "-nofuel");
                return false;
            }

            bool anyWare = false;
            for (int i = 0; i < WareSlots; i++) anyWare |= !inventory[i].Empty;
            if (!anyWare)
            {
                Refuse(byPlayer, "noware", LangPrefix + "-noware");
                return false;
            }

            if (!CanIgnite(byPlayer)) return false;

            burning = true;
            burnUntilTotalHours = Api.World.Calendar.TotalHours + BurnHours;
            FuelSlot.TakeOut(1);
            FuelSlot.MarkDirty();
            MarkDirty(true);
            return true;
        }

        /// <summary>Hook for subclasses that need more than fuel and ware — the updraft kiln's chimney.</summary>
        protected virtual bool CanIgnite(IPlayer byPlayer) => true;

        // ── Firing ───────────────────────────────────────────────────────────────────

        private void OnGameTick(float dt)
        {
            if (Api.Side != EnumAppSide.Server || !burning) return;
            if (Api.World.Calendar.TotalHours < burnUntilTotalHours) return;

            burning = false;
            burnUntilTotalHours = 0;

            for (int i = 0; i < WareSlots; i++)
            {
                if (!inventory[i].Empty) FireSlot(inventory[i]);
            }

            MarkDirty(true);
        }

        /// <summary>
        /// Converts one slot of greenware. The output is read off the input block's own combustible
        /// properties — never a hardcoded ware code — which is why one code path serves vanilla ware,
        /// Clayworks ware and anything else. Porcelain needs no special case here: its own
        /// smeltedStack is already shards, because a brick kiln cannot give it the sealed soak it
        /// wants.
        /// </summary>
        protected virtual void FireSlot(ItemSlot slot)
        {
            ItemStack raw = slot.Itemstack;
            CombustibleProperties props = raw.Collectible.GetCombustibleProperties(Api.World, raw, null);
            ItemStack fired = props?.SmeltedStack?.ResolvedItemstack;
            if (fired == null) return;

            ItemStack result = fired.Clone();
            result.StackSize = raw.StackSize / Math.Max(1, props.SmeltedRatio);
            if (result.StackSize <= 0) result.StackSize = 1;

            // Glaze rides through on the smelted stack already (BlockGlazableClayware clones it per
            // stack), so only the tier is stamped here.
            WareTier.Set(result, EnumWareTier.Stoneware);

            slot.Itemstack = result;
            slot.MarkDirty();
        }

        // ── Plumbing ─────────────────────────────────────────────────────────────────

        protected void Refuse(IPlayer byPlayer, string code, string langKey)
        {
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, Lang.Get(langKey));
            else (Api as ICoreClientAPI)?.TriggerIngameError(this, code, Lang.Get(langKey));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null) inventory = new InventoryGeneric(SlotCount, InvKey + "-" + Pos, Api);

            ITreeAttribute invTree = tree.GetTreeAttribute("inventory");
            if (invTree != null) inventory.FromTreeAttributes(invTree);

            burning = tree.GetBool("burning");
            burnUntilTotalHours = tree.GetDouble("burnUntilTotalHours");

            base.FromTreeAttributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            TreeAttribute invTree = new TreeAttribute();
            inventory.ToTreeAttributes(invTree);
            tree["inventory"] = invTree;

            tree.SetBool("burning", burning);
            tree.SetDouble("burnUntilTotalHours", burnUntilTotalHours);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            if (burning)
            {
                double left = Math.Max(0, burnUntilTotalHours - Api.World.Calendar.TotalHours);
                dsc.AppendLine(Lang.Get(LangPrefix + "-firing", left));
                return;
            }

            int wareCount = 0;
            for (int i = 0; i < WareSlots; i++) if (!inventory[i].Empty) wareCount += inventory[i].StackSize;

            if (wareCount == 0 && FuelSlot.Empty)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-empty"));
                return;
            }

            dsc.AppendLine(Lang.Get(LangPrefix + "-loaded", wareCount, WareSlots));
            dsc.AppendLine(FuelSlot.Empty
                ? Lang.Get(LangPrefix + "-nofuel-info")
                : Lang.Get(LangPrefix + "-fuel", FuelSlot.Itemstack.GetName(), FuelSlot.StackSize));

            if (wareCount > 0 && !FuelSlot.Empty) dsc.AppendLine(Lang.Get(LangPrefix + "-ready", BurnHours));
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            base.OnBlockBroken(byPlayer);
        }
    }
}
