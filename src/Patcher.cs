using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;

namespace PolyMod
{
	internal class Patcher
	{
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameStateUtils), nameof(GameStateUtils.GetRandomPickableTribe), new System.Type[] { typeof(GameState) })]
		public static bool GameStateUtils_GetRandomPickableTribe(GameState gameState)
		{
			if (Plugin.version > 0)
			{
				gameState.Version = Plugin.version;
				DebugConsole.Write($"Changed version to {Plugin.version}");
				Plugin.version = -1;
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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GenerateShoreLines))]
		public static bool MapDataExtensions_GenerateShoreLines(MapData map)
		{
			int width = (int)map.Width;
			int num = width * (int)(map.Height - 1);
			for (int i = 0; i < map.Tiles.Length; i++)
			{
				TileData.ShorelineFlag shorelineFlag = TileData.ShorelineFlag.None;
				if (map.Tiles[i].terrain == Polytopia.Data.TerrainData.Type.Water)
				{
					int num2 = i % width;
					shorelineFlag |= ((i > width && MapDataExtensions.IsNonFrozenWater(map.Tiles[i - width])) ? TileData.ShorelineFlag.None : TileData.ShorelineFlag.None);
					shorelineFlag |= ((i < num && MapDataExtensions.IsNonFrozenWater(map.Tiles[i + width])) ? TileData.ShorelineFlag.None : TileData.ShorelineFlag.None);
					shorelineFlag |= ((num2 > 0 && MapDataExtensions.IsNonFrozenWater(map.Tiles[i - 1])) ? TileData.ShorelineFlag.None : TileData.ShorelineFlag.None);
					shorelineFlag |= ((num2 < width - 1 && MapDataExtensions.IsNonFrozenWater(map.Tiles[i + 1])) ? TileData.ShorelineFlag.None : TileData.ShorelineFlag.None);
				}
				map.Tiles[i].shoreLines = shorelineFlag;
			}
			return false;
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
			CameraController.Instance.maxZoom = Plugin.CAMERA_CONSTANT;
			CameraController.Instance.techViewBounds = new(
				new(Plugin.CAMERA_CONSTANT, Plugin.CAMERA_CONSTANT), CameraController.Instance.techViewBounds.size
			);
			UnityEngine.GameObject.Find("TechViewWorldSpace").transform.position = new(Plugin.CAMERA_CONSTANT, Plugin.CAMERA_CONSTANT);
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
		[HarmonyPatch(typeof(SettingsScreen), nameof(SettingsScreen.CreateLanguageList))]
		private static bool SettingsScreen_CreateLanguageList(SettingsScreen __instance, UnityEngine.Transform parent)
		{
			List<string> list = new() { "Automatic", "English", "Français", "Deutsch", "Italiano", "Português", "Русский", "Español", "日本語", "한국어" };
			List<int> list2 = new() { 1, 3, 7, 9, 6, 4, 5, 8, 11, 12 };
			if (GameManager.GetPurchaseManager().IsTribeUnlocked(Polytopia.Data.TribeData.Type.Elyrion))
			{
				list.Add("∑∫ỹriȱŋ");
				list2.Add(10);
			}
			list.Add("Custom...");
			list2.Add(2);
			__instance.languageSelector = UnityEngine.Object.Instantiate(__instance.horizontalListPrefab, parent ?? __instance.container);
			__instance.languageSelector.UpdateScrollerOnHighlight = true;
			__instance.languageSelector.HeaderKey = "settings.language";
			__instance.languageSelector.SetIds(list2.ToArray());
			__instance.languageSelector.SetData(list.ToArray(), 0, false);
			__instance.languageSelector.SelectId(SettingsUtils.Language, true, -1f);
			__instance.languageSelector.IndexSelectedCallback = new Action<int>(__instance.LanguageChangedCallback);
			__instance.totalHeight += __instance.languageSelector.rectTransform.sizeDelta.y;

			return false;
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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnMapPresetChanged))]
		private static bool GameSetupScreen_OnMapPresetChanged(GameSetupScreen __instance, int index)
		{
			MapLoader.map = null;
			bool isMatchMaking = GameManager.PreliminaryGameSettings.GameType == GameType.Matchmaking;
			MapPreset[] array = isMatchMaking ? GameSetupScreen.MapPresetDataSource.dataMatchMaking : GameSetupScreen.MapPresetDataSource.data;

			var list = array.ToList();
			list.Add((MapPreset)500);
			array = list.ToArray();

			if (MapLoader.isListInstantiated)
			{
				UnityEngine.Object.Destroy(MapLoader.customMapsList.gameObject);
				MapLoader.isListInstantiated = false;
			}

			if (index >= 0 && index < array.Length)
			{

				if ((int)array[index] == 500)
				{
					DirectoryInfo directory = new DirectoryInfo(Plugin.MAPS_PATH);
					FileInfo[] files = directory.GetFiles("*.json");
					if (files.Length != 0)
					{
						MapPreset mapPresetFromIndex = array[index];
						GameManager.PreliminaryGameSettings.mapPreset = mapPresetFromIndex;

						string[] items = files.Select(file => file.Name).ToArray();

						MapLoader.customMapsList = __instance.CreateHorizontalList("gamesettings.custommaps", items, new Action<int>(MapLoader.OnCustomMapChanged), 0, null, 500);
						MapLoader.isListInstantiated = true;
					}
					else
					{
						MapPreset mapPresetFromIndex = array[0];
						GameManager.PreliminaryGameSettings.mapPreset = 0;
						NotificationManager.Notify(Localization.Get("gamesettings.nocustommapsfound"), Localization.Get("gamesettings.notavailable"), null, null);
						Console.Write("No maps found");
					}
				}
			}
			else
			{
				MapPreset mapPresetFromIndex = MapPreset.None;
				GameManager.PreliminaryGameSettings.mapPreset = mapPresetFromIndex;
			}

			__instance.UpdateOpponentList();
			GameManager.PreliminaryGameSettings.SaveToDisk();
			__instance.RefreshInfo();

			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnStartGameClicked))]
		private static void GameSetupScreen_OnStartGameClicked(int id, BaseEventData eventData)
		{
			MapLoader.isListInstantiated = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapPresetExtensions), nameof(MapPresetExtensions.GetLocalizationName))]
		private static void MapPresetExtensions_GetLocalizationName(ref string __result, MapPreset mapPreset)
		{
			if (mapPreset == (MapPreset)500)
			{
				__result = "gamesettings.map.custom";
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.RedrawScreen))]
		private static void GameSetupScreen_RedrawScreen()
		{
			MapLoader.map = null;
		}
	}
}
