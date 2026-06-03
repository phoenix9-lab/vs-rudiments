using Rudiments.SRC.Common.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.Blocks
{
    internal class BlockDryingRack : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side == EnumAppSide.Client) return true;

            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityDryingRack;
            if (be == null) return false;

            return be.OnInteract(byPlayer);
        }

        public MeshData GenMesh(ICoreClientAPI capi, string shapePath, ITexPositionSource texture)
        {
            var tesselator = capi.Tesselator;
            Shape shape = capi.Assets.TryGet(shapePath + ".json").ToObject<Shape>();

            tesselator.TesselateShape(shapePath, shape, out var mesh, texture, new Vec3f(0, 0, 0));
            var rotate = this.Shape.rotateY;
            mesh.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, rotate * GameMath.DEG2RAD, 0);
            return mesh;
        }
    }
}
