using Cpp2IL.Core.Extensions;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using System.IO.Compression;
using UnityEngine;

namespace PolyMod
{
	internal static class ModLoader
	{
		private static readonly Dictionary<int, string> _styles = new();

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
		private static void GameLogicData_Parse(JObject rootObject)
		{
			Init(rootObject);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.IsTribeUnlocked))]
		private static void PurchaseManager_IsTribeUnlocked(ref bool __result, TribeData.Type type)
		{
			__result = (int)type >= Plugin.AUTOIDX_STARTS_FROM || __result;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetTileSpriteAddress), new Type[] { typeof(Polytopia.Data.TerrainData.Type), typeof(string) })]
		private static void SpriteData_GetTileSpriteAddress(ref SpriteAddress __result, Polytopia.Data.TerrainData.Type terrain, string skinId)
		{
			__result = GetSprite(__result, EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetResourceSpriteAddress), new Type[] { typeof(ResourceData.Type), typeof(string) })]
		private static void SpriteData_GetResourceSpriteAddress(ref SpriteAddress __result, ResourceData.Type type, string skinOrTribeAsString)
		{
			__result = GetSprite(__result, EnumCache<ResourceData.Type>.GetName(type), skinOrTribeAsString);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetBuildingSpriteAddress), new Type[] { typeof(ImprovementData.Type), typeof(string) })]
		private static void SpriteData_GetBuildingSpriteAddress(ref SpriteAddress __result, ImprovementData.Type type, string climateOrSkinAsString)
		{
			__result = GetSprite(__result, EnumCache<ImprovementData.Type>.GetName(type), climateOrSkinAsString);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetUnitIconAddress))]
		private static void SpriteData_GetUnitIconAddress(ref SpriteAddress __result, UnitData.Type type)
		{
			__result = GetSprite(__result, "icon", EnumCache<UnitData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(string) })]
		private static void SpriteData_GetHeadSpriteAddress_1(ref SpriteAddress __result, string tribeOrSkin)
		{
			__result = GetSprite(__result, "head", tribeOrSkin);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(SpriteData.SpecialFaceIcon) })]
		private static void SpriteData_GetHeadSpriteAddress_2(ref SpriteAddress __result, SpriteData.SpecialFaceIcon specialId)
		{
			__result = GetSprite(__result, "head", specialId.ToString());
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAvatarPartSpriteAddress))]
		private static void SpriteData_GetAvatarPartSpriteAddress(ref SpriteAddress __result, string sprite)
		{
			__result = GetSprite(__result, sprite, "");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHouseAddresses), new Type[] { typeof(int), typeof(string), typeof(SkinType) })]
		private static void SpriteData_GetHouseAddresses(ref Il2CppReferenceArray<SpriteAddress> __result, int type, string styleId, SkinType skinType)
		{
			List<SpriteAddress> sprites = new()
			{
				GetSprite(__result[0], "house", styleId, type)
			};
			if (skinType != SkinType.Default)
			{
				sprites.Add(GetSprite(__result[1], "house", EnumCache<SkinType>.GetName(skinType), type));
			}
			__result = sprites.ToArray();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAddress))]
		public static bool SpriteData_GetAddress(ref SpriteAddress __result, PickerType pickerType, string skinID)
		{
			switch (pickerType)
			{
				case PickerType.Head:
					__result = SpriteData.GetHeadSpriteAddress(skinID);
					break;
				case PickerType.Unit:
					__result = new SpriteAddress("Units", skinID);
					break;
				case PickerType.TerrainFeature:
					__result = new SpriteAddress("TerrainFeatures", skinID);
					break;
				case PickerType.Roof:
					__result = new SpriteAddress("Units", "roof_" + skinID);
					break;
				case PickerType.PolytaurHead:
					__result = new SpriteAddress("Units", "polytaur_2_" + skinID);
					break;
				case PickerType.Animal:
					__result = SpriteData.GetResourceSpriteAddress(ResourceData.Type.Game, skinID);
					break;
				case PickerType.Fruit:
					__result = SpriteData.GetResourceSpriteAddress(ResourceData.Type.Fruit, skinID);
					break;
				case PickerType.Forest:
					__result = SpriteData.GetTileSpriteAddress(Polytopia.Data.TerrainData.Type.Forest, skinID);
					break;
				case PickerType.Mountain:
					__result = SpriteData.GetTileSpriteAddress(Polytopia.Data.TerrainData.Type.Mountain, skinID);
					break;
			}
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

		private static void Init(JObject gld)
		{
			Directory.CreateDirectory(Plugin.MODS_PATH);
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());

			foreach (string modname in Directory.GetFiles(Plugin.MODS_PATH, "*.polymod"))
			{
				ZipArchive mod = new(File.OpenRead(modname));

				ZipArchiveEntry? patch = mod.GetEntry("patch.json");
				if (patch != null)
				{
					try
					{
						Patch(gld, JObject.Parse(new StreamReader(patch.Open()).ReadToEnd()));
					}
					catch
					{
						Plugin.logger.LogError(modname + " gld patch parse error!");
						continue;
					}
				}

				foreach (var entry in mod.Entries)
				{
					string name = entry.ToString();

					if (Path.GetExtension(name) == ".png")
					{
						Vector2 pivot = Path.GetFileNameWithoutExtension(name).Split("_")[0] == "field" ? new(0.5f, 0.0f) : new(0.5f, 0.5f);
						GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(name), BuildSprite(entry.ReadBytes(), pivot));
					}
				}
			}
		}

		private static void Patch(JObject gld, JObject patch)
		{
			int idx = Plugin.AUTOIDX_STARTS_FROM;

			foreach (JToken jtoken in patch.SelectTokens("$.localizationData.*").ToArray())
			{
				JArray token = jtoken.Cast<JArray>();
				TermData term = LocalizationManager.Sources[0].AddTerm(Plugin.GetJTokenName(token).Replace('_', '.'));
				List<string> strings = new();
				for (int i = 0; i < token.Count; i++)
				{
					strings.Add((string)token[i]);
				}
				for (int i = 0; i < LocalizationManager.GetAllLanguages().Count - strings.Count; i++)
				{
					strings.Add(term.Term);
				}
				term.Languages = new Il2CppStringArray(strings.ToArray());
			}
			patch.Remove("localizationData");

			EnumCache<MapPreset>.AddMapping("Custom", (MapPreset)500);

			foreach (JToken jtoken in patch.SelectTokens("$.*.*").ToArray())
			{
				JObject token = jtoken.Cast<JObject>();

				if (token["climate"] != null && !int.TryParse((string)token["climate"], out _))
				{
					++idx;
					_styles.TryAdd(idx, (string)token["climate"]);
					token["climate"] = idx;
				}
				if (token["style"] != null && !int.TryParse((string)token["style"], out _))
				{
					++idx;
					_styles.TryAdd(idx, (string)token["style"]);
					token["style"] = idx;
				}
				if (token["idx"] != null && (int)token["idx"] == -1)
				{
					++idx;
					token["idx"] = idx;
					string id = Plugin.GetJTokenName(token);

					switch (Plugin.GetJTokenName(token, 2))
					{
						case "tribeData":
							EnumCache<TribeData.Type>.AddMapping(id, (TribeData.Type)idx);
							break;
						case "terrainData":
							EnumCache<Polytopia.Data.TerrainData.Type>.AddMapping(id, (Polytopia.Data.TerrainData.Type)idx);
							break;
						case "resourceData":
							EnumCache<ResourceData.Type>.AddMapping(id, (ResourceData.Type)idx);
							PrefabManager.resources.TryAdd((ResourceData.Type)idx, PrefabManager.resources[ResourceData.Type.Game]);
							break;
						case "taskData":
							EnumCache<TaskData.Type>.AddMapping(id, (TaskData.Type)idx);
							break;
						case "improvementData":
							EnumCache<ImprovementData.Type>.AddMapping(id, (ImprovementData.Type)idx);
							PrefabManager.improvements.TryAdd((ImprovementData.Type)idx, PrefabManager.improvements[ImprovementData.Type.CustomsHouse]);
							break;
						case "unitData":
							EnumCache<UnitData.Type>.AddMapping(id, (UnitData.Type)idx);
							//PrefabManager.units.TryAdd((UnitData.Type)idx, PrefabManager.units[UnitData.Type.Scout]); //what?
							break;
						case "techData":
							EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)idx);
							break;
					}
				}
			}

			gld.Merge(patch, Plugin.GLD_MERGE_SETTINGS);
		}

		private static SpriteAddress GetSprite(SpriteAddress sprite, string name, string style = "", int level = 0)
		{
			Console.WriteLine(style);
			if (int.TryParse(style, out int istyle) && _styles.ContainsKey(istyle))
			{
				style = _styles[istyle];
			}

			GetSpriteIfFound($"{name}__", ref sprite);
			GetSpriteIfFound($"{name}_{style}_", ref sprite);
			GetSpriteIfFound($"{name}__{level}", ref sprite);
			GetSpriteIfFound($"{name}_{style}_{level}", ref sprite);
			return sprite;
		}

		private static void GetSpriteIfFound(string id, ref SpriteAddress sprite)
		{
			if (GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].TryGetValue(id, out _))
			{
				sprite = new SpriteAddress("Heads", id);
			}
		}

		private static Sprite BuildSprite(byte[] data, Vector2 pivot)
		{
			Texture2D texture = new(1, 1);
			texture.LoadImage(data);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), pivot, 2112);
		}
	}
}
