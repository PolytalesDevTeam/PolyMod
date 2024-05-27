using Cpp2IL.Core.Extensions;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using UnityEngine;

namespace PolyMod
{
	internal static class ModLoader
	{
		internal static List<Script> scripts = new();
		internal static Dictionary<string, string> methodsDict = new();

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

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SpriteAtlasManager), nameof(SpriteAtlasManager.LoadSpriteAtlasTexture))]
		private static bool SpriteAtlasManager_LoadSpriteAtlasTexture(SpriteAtlasManager __instance, string atlas, Il2CppSystem.Action<Texture2D> completion)
		{
			Dictionary<string, Sprite> sprites = new();

			if (atlas != "Heads") return true;
			if (!__instance.cachedSprites.TryGetValue(atlas, out _))
			{
				__instance.cachedSprites[atlas] = new();
			}
			foreach (var sprite in __instance.cachedSprites[atlas])
			{
				if (sprite.Value.name != string.Empty) break;
				sprites.Add(sprite.Key, sprite.Value);
			}
			Texture2D customAtlas = new(1, 1);
			Rect[] rects = customAtlas.PackTextures(sprites.Select(i => i.Value.texture).ToArray(), 2);
			foreach (var sprite in sprites)
			{
				Sprite newSprite = Sprite.Create(
					customAtlas,
					rects[sprites.Values.ToList().IndexOf(sprite.Value)],
					new(0.5f, 0.5f)
				);
				__instance.cachedSprites[atlas][sprite.Key] = newSprite;
				__instance.spriteToAtlasName[newSprite] = atlas;
			}
			completion.Invoke(customAtlas);
			return false;
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

		private static void Init(JObject gld)
		{
			Directory.CreateDirectory(Plugin.MODS_PATH);
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());

			foreach (string modname in Directory.GetFiles(Plugin.MODS_PATH, "*.polymod"))
			{
				ZipArchive mod = new(File.OpenRead(modname));

				foreach (var entry in mod.Entries)
				{
					string name = entry.ToString();

					if (Path.GetFileName(name) == "script.lua")
					{
						Script script = new();
						script.Globals["patch"] = (Action<string, string, string>)Patch_;
						script.DoString(new StreamReader(entry.Open()).ReadToEnd());
						scripts.Add(script);
					}
					if (Path.GetFileName(name) == "patch.json")
					{
						Patch(gld, JObject.Parse(new StreamReader(entry.Open()).ReadToEnd()));
					}
					if (Path.GetExtension(name) == ".png")
					{
						Vector2 pivot = Path.GetFileNameWithoutExtension(name).Split("_")[0] switch
						{
							"field" => new(0.5f, 0.0f),
							"mountain" => new(0.5f, -0.375f),
							_ => new(0.5f, 0.5f),
						};
						Sprite sprite = BuildSprite(entry.ReadBytes(), pivot);
						GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(name), sprite);
						GameManager.GetSpriteAtlasManager().spriteToAtlasName.Add(sprite, "Heads");
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

			foreach (JToken jtoken in patch.SelectTokens("$.*.*").ToArray())
			{
				JObject token = jtoken.Cast<JObject>();

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
							PrefabManager.units.TryAdd((int)(UnitData.Type)idx, PrefabManager.units[(int)UnitData.Type.Scout]);
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

		public static void Test()
		{
			Console.WriteLine("You were hooked 🤣");
		}

		public static void TestC()
		{
			Console.WriteLine("I was called 🤯");
		}

		private static void Patcher() 
		{
			MethodBase caller = new StackTrace().GetFrame(1).GetMethod();
			string callerMethodName = caller.Name.Replace("::", "|");

			Console.WriteLine("The caller method is: " + callerMethodName);

			for (int i = 0; i < methodsDict.Count; i++)
			{
				var element = methodsDict.ElementAt(i);
				if (callerMethodName.Contains(element.Key))
				{
					string[] words = (element.Value.Split('|'));
					InvokeStringMethod(words[0], words[1]);
				}
			}
			//called on ANY lua patch
		}

		public static string InvokeStringMethod(string typeName, string methodName)
		{
			Type calledType = Type.GetType(typeName);

			String s = (String)calledType.InvokeMember(
				methodName,
				BindingFlags.InvokeMethod | BindingFlags.Public |
					BindingFlags.Static,
				null,
				null,
				null);

			return s;
		}
		
		private static void Patch_(string type, string method, string patch)
		{
			methodsDict[type + '|' + method] = type + '|' + patch;
			new Harmony(Guid.NewGuid().ToString()).Patch(AccessTools.Method(Type.GetType(type), method), new(AccessTools.Method(typeof(ModLoader), nameof(Patcher))));
		}
	}
}
