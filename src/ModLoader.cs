using Cpp2IL.Core.Extensions;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;

namespace PolyMod
{
	internal static class ModLoader
	{
		private static List<JObject> _patches = new();
		private static Dictionary<string, byte[]> _textures = new();
		private static Dictionary<string, Sprite> _sprites = new();
		private static Dictionary<string, AudioClip> _audios = new();
		public static Dictionary<string, int> gldDictionary = new ();
		public static int tribesCount = (int)Enum.GetValues(typeof(TribeData.Type)).Cast<TribeData.Type>().Last();
		public static int techCount = (int)Enum.GetValues(typeof(TechData.Type)).Cast<TechData.Type>().Last();
		public static int unitCount = (int)Enum.GetValues(typeof(UnitData.Type)).Cast<UnitData.Type>().Last();
		public static int improvementsCount = (int)Enum.GetValues(typeof(ImprovementData.Type)).Cast<ImprovementData.Type>().Last();
		public static int terrainCount = (int)Enum.GetValues(typeof(Polytopia.Data.TerrainData.Type)).Cast<Polytopia.Data.TerrainData.Type>().Last();
		public static int resourceCount = (int)Enum.GetValues(typeof(ResourceData.Type)).Cast<ResourceData.Type>().Last();
		public static int taskCount = (int)Enum.GetValues(typeof(TaskData.Type)).Cast<TaskData.Type>().Last();

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
		private static void GameLogicData_Parse(JObject rootObject)
		{
			Load(rootObject);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.IsTribeUnlocked))]
		private static void PurchaseManager_IsTribeUnlocked(ref bool __result, TribeData.Type type)
		{
			__result = (int)type >= ((int)Enum.GetValues(typeof(TribeData.Type)).Cast<TribeData.Type>().Last() + 1) || __result;
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
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(SkinType) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, SkinType skin)
		{
			__result = GetSprite(__result, "head", EnumCache<SkinType>.GetName(skin));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(TribeData.Type) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, TribeData.Type type)
		{
			__result = GetSprite(__result, "head", EnumCache<TribeData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(SpriteData.SpecialFaceIcon) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, SpriteData.SpecialFaceIcon specialId)
		{
			__result = GetSprite(__result, "head", specialId.ToString());
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAvatarPartSpriteAddress))]
		private static void SpriteData_GetAvatarPartSpriteAddress(ref SpriteAddress __result, string sprite)
		{
			__result = GetSprite(__result, sprite, "");
		}

		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHouseAddresses), new Type[] { typeof(int), typeof(string), typeof(SkinType) })]
		// private static void SpriteData_GetHouseAddresses(ref Il2CppReferenceArray<SpriteAddress> __result, int type, string styleId, SkinType skinType)
		// {
		// 	List<SpriteAddress> sprites = new()
		// 	{
		// 		GetSprite(__result[0], "house", styleId, type)
		// 	};
		// 	if (skinType != SkinType.Default)
		// 	{
		// 		sprites.Add(GetSprite(__result[1], "house", EnumCache<SkinType>.GetName(skinType), type));
		// 	}
		// 	__result = sprites.ToArray();
		// }

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
				case PickerType.Monument_1:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument1, skinID);
					break;
				case PickerType.Monument_2:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument2, skinID);
					break;
				case PickerType.Monument_3:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument3, skinID);
					break;
				case PickerType.Monument_4:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument4, skinID);
					break;
				case PickerType.Monument_5:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument5, skinID);
					break;
				case PickerType.Monument_6:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument6, skinID);
					break;
				case PickerType.Monument_7:
					__result = SpriteData.GetBuildingSpriteAddress(ImprovementData.Type.Monument7, skinID);
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

		[HarmonyPostfix]
		[HarmonyPatch(typeof(City), nameof(City.UpdateObject))]
		private static void City_UpdateObject(City __instance)
		{
			Console.Write((int)__instance.Owner.tribe);
			if (__instance.Owner != null && (int)__instance.Owner.tribe > 17){
				__instance.cityRenderer.Tribe = TribeData.Type.Imperius;
				__instance.cityRenderer.SkinType = SkinType.Default;
				__instance.cityRenderer.RefreshCity();
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.GetUnlockItems))]
		private static void TechItem_GetUnlockItems(TechData techData, PlayerState playerState, bool onlyPickFirstItem = false)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.SetupComplete))]
		private static void TechItem_SetupComplete()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UICityPlot), nameof(UICityPlot.AddHouse))]
		private static void UICityPlot_AddHouse()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapRenderer), nameof(MapRenderer.LateUpdate))]
		private static void MapRenderer_LateUpdate()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Unit), nameof(Unit.SetVisible))]
		private static void Unit_SetVisible(Unit __instance, bool isVisible)
		{
			//Console.Write("Parent: " + __instance.transform.parent + ", of unit with type: " + __instance.UnitData.type);
			Transform unitTransform = new Transform();
			foreach (Transform unit in __instance.transform.parent)
			{
				unitTransform = unit.gameObject.transform;
			}
			Transform spriteContainerTransform = unitTransform.Find("SpriteContainer");
			if (spriteContainerTransform != null)
			{
				GameObject spriteContainer = spriteContainerTransform.gameObject;
				Transform headTransform = spriteContainer.transform.Find("Head");
				Transform bodyTransform = spriteContainer.transform.Find("Body");
				if(headTransform != null){
					SpriteRenderer sr = headTransform.gameObject.GetComponent<SpriteRenderer>();
					if(sr != null){
						foreach (var kvp in _sprites) {
							if (kvp.Value) Console.WriteLine(kvp.Key);
						}
						if(sr.sprite.name == "head" || sr.sprite.name == ""){
							string idKey = gldDictionary.FirstOrDefault(x => x.Value == (int)__instance.Owner.tribe).Key;
							string spritesKey = "head_" + idKey + "_";
							Console.Write("Found custom tribe's head, changing sprite to: " + spritesKey + ".png");
							sr.sprite = _sprites[spritesKey];
						}
					}
				}
				if(bodyTransform != null){}
			}
		}

		public static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(ModLoader));
			Directory.CreateDirectory(Plugin.MODS_PATH);
			string[] mods = Directory.GetFiles(Plugin.MODS_PATH, "*.polymod").Union(Directory.GetFiles(Plugin.MODS_PATH, "*.polytale")).Union(Directory.GetFiles(Plugin.MODS_PATH, "*.zip")).ToArray();
			foreach (string modname in mods)
			{
				ZipArchive mod = new(File.OpenRead(modname));

				foreach (var entry in mod.Entries)
				{
					string name = entry.ToString();

					if (Path.GetExtension(name) == ".dll")
					{
						PolyscriptLoad(entry.ReadBytes());
					}
					if (Path.GetFileName(name) == "patch.json")
					{
						_patches.Add(JObject.Parse(new StreamReader(entry.Open()).ReadToEnd()));
					}
					if (Path.GetExtension(name) == ".png")
					{
						_textures.Add(name, entry.ReadBytes());
					}
				}
			}
		}

		public static void Load(JObject gameLogicdata)
		{
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());
			foreach (var patch in _patches){
				try
				{
					GameLogicDataPatch(gameLogicdata, patch);
				} catch {}
			}
			foreach (var sprite_ in _textures){
				Vector2 pivot = Path.GetFileNameWithoutExtension(sprite_.Key).Split("_")[0] switch
				{
					"field" => new(0.5f, 0.0f),
					"mountain" => new(0.5f, -0.375f),
					_ => new(0.5f, 0.5f),
				};
				Sprite sprite = Api.BuildSprite(sprite_.Value, pivot);
				GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(sprite_.Key), sprite);
				_sprites.Add(Path.GetFileNameWithoutExtension(sprite_.Key), sprite);
			}
		}

		private static void PolyscriptLoad(byte[] polyscriptData)
		{
			try{
				Assembly assembly = Assembly.Load(polyscriptData);
				foreach (Type type in assembly.GetTypes())
				{
					type.GetMethod("Load")?.Invoke(null, null);
				}
			}
			catch(Exception exception){
				Plugin.logger.LogInfo(exception.Message);
			}
		}
		private static void GameLogicDataPatch(JObject gld, JObject patch)
		{
			foreach (JToken jtoken in patch.SelectTokens("$.localizationData.*").ToArray())
			{
				JArray token = jtoken.Cast<JArray>();
				TermData term = LocalizationManager.Sources[0].AddTerm(Api.GetJTokenName(token).Replace('_', '.'));
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

			foreach (JToken jtoken in patch.SelectTokens("$.*.*").ToArray())
			{
				JObject token = jtoken.Cast<JObject>();

				if (token["idx"] != null && (int)token["idx"] == -1)
				{
					string id = Api.GetJTokenName(token);
					string dataType = Api.GetJTokenName(token, 2);
					switch (dataType)
					{
						case "tribeData":
							++tribesCount;
							token["idx"] = tribesCount;
							gldDictionary[id] = tribesCount;
							EnumCache<TribeData.Type>.AddMapping(id, (TribeData.Type)tribesCount);
							break;
						case "techData":
							++techCount;
							token["idx"] = techCount;
							gldDictionary[id] = techCount;
							EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)techCount);
							break;
						case "unitData":
							++unitCount;
							token["idx"] = unitCount;
							gldDictionary[id] = unitCount;
							EnumCache<UnitData.Type>.AddMapping(id, (UnitData.Type)unitCount);
							PrefabManager.units.TryAdd((int)(UnitData.Type)unitCount, PrefabManager.units[(int)UnitData.Type.Scout]);
							break;
						case "improvementData":
							++improvementsCount;
							token["idx"] = improvementsCount;
							gldDictionary[id] = improvementsCount;
							EnumCache<ImprovementData.Type>.AddMapping(id, (ImprovementData.Type)improvementsCount);
							PrefabManager.improvements.TryAdd((ImprovementData.Type)improvementsCount, PrefabManager.improvements[ImprovementData.Type.CustomsHouse]);
							break;
						case "terrainData":
							++terrainCount;
							token["idx"] = terrainCount;
							gldDictionary[id] = terrainCount;
							EnumCache<Polytopia.Data.TerrainData.Type>.AddMapping(id, (Polytopia.Data.TerrainData.Type)terrainCount);
							break;
						case "resourceData":
							++resourceCount;
							token["idx"] = resourceCount;
							gldDictionary[id] = resourceCount;
							EnumCache<ResourceData.Type>.AddMapping(id, (ResourceData.Type)resourceCount);
							PrefabManager.resources.TryAdd((ResourceData.Type)resourceCount, PrefabManager.resources[ResourceData.Type.Game]);
							break;
						case "taskData":
							++taskCount;
							token["idx"] = taskCount;
							gldDictionary[id] = taskCount;
							EnumCache<TaskData.Type>.AddMapping(id, (TaskData.Type)taskCount);
							break;
					}
				}
			}

			gld.Merge(patch, Plugin.GLD_MERGE_SETTINGS);
		}

		private static SpriteAddress GetSprite(SpriteAddress sprite, string name, string style = "", int level = 0)
		{
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
	}
}
