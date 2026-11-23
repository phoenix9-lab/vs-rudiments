using System.Text;
using Rudiments.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Items
{
    /// <summary>
    /// Puts a vanilla item into the fine-textile chain (patches/fine-textiles.json): cloth (role
    /// <c>fabric</c>), sewing kits (<c>tool</c>), bandages, poultices and fishing poles
    /// (<c>product</c>). It carries the fine mark through crafting (<see cref="FineTextile.ApplyCraftMark"/>),
    /// names and describes fine stacks, and keeps the mark when the item transitions, so an
    /// alcohol-soaked fine bandage dries into a fine clean one. The bonuses themselves live in
    /// <see cref="FineTextilePatches"/>, because they are vanilla code.
    /// </summary>
    public class CollectibleBehaviorFineTextile : CollectibleBehavior, IFineTextile
    {
        public FineRole Role { get; private set; } = FineRole.Fabric;

        private ICoreAPI api;

        public CollectibleBehaviorFineTextile(CollectibleObject collObj) : base(collObj) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            Role = (properties?["role"].AsString("fabric") ?? "fabric") switch
            {
                "product" => FineRole.Product,
                "tool" => FineRole.Tool,
                _ => FineRole.Fabric,
            };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
        }

        public override void OnCreatedByCrafting(ItemSlot[] allInputSlots, ItemSlot outputSlot, IRecipeBase byRecipe, ref EnumHandling bhHandling)
        {
            base.OnCreatedByCrafting(allInputSlots, outputSlot, byRecipe, ref bhHandling);
            FineTextile.ApplyCraftMark(allInputSlots, outputSlot);
        }

        public override void GetHeldItemName(StringBuilder sb, ItemStack itemStack)
        {
            base.GetHeldItemName(sb, itemStack);
            FineTextile.WrapName(sb, itemStack);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            FineTextile.AppendInfo(inSlot, dsc);
        }

        public override ItemStack OnTransitionNow(ItemSlot slot, TransitionableProperties props, ref EnumHandling handling)
        {
            ItemStack from = slot?.Itemstack;
            ItemStack template = props?.TransitionedStack?.ResolvedItemstack;
            if (!FineTextile.IsFine(from) || template == null || api == null)
            {
                return base.OnTransitionNow(slot, props, ref handling);
            }

            ItemStack to = template.Clone();
            to.StackSize = GameMath.RoundRandom(api.World.Rand, from.StackSize * props.TransitionRatio);
            FineTextile.SetFine(to);
            handling = EnumHandling.Handled;
            return to;
        }
    }
}
