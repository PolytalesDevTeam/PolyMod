using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal static class MapLoader
	{
		private static JObject? _map;
		private static bool _isListInstantiated = false;
		private static UIHorizontalList _customMapsList = new() { };

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			PreGenerate(ref state, ref settings);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate_(ref GameState state)
		{
			PostGenerate(ref state);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GeneratePlayerCapitalPositions))]
		private static void MapGenerator_GeneratePlayerCapitalPositions(ref Il2CppSystem.Collections.Generic.List<int> __result)
		{
			__result = GetCapitals(__result);
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
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = Plugin.CAMERA_MAXZOOM_CONSTANT;
			CameraController.Instance.techViewBounds = new(
				new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT), CameraController.Instance.techViewBounds.size
			);
			UnityEngine.GameObject.Find("TechViewWorldSpace").transform.position = new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT);
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
			_map = null;
			MapPreset[] array = GameSetupScreen.MapPresetDataSource.GetData(GameManager.PreliminaryGameSettings.GameType == GameType.Matchmaking);

			if (_isListInstantiated)
			{
				UnityEngine.Object.Destroy(_customMapsList.gameObject);
				_isListInstantiated = false;
			}

			if ((int)array[index] == 500)
			{
				string[] maps = Directory.GetFiles(Plugin.MAPS_PATH, "*.json");
				if (maps.Length != 0)
				{
					GameManager.PreliminaryGameSettings.mapPreset = array[index];
					_customMapsList = __instance.CreateHorizontalList("Maps", maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToArray(), new Action<int>(OnCustomMapChanged), 0, null, 500);
					_isListInstantiated = true;
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
			_isListInstantiated = false;
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
			_map = null;
		}

		private static Il2CppSystem.Collections.Generic.List<int> GetCapitals(Il2CppSystem.Collections.Generic.List<int> originalCapitals)
		{
			if (_map == null || _map["capitals"] == null)
			{
				return originalCapitals;
			}

			JArray jcapitals = _map["capitals"].Cast<JArray>();
			Il2CppSystem.Collections.Generic.List<int> capitals = new();
			for (int i = 0; i < jcapitals.Count; i++)
			{
				capitals.Add((int)jcapitals[i]);
			}

			if (capitals.Count < originalCapitals.Count)
			{
				throw new Exception("Too few capitals provided");
			}

			return capitals.GetRange(0, originalCapitals.Count);
		}

		private static void PreGenerate(ref GameState state, ref MapGeneratorSettings settings)
		{
			if (_map == null)
			{
				return;
			}
			ushort size = (ushort)_map["size"];
			state.Map = new(size, size);
			settings.mapType = PolytopiaBackendBase.Game.MapPreset.Dryland;
		}

		internal static void Init()
		{
			Directory.CreateDirectory(Plugin.MAPS_PATH);
			EnumCache<MapPreset>.AddMapping("Custom", (MapPreset)500);
		}

		private static void PostGenerate(ref GameState state)
		{
			if (_map == null)
			{
				return;
			}
			MapData originalMap = state.Map;

			for (int i = 0; i < originalMap.tiles.Length; i++)
			{
				TileData tile = originalMap.tiles[i];
				JToken tileJson = _map["map"][i];

				if (tileJson["skip"] != null && (bool)tileJson["skip"]) continue;

				tile.climate = (tileJson["climate"] == null || (int)tileJson["climate"] < 0 || (int)tileJson["climate"] > 16) ? 1 : (int)tileJson["climate"];
				tile.skinType = tileJson["skinType"] == null ? SkinType.Default : EnumCache<SkinType>.GetType((string)tileJson["skinType"]);
				tile.terrain = tileJson["terrain"] == null ? TerrainData.Type.None : EnumCache<TerrainData.Type>.GetType((string)tileJson["terrain"]);
				tile.resource = tileJson["resource"] == null ? null : new() { type = EnumCache<ResourceData.Type>.GetType((string)tileJson["resource"]) };

				if (tile.rulingCityCoordinates != tile.coordinates)
				{
					tile.improvement = tileJson["improvement"] == null ? null : new() { type = EnumCache<ImprovementData.Type>.GetType((string)tileJson["improvement"]) };
					if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
					{
						tile.improvement = new ImprovementState
						{
							type = ImprovementData.Type.City,
							founded = 0,
							level = 1,
							borderSize = 1,
							production = 1
						};
					}
				}
				else
				{
					if (_map["autoTribe"] != null && (bool)_map["autoTribe"])
					{
						state.TryGetPlayer(tile.owner, out PlayerState player);
						if (player == null)
						{
							throw new Exception($"Player {tile.owner} does not exist");
						}
						foreach (var tribe in PolytopiaDataManager.currentVersion.tribes.Values)
						{
							if (tile.climate == tribe.climate)
							{
								player.tribe = tribe.type;
							}
						}
					}
				}

				switch (tile.terrain)
				{
					case TerrainData.Type.Water:
						tile.altitude = -1;
						tile.shoreLines = TileData.ShorelineFlag.None;
						break;
					case TerrainData.Type.Ocean:
					case TerrainData.Type.Ice:
						tile.altitude = -2;
						tile.shoreLines = TileData.ShorelineFlag.None;
						break;
					case TerrainData.Type.Field:
					case TerrainData.Type.Forest:
						tile.altitude = 1;
						tile.shoreLines = TileData.ShorelineFlag.None;
						break;
					case TerrainData.Type.Mountain:
						tile.altitude = 2;
						tile.shoreLines = TileData.ShorelineFlag.None;
						break;
				}

				originalMap.tiles[i] = tile;
			}

			_map = null;
		}

		private static void OnCustomMapChanged(int index)
		{
			_map = JObject.Parse(File.ReadAllText(Directory.GetFiles(Plugin.MAPS_PATH, "*.json")[index]));
		}
	}
}
