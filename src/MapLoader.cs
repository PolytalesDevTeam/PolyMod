using Newtonsoft.Json.Linq;
using Polytopia.Data;

namespace PolyMod
{
	internal static class MapLoader
	{
		internal static JObject? map;
		internal static bool isListInstantiated = false;
		internal static UIHorizontalList customMapsList = new UIHorizontalList { };
		internal static Il2CppSystem.Collections.Generic.List<int> GetCapitals(Il2CppSystem.Collections.Generic.List<int> originalCapitals, int width, int playerCount)
		{
			if (map == null || map["capitals"] == null)
			{
				return originalCapitals;
			}

			JArray jcapitals = map["capitals"].Cast<JArray>();
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

		internal static void PreGenerate(ref GameState state, ref MapGeneratorSettings settings)
		{
			if (map == null)
			{
				return;
			}
			ushort size = (ushort)map["size"];

			if (size < Plugin.MAP_MIN_SIZE || size > Plugin.MAP_MAX_SIZE)
			{
				throw new Exception($"The map size must be between {Plugin.MAP_MIN_SIZE} and {Plugin.MAP_MAX_SIZE}");
			}
			state.Map = new(size, size);
			settings.mapType = PolytopiaBackendBase.Game.MapPreset.Dryland;
		}

		internal static void PostGenerate(ref GameState state)
		{
			if (map == null)
			{
				return;
			}
			MapData originalMap = state.Map;

			for (int i = 0; i < originalMap.tiles.Length; i++)
			{
				TileData tile = originalMap.tiles[i];
				JToken tileJson = map["map"][i];

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
					if (map["autoTribe"] != null && (bool)map["autoTribe"])
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

			map = null;
		}

		public static void OnCustomMapChanged(int index)
		{
			map = JObject.Parse(File.ReadAllText(Directory.GetFiles(Plugin.MAPS_PATH, "*.json")[index]));
		}
	}
}
