using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Rudiments.Utils
{
    /// <summary>
    /// Reads a "durabilityBonus" integer from the item's stack attributes and adds it to max durability.
    /// Set via recipe output attributes when fine cord is used as the string ingredient.
    /// Patched onto vanilla bows (and any other item that benefits) via JSON patch.
    /// </summary>
    public class DurabilityBonusBehavior : CollectibleBehavior
    {
        public DurabilityBonusBehavior(CollectibleObject collObj) : base(collObj) { }

        public override int GetMaxDurability(ItemStack itemstack, int durability, ref EnumHandling bhHandling)
        {
            int bonus = itemstack?.Attributes?.GetInt("durabilityBonus", 0) ?? 0;
            if (bonus > 0)
            {
                bhHandling = EnumHandling.Handled;
                return durability + bonus;
            }
            return base.GetMaxDurability(itemstack, durability, ref bhHandling);
        }
    }
}
