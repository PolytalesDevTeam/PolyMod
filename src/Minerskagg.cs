using Polytopia.Data;

namespace PolyMod
{
    internal static class Minerskagg
    {
        public static void AddMappings()
        {
            EnumCache<UnitAbility.Type>.AddMapping("selfdestruction", (UnitAbility.Type)600);
            EnumCache<UnitAbility.Type>.AddMapping("selfdestruction", (UnitAbility.Type)600);

            EnumCache<TribeAbility.Type>.AddMapping("homelandbuff", (TribeAbility.Type)601);
            EnumCache<TribeAbility.Type>.AddMapping("homelandbuff", (TribeAbility.Type)601);

            EnumCache<ImprovementAbility.Type>.AddMapping("airport", (ImprovementAbility.Type)602);
            EnumCache<ImprovementAbility.Type>.AddMapping("airport", (ImprovementAbility.Type)602);
        }
    }
}
