using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Tilemaps;

namespace PolyMod
{
    internal static class Minerskagg
    {
        public static void AddMappings()
        {
            EnumCache<UnitAbility.Type>.AddMapping("selfdestruction", (UnitAbility.Type)600);
            EnumCache<UnitAbility.Type>.AddMapping("selfdestruction", (UnitAbility.Type)600);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
        private static void AttackCommand_ExecuteDefault(AttackCommand __instance, ref GameState gameState)
        {
            UnitState unitState;
            gameState.TryGetUnit(__instance.UnitId, out unitState);
            TileData tile = gameState.Map.GetTile(__instance.Target);
            UnitState unit = tile.unit;
            Console.Write(unit);
            Console.Write(unit.HasAbility((UnitAbility.Type)600, gameState));
            if (unit.HasAbility((UnitAbility.Type)600, gameState))
            {
                gameState.ActionStack.Add(new AttackAction(__instance.PlayerId, __instance.Target, __instance.Origin, 100, false, AttackAction.AnimationType.None, 20));
            }
        }
    }
}
