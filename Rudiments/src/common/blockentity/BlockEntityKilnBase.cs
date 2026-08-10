using System;
using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// Everything the Rudiments kilns have in common: the fuel gate, the loading and unloading,
    /// the burn timer, and turning greenware into fired ware.
    ///
    /// The fuel gate is the bloomery's, verbatim: <c>BurnTemperature &gt;= 1200 &amp;&amp;
    /// BurnDuration &gt; 30</c>. That admits charcoal, coke, bituminous and anthracite coal, and
    /// refuses lignite, peat and every wood. Cold fuel is refused **at insertion** with an ingame
    /// error rather than accepted and then disappointing, which is how every vanilla fuel gate
    /// behaves — nothing in vanilla lets a player wait ten in-game hours for a worse result.
    ///
    /// Ware admission is the generic <c>SmeltingType == Fire</c> contract rather than a list of
    /// codes, so Clayworks greenware and any other mod's fires here with no compat file at all, and
    /// the output is read off the input block's own combustible properties rather than a hardcoded
    /// ware code. That is why one code path serves every domain.
    ///
    /// Capacity is measured in <b>quarter-tile units</b> rather than slots, which is vanilla's own
    /// ground-storage layout model: a <c>SingleCenter</c> item — a storage vessel, a big crock — is
    /// one whole tile and costs 4, everything else costs 1. A kiln with 8 units therefore holds two
    /// large pieces or eight small ones, or any honest mix, with no capacity table anywhere.
    /// </summary>
    public abstract class BlockEntityKilnBase : BlockEntity
    {
        protected InventoryGeneric inventory;

        /// <summary>Total game hours at which the current burn finishes. 0 = not lit.</summary>
        protected double burnUntilTotalHours;
        protected bool burning;

        private AssetLocation insertSound;

        // --- subclass contract ---
        protected abstract string InvKey { get; }
        protected abstract string LangPrefix { get; }
        /// <summary>Number of ware slots. The fuel slot always follows them.</summary>
        protected abstract int WareSlots { get; }
        /// <summary>Ware capacity in quarter-tile units. 4 = one large piece, 8 = two.</summary>
        protected abstract int WareCapacityUnits { get; }
        protected abstract float BurnHours { get; }
        /// <summary>Extra ignition requirements — the updraft kiln's chimney. Refuse with a message.</summary>
        protected virtual bool CanIgnite(IPlayer byPlayer) => true;

        public int FuelSlotIndex => WareSlots;
        public ItemSlot FuelSlot => inventory[FuelSlotIndex];
        public bool IsBurning => burning;

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null) inventory = new InventoryGeneric(WareSlots + 1, InvKey + "-" + Pos, api);

            base.Initialize(api);
            inventory.LateInitialize(InvKey + "-" + Pos, api);

            insertSound = new AssetLocation("game", "sounds/block/ceramicplace");
            RegisterGameTickListener(OnGameTick, 1000);
        }

        // ── Admission ────────────────────────────────────────────────────────────────

        /// <summary>Anything a kiln is supposed to fire, by the generic contract rather than a code list.</summary>
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

        /// <summary>
        /// What one piece of this ware costs in quarter-tile units. Read straight off the item's own
        /// ground-storage layout, so a mod's big vessel is large here for the same reason it is
        /// large on the floor.
        /// </summary>
        public static int UnitCost(ItemStack stack)
        {
            var props = stack?.Collectible?.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
            return props != null && props.Layout == EnumGroundStorageLayout.SingleCenter ? 4 : 1;
        }

        public int UsedUnits()
        {
            int used = 0;
            for (int i = 0; i < WareSlots; i++)
            {
                if (!inventory[i].Empty) used += UnitCost(inventory[i].Itemstack) * inventory[i].StackSize;
            }
            return used;
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

            if (held == null) return TryUnload(byPlayer);
            if (IsHotFuel(held)) return TryInsert(byPlayer, heldSlot, FuelSlot, heldSlot.StackSize);
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
            int cost = UnitCost(heldSlot.Itemstack);
            int room = (WareCapacityUnits - UsedUnits()) / cost;
            if (room <= 0)
            {
                Refuse(byPlayer, "full", LangPrefix + "-full");
                return true;
            }

            int wanted = Math.Min(room, byPlayer.Entity.Controls.CtrlKey ? heldSlot.StackSize : 1);

            for (int i = 0; i < WareSlots; i++)
            {
                ItemSlot slot = inventory[i];
                if (!slot.Empty && slot.Itemstack.Collectible != heldSlot.Itemstack.Collectible) continue;
                if (TryInsert(byPlayer, heldSlot, slot, wanted)) return true;
            }

            Refuse(byPlayer, "full", LangPrefix + "-full");
            return true;
        }

        private bool TryInsert(IPlayer byPlayer, ItemSlot heldSlot, ItemSlot target, int quantity)
        {
            int moved = heldSlot.TryPutInto(Api.World, target, Math.Max(1, quantity));
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

            if (UsedUnits() == 0)
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
        /// Converts one slot of greenware. The output comes off the input block's own combustible
        /// properties — never a hardcoded ware code. Porcelain needs no special case here: its own
        /// smeltedStack is already shards, because an ordinary firing cannot give it the sealed soak
        /// it wants. Kilns that can do better override this.
        /// </summary>
        protected virtual void FireSlot(ItemSlot slot)
        {
            ItemStack raw = slot.Itemstack;
            ItemStack result = SmeltOne(raw, raw.StackSize);
            if (result == null) return;

            // Glaze rides through on the smelted stack already — BlockGlazableClayware clones it per
            // stack — so only the tier is stamped here.
            WareTier.Set(result, EnumWareTier.Stoneware);

            slot.Itemstack = result;
            slot.MarkDirty();
        }

        /// <summary>The fired form of <paramref name="raw"/>, at the given input count. Null if it does not fire.</summary>
        protected ItemStack SmeltOne(ItemStack raw, int inputCount)
        {
            CombustibleProperties props = raw.Collectible.GetCombustibleProperties(Api.World, raw, null);
            ItemStack fired = props?.SmeltedStack?.ResolvedItemstack;
            if (fired == null) return null;

            ItemStack result = fired.Clone();
            result.StackSize = Math.Max(1, inputCount / Math.Max(1, props.SmeltedRatio));
            return result;
        }

        /// <summary>Puts a stack in the first free ware slot, or drops it at the kiln if there is none.</summary>
        protected void PutOrDrop(ItemStack stack)
        {
            if (stack == null) return;

            for (int i = 0; i < WareSlots; i++)
            {
                if (inventory[i].Empty)
                {
                    inventory[i].Itemstack = stack;
                    inventory[i].MarkDirty();
                    return;
                }
            }

            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        // ── Plumbing ─────────────────────────────────────────────────────────────────

        protected void Refuse(IPlayer byPlayer, string code, string langKey)
        {
            if (byPlayer is IServerPlayer splr) splr.SendIngameError(code, Lang.Get(langKey));
            else (Api as ICoreClientAPI)?.TriggerIngameError(this, code, Lang.Get(langKey));
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            if (inventory == null) inventory = new InventoryGeneric(WareSlots + 1, InvKey + "-" + Pos, Api);

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

            int used = UsedUnits();
            if (used == 0 && FuelSlot.Empty)
            {
                dsc.AppendLine(Lang.Get(LangPrefix + "-empty"));
                return;
            }

            dsc.AppendLine(Lang.Get(LangPrefix + "-loaded", used, WareCapacityUnits));
            dsc.AppendLine(FuelSlot.Empty
                ? Lang.Get(LangPrefix + "-nofuel-info")
                : Lang.Get(LangPrefix + "-fuel", FuelSlot.Itemstack.GetName(), FuelSlot.StackSize));

            if (used > 0 && !FuelSlot.Empty) dsc.AppendLine(Lang.Get(LangPrefix + "-ready", BurnHours));
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
