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
		private static int _autoidx = Plugin.AUTOIDX_STARTS_FROM;
		private static List<JObject> _patches = new();
		private static Dictionary<string, Sprite> _sprites = new();
		private static Dictionary<string, AudioClip> _audios = new();
		public static Dictionary<string, int> gldDictionary = new();

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
			__result = (int)type >= Plugin.AUTOIDX_STARTS_FROM || __result;
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
			if (__instance.Owner != null && (int)__instance.Owner.tribe > 17)
			{
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
				if (headTransform != null)
				{
					SpriteRenderer sr = headTransform.gameObject.GetComponent<SpriteRenderer>();
					if (sr != null)
					{
						foreach (var kvp in _sprites)
						{
							if (kvp.Value) Console.WriteLine(kvp.Key);
						}
						if (sr.sprite.name == "head" || sr.sprite.name == "")
						{
							string idKey = gldDictionary.FirstOrDefault(x => x.Value == (int)__instance.Owner.tribe).Key;
							string spritesKey = "head_" + idKey + "_";
							Console.Write("Found custom tribe's head, changing sprite to: " + spritesKey + ".png");
							sr.sprite = _sprites[spritesKey];
						}
					}
				}
				if (bodyTransform != null) { }
			}
		}

		public static void Init()
		{
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
						try
						{
							Assembly assembly = Assembly.Load(entry.ReadBytes());
							foreach (Type type in assembly.GetTypes())
							{
								type.GetMethod("Load")?.Invoke(null, null);
							}
						}
						catch (Exception exception)
						{
							Plugin.logger.LogInfo(exception.Message);
						}
					}
					if (Path.GetFileName(name) == "patch.json")
					{
						_patches.Add(JObject.Parse(new StreamReader(entry.Open()).ReadToEnd()));
					}
					if (Path.GetExtension(name) == ".png")
					{
						Vector2 pivot = Path.GetFileNameWithoutExtension(name).Split("_")[0] switch
						{
							"field" => new(0.5f, 0.0f),
							"mountain" => new(0.5f, -0.375f),
							_ => new(0.5f, 0.5f),
						};
						_sprites.Add(Path.GetFileNameWithoutExtension(name), Api.BuildSprite(entry.ReadBytes(), pivot));
					}
				}
			}
		}

		public static void Load(JObject gld)
		{
			foreach (var patch in _patches)
			{
				try
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
							++_autoidx;
							token["idx"] = _autoidx;
							string id = Api.GetJTokenName(token);
							gldDictionary[id] = _autoidx;
							switch (Api.GetJTokenName(token, 2))
							{
								case "tribeData":
									EnumCache<TribeData.Type>.AddMapping(id, (TribeData.Type)_autoidx);
									break;
								case "terrainData":
									EnumCache<Polytopia.Data.TerrainData.Type>.AddMapping(id, (Polytopia.Data.TerrainData.Type)_autoidx);
									break;
								case "resourceData":
									EnumCache<ResourceData.Type>.AddMapping(id, (ResourceData.Type)_autoidx);
									PrefabManager.resources.TryAdd((ResourceData.Type)_autoidx, PrefabManager.resources[ResourceData.Type.Game]);
									break;
								case "taskData":
									EnumCache<TaskData.Type>.AddMapping(id, (TaskData.Type)_autoidx);
									break;
								case "improvementData":
									EnumCache<ImprovementData.Type>.AddMapping(id, (ImprovementData.Type)_autoidx);
									PrefabManager.improvements.TryAdd((ImprovementData.Type)_autoidx, PrefabManager.improvements[ImprovementData.Type.CustomsHouse]);
									break;
								case "unitData":
									EnumCache<UnitData.Type>.AddMapping(id, (UnitData.Type)_autoidx);
									PrefabManager.units.TryAdd((int)(UnitData.Type)_autoidx, PrefabManager.units[(int)UnitData.Type.Scout]);
									break;
								case "techData":
									EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)_autoidx);
									break;
							}
						}
					}

					gld.Merge(patch, Plugin.GLD_MERGE_SETTINGS);
				}
				catch { }
			}
		}
	}
}
