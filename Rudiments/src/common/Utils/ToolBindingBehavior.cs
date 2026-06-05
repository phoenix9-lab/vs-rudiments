using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Rudiments.Utils
{
    /// <summary>
    /// Drives the "how is the tool head bound to the handle" feature. Injected at runtime onto every
    /// tool collectible (see <see cref="ToolBindingSystem"/>). All behaviour keys off the
    /// <c>bindingMethod</c> stack attribute baked on by the derived recipes:
    ///   frictionfit | glue | nail | gluenail   (absent = vanilla/rope, no effect)
    /// Reads:
    ///   durabilityMul (float) — multiplies max durability
    ///   frictionHead  (string) — item code dropped when a friction-fit tool comes apart
    ///   cureStartHours (double) — calendar hours when a glued tool was crafted (stamped here)
    /// </summary>
    public class ToolBindingBehavior : CollectibleBehavior
    {
        // Cached per (collectible id + method) tool meshes with the swapped binding texture.
        private static readonly Dictionary<string, MultiTextureMeshRef> meshCache = new();

        private ICoreAPI api;

        public ToolBindingBehavior(CollectibleObject collObj) : base(collObj) { }

        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            base.OnLoaded(api);
        }

        // ── Durability ───────────────────────────────────────────────────────────────

        public override int GetMaxDurability(ItemStack itemstack, int durability, ref EnumHandling bhHandling)
        {
            float mul = itemstack?.Attributes?.GetFloat("durabilityMul", 1f) ?? 1f;
            if (mul != 1f)
            {
                bhHandling = EnumHandling.Handled;
                return Math.Max(1, (int)Math.Round(durability * mul));
            }
            return base.GetMaxDurability(itemstack, durability, ref bhHandling);
        }

        // ── Glue curing: stamp at craft, block use until cured ─────────────────────────

        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling)
        {
            ItemStack stack = outputSlot?.Itemstack;
            string method = stack?.Attributes?.GetString("bindingMethod");
            if (method == null)
            {
                base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
                return;
            }

            if (method.Contains("glue") && !stack.Attributes.HasAttribute("cureStartHours"))
            {
                double now = api?.World?.Calendar?.TotalHours ?? 0;
                stack.Attributes.SetDouble("cureStartHours", now);
            }

            // Capture the actual resolved tool-head item code from the input grid so FrictionBreak
            // can drop the correct specific material (e.g. game:axehead-flint, not a wildcard).
            if (method == "frictionfit" && !stack.Attributes.HasAttribute("frictionHead"))
            {
                foreach (ItemSlot slot in allInputSlots)
                {
                    string path = slot?.Itemstack?.Collectible?.Code?.Path;
                    if (path != null && (path.Contains("head") || path.Contains("blade")))
                    {
                        stack.Attributes.SetString("frictionHead", slot.Itemstack.Collectible.Code.ToString());
                        break;
                    }
                }
            }

            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
        }

        private bool IsUncured(ItemStack stack)
        {
            string method = stack?.Attributes?.GetString("bindingMethod");
            if (method == null || !method.Contains("glue")) return false;
            if (!stack.Attributes.HasAttribute("cureStartHours")) return false;
            double start = stack.Attributes.GetDouble("cureStartHours");
            double cure = RudimentsModSystem.Config.GlueCureHours;
            return (api?.World?.Calendar?.TotalHours ?? double.MaxValue) < start + cure;
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (IsUncured(slot?.Itemstack))
            {
                WarnCuring(byEntity);
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                return;
            }
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handHandling, ref handling);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (IsUncured(slot?.Itemstack))
            {
                WarnCuring(byEntity);
                handHandling = EnumHandHandling.PreventDefault;
                handling = EnumHandling.PreventSubsequent;
                return;
            }
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
        }

        private void WarnCuring(EntityAgent byEntity)
        {
            if (byEntity?.World?.Side == EnumAppSide.Client)
            {
                (byEntity.Api as ICoreClientAPI)?.TriggerIngameError(this, "curing", Lang.Get("rudiments:binding-curing"));
            }
        }

        // ── Friction-fit failure: chance to come apart whenever durability is consumed ──

        public override void OnDamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount, ref EnumHandling bhHandling)
        {
            ItemStack stack = itemslot?.Itemstack;
            string method = stack?.Attributes?.GetString("bindingMethod");
            // Only trigger failure for a player holding the item; projectile entities (thrown spears,
            // arrows) carry the item in an entity-internal slot — destroying that slot makes the
            // projectile vanish rather than failing gracefully.
            if (method == "frictionfit" && world.Side == EnumAppSide.Server && byEntity is EntityPlayer)
            {
                var cfg = RudimentsModSystem.Config;
                if (world.Rand.NextDouble() < cfg.FrictionFailChance)
                {
                    FrictionBreak(world, byEntity, itemslot, stack, cfg);
                    bhHandling = EnumHandling.PreventDefault;
                    return;
                }
            }
            base.OnDamageItem(world, byEntity, itemslot, ref amount, ref bhHandling);
        }

        private void FrictionBreak(IWorldAccessor world, Entity byEntity, ItemSlot slot, ItemStack stack, RudimentsConfig cfg)
        {
            Vec3d pos = byEntity?.Pos?.XYZ ?? new Vec3d();
            world.PlaySoundAt(new AssetLocation("game:sounds/block/planks"), pos.X, pos.Y, pos.Z, null, true, 16f);

            // The stone head may survive (drops) or have broken when it came loose.
            string headCode = stack?.Attributes?.GetString("frictionHead");
            if (headCode != null && world.Rand.NextDouble() < cfg.HeadSurvivesChance)
            {
                Item headItem = world.GetItem(new AssetLocation(headCode));
                if (headItem != null) world.SpawnItemEntity(new ItemStack(headItem), pos.AddCopy(0, 0.2, 0));
            }

            slot.TakeOut(slot.StackSize);
            slot.MarkDirty();

            if (byEntity is EntityPlayer ep && ep.Player is IServerPlayer sp)
            {
                sp.SendIngameError("frictionfail", Lang.Get("rudiments:binding-frictionfail"));
            }
        }

        // ── Tooltip / name ─────────────────────────────────────────────────────────────

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            string method = inSlot?.Itemstack?.Attributes?.GetString("bindingMethod");
            if (method != null)
            {
                dsc.AppendLine(Lang.Get("rudiments:binding-info-" + method));
                if (IsUncured(inSlot.Itemstack)) dsc.AppendLine(Lang.Get("rudiments:binding-curing"));
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        // ── Graphic: swap only the "string" binding texture per method ─────────────────

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string method = itemstack?.Attributes?.GetString("bindingMethod");
            AssetLocation texLoc = BindingTexture(method);
            if (texLoc == null || collObj is not Item item || item.Shape?.Base == null) return;

            string key = item.Id + "-" + method;
            if (!meshCache.TryGetValue(key, out MultiTextureMeshRef meshRef))
            {
                meshRef = BuildMesh(capi, item, texLoc);
                if (meshRef == null) return;
                meshCache[key] = meshRef;
            }
            renderinfo.ModelRef = meshRef;
        }

        private static AssetLocation BindingTexture(string method)
        {
            return method switch
            {
                "glue" or "gluenail" => new AssetLocation("rudiments", "item/binding/glue"),
                "nail" => new AssetLocation("rudiments", "item/binding/nail"),
                "frictionfit" => new AssetLocation("rudiments", "item/binding/none"),
                _ => null
            };
        }

        private static MultiTextureMeshRef BuildMesh(ICoreClientAPI capi, Item item, AssetLocation texLoc)
        {
#pragma warning disable CS0618 // item texture source overload is the documented path for items
            ITexPositionSource baseSource = capi.Tesselator.GetTextureSource(item);
#pragma warning restore CS0618
            if (baseSource == null) return null;

            capi.ItemTextureAtlas.GetOrInsertTexture(texLoc, out _, out TextureAtlasPosition pos);
            if (pos == null) return null;

            var src = new BindingTexSource { Base = baseSource, StringPos = pos };
            capi.Tesselator.TesselateShape("rudiments-binding", item.Shape.Base, item.Shape, out MeshData mesh, src);
            return capi.Render.UploadMultiTextureMesh(mesh);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Client && meshCache.Count > 0)
            {
                foreach (var mr in meshCache.Values) mr?.Dispose();
                meshCache.Clear();
            }
            base.OnUnloaded(api);
        }

        /// <summary>Texture source that returns the swapped binding texture for the "string" slot and
        /// delegates everything else (handle, material, …) to the item's real texture source.</summary>
        private class BindingTexSource : ITexPositionSource
        {
            public ITexPositionSource Base;
            public TextureAtlasPosition StringPos;
            public Size2i AtlasSize => Base.AtlasSize;
            public TextureAtlasPosition this[string textureCode] => textureCode == "string" ? StringPos : Base[textureCode];
        }
    }
}
