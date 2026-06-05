namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// A spreading-plant block entity whose patch has a bounded origin. When a plant spreads, the
    /// child's origin is set to the parent's via this interface so the outward radius cap bounds the
    /// whole patch (not each plant). Implemented by nettle, the buried rhizome, and reeds.
    /// </summary>
    public interface IPatchOrigin
    {
        void SetPatchOrigin(int x, int z);
    }
}
