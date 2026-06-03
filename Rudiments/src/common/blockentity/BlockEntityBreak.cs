using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;

namespace Rudiments.SRC.Common.BlockEntities
{
    public class BlockEntityBreak : BlockEntity
    {
        protected ICoreServerAPI sapi;

        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            sapi = api as ICoreServerAPI;

            if (api.Side == EnumAppSide.Client)
            {
                animUtil?.InitializeAnimator(Block.Attributes["animatorName"].AsString(), null, null, new Vec3f(0, Block.Shape.rotateY, 0));
            }
        }

        public void Activate()
        {
            animUtil?.StartAnimation(new AnimationMetaData(){Animation = "breaking", Code = "breaking"});
        }

        public void Deactivate() 
        {
            animUtil?.StopAnimation("breaking");
        }

    }
}