namespace Rudiments.SRC.Common.BlockEntities
{
    /// <summary>
    /// A single-block brick kiln: four ware slots and one fuel, hot enough to vitrify.
    ///
    /// It is the rung between the pit kiln and the beehive. The pit kiln is free and gives you
    /// porous earthenware; the beehive is a multi-block build. This one is made out of the bricks
    /// the pit kiln already gave you, so nothing about the vanilla progression changes to reach it.
    ///
    /// Everything it does — the fuel gate, loading, the burn timer, turning greenware into
    /// stoneware — lives in <see cref="BlockEntityKilnBase"/>. What is left here is the size of it.
    /// </summary>
    public class BlockEntitySmallBrickKiln : BlockEntityKilnBase
    {
        protected override string InvKey => "smallbrickkiln";
        protected override string LangPrefix => "rudiments:smallbrickkiln";
        protected override int WareSlots => 4;

        /// <summary>One ground-storage tile: four small pieces, one large one, or one full
        /// pit-kiln-sized brick pile. Half the updraft kiln.</summary>
        protected override float WareTiles => 1f;

        protected override float BurnHours => RudimentsModSystem.Config.SmallBrickKilnBurnHours;

        protected override int FuelPerFiring => RudimentsModSystem.Config.SmallBrickKilnFuelPerFiring;
    }
}
