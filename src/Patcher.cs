using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;

namespace PolyMod
{
	internal class Patcher
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameStateUtils), nameof(GameStateUtils.GetRandomPickableTribe), new System.Type[] { typeof(GameState) })]
		public static void GameStateUtils_GetRandomPickableTribe(GameState gameState)
		{
			if (Plugin.version != -1)
			{
				gameState.Version = Plugin.version;
				Plugin.version = -1;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SearchFriendCodePopup), nameof(SearchFriendCodePopup.OnInputChanged))]
		private static bool SearchFriendCodePopup_OnInputChanged(SearchFriendCodePopup __instance, string value)
		{
			if (PolymodUI.isUIActive)
			{
				PolymodUI.OnInputChanged(__instance, value);
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SearchFriendCodePopup), nameof(SearchFriendCodePopup.OnInputDone))]
		private static bool SearchFriendCodePopup_OnInputDone(SearchFriendCodePopup __instance, string value)
		{
			if (PolymodUI.isUIActive)
			{
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update(GameManager __instance)
		{
			Plugin.Update();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			MapLoader.PreGenerate(ref state, ref settings);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate_(ref GameState state)
		{
			MapLoader.PostGenerate(ref state);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GeneratePlayerCapitalPositions))]
		private static void MapGenerator_GeneratePlayerCapitalPositions(ref Il2CppSystem.Collections.Generic.List<int> __result, int width, int playerCount)
		{
			__result = MapLoader.GetCapitals(__result, width, playerCount);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetMaxOpponents))]
		private static void GameManager_GetMaxOpponents(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS - 1;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GetMaximumOpponentCountForMapSize))]
		private static void MapDataExtensions_GetMaximumOpponentCountForMapSize(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.GetUnlockedTribeCount))]
		private static void PurchaseManager_GetUnlockedTribeCount(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS + 2;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.IsTribeUnlocked))]
		private static void PurchaseManager_IsTribeUnlocked(ref bool __result, TribeData.Type type)
		{
			__result = (int)type >= Plugin.AUTOIDX_STARTS_FROM || __result;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
		private static void GameLogicData_Parse(JObject rootObject)
		{
			ModLoader.Init(rootObject);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = Plugin.CAMERA_MAXZOOM_CONSTANT;
			//CameraController.Instance.minZoom = Plugin.CAMERA_MINZOOM_CONSTANT;
			CameraController.Instance.techViewBounds = new(
				new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT), CameraController.Instance.techViewBounds.size
			);
			GameObject.Find("TechViewWorldSpace").transform.position = new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetTileSpriteAddress), new Type[] { typeof(Polytopia.Data.TerrainData.Type), typeof(string) })]
		private static void SpriteData_GetTileSpriteAddress(ref SpriteAddress __result, Polytopia.Data.TerrainData.Type terrain, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetResourceSpriteAddress), new Type[] { typeof(ResourceData.Type), typeof(string) })]
		private static void SpriteData_GetResourceSpriteAddress(ref SpriteAddress __result, ResourceData.Type type, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<ResourceData.Type>.GetName(type), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetBuildingSpriteAddress), new Type[] { typeof(ImprovementData.Type), typeof(string) })]
		private static void SpriteData_GetBuildingSpriteAddress(ref SpriteAddress __result, ImprovementData.Type type, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<ImprovementData.Type>.GetName(type), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetUnitIconAddress))]
		private static void SpriteData_GetUnitIconAddress(ref SpriteAddress __result, UnitData.Type type)
		{
			__result = ModLoader.GetSprite(__result, "icon", EnumCache<UnitData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(int) })]
		private static void SpriteData_GetHeadSpriteAddress_1(ref SpriteAddress __result, int tribe)
		{
			__result = ModLoader.GetSprite(__result, "head", $"{tribe}");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(string) })]
		private static void SpriteData_GetHeadSpriteAddress_2(ref SpriteAddress __result, string specialId)
		{
			__result = ModLoader.GetSprite(__result, "head", specialId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAvatarPartSpriteAddress))]
		private static void SpriteData_GetAvatarPartSpriteAddress(ref SpriteAddress __result, string sprite)
		{
			__result = ModLoader.GetSprite(__result, sprite, "");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHouseAddresses))]
		private static void SpriteData_GetHouseAddresses(ref Il2CppReferenceArray<SpriteAddress> __result, int type, string styleId, SkinType skinType)
		{
			List<SpriteAddress> sprites = new()
			{
				ModLoader.GetSprite(__result[0], "house", styleId, type)
			};
			if (skinType != SkinType.Default)
			{
				sprites.Add(ModLoader.GetSprite(__result[1], "house", EnumCache<SkinType>.GetName(skinType), type));
			}
			__result = sprites.ToArray();
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.SetAmbienceClimate))]
		private static void AudioManager_SetAmbienceClimatePrefix(ref int climate)
		{
			if (climate > 16)
			{
				climate = 1;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen.MapPresetDataSource), nameof(GameSetupScreen.MapPresetDataSource.GetData))]
		private static void GameSetupScreen_MapPresetDataSource_GetData(ref Il2CppStructArray<MapPreset> __result)
		{
			List<MapPreset> presets = __result.ToList();
			presets.Add((MapPreset)500);
			__result = presets.ToArray();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnMapPresetChanged))]
		private static void GameSetupScreen_OnMapPresetChanged(GameSetupScreen __instance, int index)
		{
			MapLoader.map = null;
			MapPreset[] array = GameSetupScreen.MapPresetDataSource.GetData(GameManager.PreliminaryGameSettings.GameType == GameType.Matchmaking);

			if (MapLoader.isListInstantiated)
			{
				UnityEngine.Object.Destroy(MapLoader.customMapsList.gameObject);
				MapLoader.isListInstantiated = false;
			}

			if ((int)array[index] == 500)
			{
				string[] maps = Directory.GetFiles(Plugin.MAPS_PATH, "*.json");
				if (maps.Length != 0)
				{
					GameManager.PreliminaryGameSettings.mapPreset = array[index];
					MapLoader.customMapsList = __instance.CreateHorizontalList("Maps", maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToArray(), new Action<int>(MapLoader.OnCustomMapChanged), 0, null, 500);
					MapLoader.isListInstantiated = true;
				}
				else
				{
					GameManager.PreliminaryGameSettings.mapPreset = MapPreset.None;
					NotificationManager.Notify(Localization.Get("No maps found"), Localization.Get("gamesettings.notavailable"), null, null);
				}
			}

			__instance.UpdateOpponentList();
			GameManager.PreliminaryGameSettings.SaveToDisk();
			__instance.RefreshInfo();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnStartGameClicked))]
		private static void GameSetupScreen_OnStartGameClicked()
		{
			MapLoader.isListInstantiated = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapPresetExtensions), nameof(MapPresetExtensions.GetLocalizationName))]
		private static void MapPresetExtensions_GetLocalizationName(ref string __result, MapPreset mapPreset)
		{
			if (mapPreset == (MapPreset)500)
			{
				__result = "Custom";
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.RedrawScreen))]
		private static void GameSetupScreen_RedrawScreen()
		{
			MapLoader.map = null;
		}

		//do not touch TechItem patches, they prevent game from crashing when opening tech tree while playing as custom tribe(idfk how this works)
		[HarmonyPostfix]
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.GetUnlockItems))]
		private static void TechItem_GetUnlockItems(TechData data, PlayerState playerState, bool onlyPickFirstItem = false)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.SetupComplete))]
		private static void TechItem_SetupComplete()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(BasicPopup), nameof(BasicPopup.Update))]
		private static void BasicPopup_Update(BasicPopup __instance)
		{
			if (PolymodUI.isUIActive)
			{
				__instance.rectTransform.SetWidth(PolymodUI.width);
				__instance.rectTransform.SetHeight(PolymodUI.height);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PopupButtonContainer), nameof(PopupButtonContainer.SetButtonData))]
		private static void PopupButtonContainer_SetButtonData(PopupButtonContainer __instance, Il2CppReferenceArray<PopupBase.PopupButtonData> buttonData)
		{
			int num = __instance.buttons.Length;
			for (int i = 0; i < num; i++)
			{
				UITextButton uitextButton = __instance.buttons[i];
				Vector2 vector = new Vector2((num == 1) ? 0.5f : (i / (num - 1.0f)), 0.5f);
				uitextButton.rectTransform.anchorMin = vector;
				uitextButton.rectTransform.anchorMax = vector;
				uitextButton.rectTransform.pivot = vector;
			}
		}

		//MINERSKAGG
		[HarmonyPostfix]
		[HarmonyPatch(typeof(AttackReaction), nameof(AttackReaction.NormalAnimation))]
		private static void AttackReaction_NormalAnimation(AttackReaction __instance, Action onComplete)
		{
			Tile originTile = MapRenderer.Current.GetTileInstance(__instance.action.Origin);
			Tile targetTile = MapRenderer.Current.GetTileInstance(__instance.action.Target);
			GameState gameState = GameManager.GameState;
			if (originTile.unit.state.HasAbility((UnitAbility.Type)600, gameState))
			{
				if (originTile && originTile.Unit && (!originTile.IsHidden || !targetTile.IsHidden))
				{
					//targetTile.Unit.Attack(__instance.action.Target, false, (Action)test);
					return;
				}
				onComplete();

				void test()
				{
					if (originTile && originTile.Unit && !originTile.IsHidden)
					{
						originTile.Damage(10);
						originTile.RenderUnit();
					}
					GameManager.DelayCall((int)__instance.action.Delay, onComplete);
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetDefenceBonus))]
		private static bool UnitDataExtensions_GetDefenceBonus(ref int __result, UnitState unit, GameState gameState)
		{
			int result = 10;
			if (unit.HasEffect(UnitEffect.Poisoned))
			{
				__result = 7;
			}
			TileData tile = gameState.Map.GetTile(unit.coordinates);
			if (tile == null)
			{
				__result = result;
			}
			PlayerState player;
			gameState.TryGetPlayer(unit.owner, out player);
			if (unit.owner != 0 && gameState.TryGetPlayer(unit.owner, out player) && player.GetDefenceBonus(tile.terrain, gameState) > 1)
			{
				result = 15;
			}
			ImprovementData data;
			if (tile.improvement != null && gameState.GameLogicData.TryGetData(tile.improvement.type, out data))
			{
				if (tile.HasImprovement(ImprovementData.Type.City) && tile.owner == unit.owner && unit.HasAbility(UnitAbility.Type.Fortify, gameState))
				{
					result = 15;
					if (tile.improvement.HasReward(CityReward.CityWall))
					{
						result = 40;
					}
				}
				if (data.HasAbility(ImprovementAbility.Type.Defend) && tile.owner == unit.owner)
				{
					if (tile.owner == unit.owner || player.HasPeaceWith(tile.owner))
					{
						result = 40;
					}
					else
					{
						result = 15;
					}
				}
			}
			if (player.HasTribeAbility((TribeAbility.Type)601, gameState))
			{
				if (tile.owner == unit.owner || player.HasPeaceWith(tile.owner))
				{
					result *= 2;
				}
				else
				{
					result /= 2;
				}
			}
			__result = result;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TileDataExtensions), nameof(TileDataExtensions.HasEmbarkImprovement))]
		private static bool TileDataExtensions_HasEmbarkImprovement(ref bool __result, TileData tileState, GameState gameState)
		{
			if (tileState.improvement == null)
			{
                __result = false;
			}
			ImprovementData data;
			if (gameState.Version < 95)
			{
				if (tileState.improvement.type == ImprovementData.Type.Port)
				{
                    __result = true;
				}
			}
			else if (gameState.GameLogicData.TryGetData(tileState.improvement.type, out data) && data.HasAbility(ImprovementAbility.Type.Embark))
			{
                __result = true;
			}
            __result = false;
			return false;
		}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EmbarkAction), nameof(EmbarkAction.ExecuteDefault))]
        private static bool EmbarkAction_ExecuteDefault(EmbarkAction __instance, GameState gameState)
        {
            PlayerState playerState;
            if (gameState.TryGetPlayer(__instance.PlayerId, out playerState))
            {
                TileData tile = gameState.Map.GetTile(__instance.Coordinates);
                UnitState unit = tile.unit;
                UnitData.Type type = UnitData.Type.Transportship;
                if (unit.HasAbility(UnitAbility.Type.Hide, gameState))
                {
                    type = UnitData.Type.Cloak_Boat;
                }
                if (unit.type == UnitData.Type.Dagger)
                {
                    type = UnitData.Type.Pirate;
                }
                if (unit.type == UnitData.Type.Giant)
                {
                    type = UnitData.Type.Juggernaut;
                }
                if (tile.HasImprovement((ImprovementData.Type)ModLoader.gldDictionary["airport"]))
                {
                    type = (UnitData.Type)ModLoader.gldDictionary["transportplane"];
                }
                UnitData unitData;
                gameState.GameLogicData.TryGetData(type, out unitData);
                UnitState unitState = ActionUtils.TrainUnit(gameState, playerState, tile, unitData);
                if (!unitState.HasAbility(UnitAbility.Type.Protect, gameState))
                {
                    unitState.health = unit.health;
                }
                unitState.home = unit.home;
                unitState.direction = unit.direction;
                unitState.flipped = unit.flipped;
                unitState.passengerUnit = unit;
                unitState.effects = unit.effects;
                unitState.attacked = true;
                unitState.moved = true;
                if (unitState.HasAbility(UnitAbility.Type.Stomp, gameState))
                {
                    ActionUtils.StompAttack(gameState, unitState, __instance.Coordinates);
                }
            }
			return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
        private static void MoveAction_ExecuteDefault(MoveAction __instance, GameState gameState)
        {
            UnitState unitState;
            PlayerState playerState;
            UnitData unitData;
            if (gameState.TryGetUnit(__instance.UnitId, out unitState) && gameState.TryGetPlayer(__instance.PlayerId, out playerState) && gameState.GameLogicData.TryGetData(unitState.type, out unitData))
            {
                WorldCoordinates worldCoordinates = __instance.Path[0];
                WorldCoordinates worldCoordinates2 = __instance.Path[__instance.Path.Count - 1];
                TileData tile = gameState.Map.GetTile(worldCoordinates2);
                TileData tile2 = gameState.Map.GetTile(worldCoordinates);
                unitState.moved = (unitState.moved || ((__instance.Reason != MoveAction.MoveReason.Attack || !unitState.HasAbility(UnitAbility.Type.Escape, null)) && __instance.Reason != MoveAction.MoveReason.Push));
                tile.SetUnit(null);
                tile2.SetUnit(unitState);
                unitState.coordinates = worldCoordinates;
                if (!unitData.IsAquatic() && !unitState.HasAbility(UnitAbility.Type.Fly, gameState) && tile2.HasEmbarkImprovement(gameState))
                {
                    gameState.ActionStack.Add(new EmbarkAction(__instance.PlayerId, worldCoordinates));
                }
                else if (!tile2.IsWater && unitData.IsVehicle())
                {
                    gameState.ActionStack.Add(new DisembarkAction(__instance.PlayerId, worldCoordinates));
                }
            }
        }
    }
}
