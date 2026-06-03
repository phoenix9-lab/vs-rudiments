using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    /// <summary>
    /// Reusable rhizome-spread behavior. Applied to nettle crop and all reedpapyrus variants.
    ///
    /// Design: the hosting block calls TrySpreadTick() from its own OnServerGameTick override.
    ///
    /// Auto-detection rules (when not explicitly configured):
    ///   - If block has variant "habitat" == "water" or "ice" → requires water below the target
    ///   - Otherwise → requires fertile soil below the target (Fertility > 0)
    ///   - If no "spreadBlock" is set → spreads as the current block (same variant)
    ///   - Never spreads when block variant "state" == "harvested"
    ///
    /// Config properties (all optional):
    ///   spreadBlock         - AssetLocation to place on spread; default = current block
    ///   spreadChance        - probability per eligible tick (default 0.05)
    ///   requireFertileSoil  - override auto-detect: force soil check
    ///   requireWater        - override auto-detect: force water check
    /// </summary>
    public class BlockBehaviorRhizomeSpread : BlockBehavior
    {
        private AssetLocation spreadBlockCode;
        private double spreadChance = 0.05;
        private bool? requireFertileSoil = null; // null = auto-detect
        private bool? requireWater       = null; // null = auto-detect

        public BlockBehaviorRhizomeSpread(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            string code = properties["spreadBlock"].AsString(null);
            if (code != null) spreadBlockCode = new AssetLocation(code);
            spreadChance = properties["spreadChance"].AsDouble(0.05);

            if (properties["requireFertileSoil"].Exists) requireFertileSoil = properties["requireFertileSoil"].AsBool();
            if (properties["requireWater"].Exists)       requireWater       = properties["requireWater"].AsBool();
        }

        /// <summary>Call from the hosting block's OnServerGameTick.</summary>
        public bool TrySpreadTick(IWorldAccessor world, BlockPos pos)
        {
            // Never spread from a harvested state (e.g. coopersreed-harvested)
            if (block.Variant?["state"] == "harvested") return false;

            if (world.Rand.NextDouble() >= spreadChance) return false;

            // Determine spread target
            Block spreadBlock = spreadBlockCode != null
                ? world.GetBlock(spreadBlockCode)
                : block; // use current block variant as default
            if (spreadBlock == null) return false;

            // Pick random adjacent position
            BlockFacing face = BlockFacing.HORIZONTALS[world.Rand.Next(4)];
            BlockPos target = pos.AddCopy(face.Normali);
            BlockPos below  = target.DownCopy();

            Block atTarget   = world.BlockAccessor.GetBlock(target);
            Block belowBlock = world.BlockAccessor.GetBlock(below);

            if (atTarget.Replaceable < 6000) return false;

            // Resolve water vs soil requirement
            bool needsWater, needsSoil;
            if (requireWater.HasValue || requireFertileSoil.HasValue)
            {
                // Explicit config overrides
                needsWater = requireWater ?? false;
                needsSoil  = requireFertileSoil ?? false;
            }
            else
            {
                // Auto-detect from block's habitat variant
                string habitat = block.Variant?["habitat"];
                needsWater = habitat == "water" || habitat == "ice";
                needsSoil  = !needsWater;
            }

            if (needsWater && !belowBlock.IsLiquid())    return false;
            if (needsSoil  && belowBlock.Fertility <= 0) return false;

            world.BlockAccessor.SetBlock(spreadBlock.BlockId, target);
            return true;
        }
    }
}
