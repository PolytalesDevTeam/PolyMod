using Cpp2IL.Core.Extensions;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using LibCpp2IL;
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
		private static int _autoidx = Plugin.AUTOIDX_STARTS_FROM;
		private static Stopwatch _stopwatch = new();
		public static Dictionary<string, Sprite> sprites = new();
		public static Dictionary<string, int> gldDictionary = new();
		public static Dictionary<int, string> gldDictionaryInversed = new();
		public static Dictionary<int, Tuple<string, string, List<Tuple<string, byte[]>>>> modsData = new Dictionary<int, Tuple<string, string, List<Tuple<string, byte[]>>>>();
		public static Dictionary<int, int> climateToTribeData = new();
		public static int climateAutoidx = (int)Enum.GetValues(typeof(TribeData.Type)).Cast<TribeData.Type>().Last();
		public static bool shouldInitializeSprites = true;


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

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.IsSkinUnlocked))]
		private static void PurchaseManager_IsSkinUnlocked(ref bool __result, SkinType skinType)
		{
			__result = ((int)skinType >= Plugin.AUTOIDX_STARTS_FROM && (int)skinType != 2000)  || __result;
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
		[HarmonyPatch(typeof(SelectTribePopup), nameof(SelectTribePopup.SetDescription))]
		private static void SetDescription(SelectTribePopup __instance)
		{
			if((int)__instance.SkinType >= Plugin.AUTOIDX_STARTS_FROM){
				__instance.Description = Localization.Get(__instance.SkinType.GetLocalizationDescriptionKey()) + "\n\n" + Localization.GetSkinned(__instance.SkinType, __instance.tribeData.description2, new Il2CppSystem.Object[]
				{
					__instance.tribeName,
					Localization.Get(__instance.startTechSid, Array.Empty<Il2CppSystem.Object>())
				});
			}
		}

		public static void Init()
		{
			_stopwatch.Start();
			Harmony.CreateAndPatchAll(typeof(ModLoader));
			Directory.CreateDirectory(Plugin.MODS_PATH);
			string[] mods = Directory.GetFiles(Plugin.MODS_PATH, "*.polymod").Union(Directory.GetFiles(Plugin.MODS_PATH, "*.polytale")).Union(Directory.GetFiles(Plugin.MODS_PATH, "*.zip")).ToArray();
			string[] folders = Directory.GetDirectories(Plugin.MODS_PATH);
			int modsCount = 0;
			foreach (string modname in mods)
			{
				ZipArchive mod = new(File.OpenRead(modname));
				List<Tuple<string, byte[]>> files = new List<Tuple<string, byte[]>>();
				foreach (var entry in mod.Entries)
				{
					files.Add(new Tuple<string, byte[]>(entry.ToString(), entry.ReadBytes()));
				}
				modsData[modsCount] = new Tuple<string, string, List<Tuple<string, byte[]>>>(modname, "loaded successfully", files);
				modsCount++;
			}
			foreach(var folder in folders){
				List<Tuple<string, byte[]>> files = new List<Tuple<string, byte[]>>();
				foreach(var filePath in Directory.GetFiles(folder)){
					var entry = File.OpenRead(filePath);
					files.Add(new Tuple<string, byte[]>(filePath.ToString(), entry.ReadBytes()));
				}
				modsData[modsCount] = new Tuple<string, string, List<Tuple<string, byte[]>>>(folder, "loaded successfully", files);
				modsCount++;
			}
			for (int i = 0; i < modsData.Count; i++)
			{
				Tuple<string, string, List<Tuple<string, byte[]>>> data = modsData[i];
				string modStatus = data.Item2;
				List<Tuple<string, byte[]>> filesInfo = data.Item3;
				foreach (Tuple<string, byte[]> fileInfo in filesInfo)
				{
					string name = fileInfo.Item1;
					byte[] bytes = fileInfo.Item2;
					if (Path.GetExtension(name) == ".dll")
					{
						try
						{
							Assembly assembly = Assembly.Load(bytes);
							foreach (Type type in assembly.GetTypes())
							{
								type.GetMethod("Load")?.Invoke(null, null);
							}
						}
						catch (TargetInvocationException exception)
						{
							if (exception.InnerException != null)
							{
								Plugin.logger.LogError(exception.InnerException.Message);
								modStatus = "had loading error";
							}
						}
					}
				}
				Tuple<string, string, List<Tuple<string, byte[]>>> modifiedData = new Tuple<string, string, List<Tuple<string, byte[]>>>(data.Item1, modStatus, filesInfo);
				modsData[i] = modifiedData;
			}
			_stopwatch.Stop();
		}

		public static void Load(JObject gameLogicdata)
		{
			_stopwatch.Start();
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());
			for (int i = 0; i < modsData.Count; i++)
			{
				Tuple<string, string, List<Tuple<string, byte[]>>> data = modsData[i];
				string modStatus = data.Item2;
				List<Tuple<string, byte[]>> filesInfo = data.Item3;
				foreach (Tuple<string, byte[]> fileInfo in filesInfo)
				{
					string name = fileInfo.Item1;
					byte[] bytes = fileInfo.Item2;
					if (Path.GetFileName(name) == "patch.json")
					{
						try
						{
							Plugin.logger.LogInfo($"Registried patch from {name}");
							GameLogicDataPatch(gameLogicdata, JObject.Parse(new StreamReader(new MemoryStream(bytes)).ReadToEnd()));
						}
						catch(Exception ex) {
							Plugin.logger.LogInfo($"Patch error: {ex.Message}");
							modStatus = "had loading error";
						}
					}
					if (Path.GetExtension(name) == ".png" && shouldInitializeSprites)
					{
						Vector2 pivot = Path.GetFileNameWithoutExtension(name).Split("_")[0] switch
						{
							"field" => new(0.5f, 0.0f),
							"mountain" => new(0.5f, -0.375f),
							_ => new(0.5f, 0.5f),
						};
						Sprite sprite = SpritesLoader.BuildSprite(bytes, pivot);
						GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(name), sprite);
						sprites.Add(Path.GetFileNameWithoutExtension(name), sprite);
					}
				}
				Tuple<string, string, List<Tuple<string, byte[]>>> modifiedData = new Tuple<string, string, List<Tuple<string, byte[]>>>(data.Item1, modStatus, filesInfo);
				modsData[i] = modifiedData;
			}
			gldDictionaryInversed = gldDictionary.ToDictionary((i) => i.Value, (i) => i.Key);
			shouldInitializeSprites = false;
			_stopwatch.Stop();
			Plugin.logger.LogInfo($"Elapsed time: {_stopwatch.ElapsedMilliseconds}ms");
		}

		private static void GameLogicDataPatch(JObject gld, JObject patch)
		{
			try
			{
				foreach (JToken jtoken in patch.SelectTokens("$.localizationData.*").ToArray())
				{
					JObject token = jtoken.Cast<JObject>();
					string name = GetJTokenName(token).Replace('_', '.');
					if (name.StartsWith("tribeskins")) name = "TribeSkins/" + name;
					TermData term = LocalizationManager.Sources[0].AddTerm(name);

					List<string> strings = new List<string>();
					Il2CppSystem.Collections.Generic.List<string> availableLanguages = LocalizationManager.GetAllLanguages();

					foreach (string language in availableLanguages)
					{
						if (token.TryGetValue(language, out JToken localizedString))
						{
							strings.Add((string)localizedString);
						}
						else
						{
							strings.Add(term.Term);
						}
					}
					term.Languages = new Il2CppStringArray(strings.ToArray());
				}

				patch.Remove("localizationData");

				foreach (JToken jtoken in patch.SelectTokens("$.tribeData.*").ToArray())
				{
					JObject token = jtoken.Cast<JObject>();

					if (token["skins"] != null)
					{
						JArray skinsArray = token["skins"].Cast<JArray>();
						Dictionary<string, int> skinsToReplace = new Dictionary<string, int>();

						foreach (var skin in skinsArray._values)
						{
							string skinValue = skin.ToString();

							if (!Enum.TryParse<SkinType>(skinValue, out _))
							{
								Plugin.logger.LogInfo($"Creating mapping for non-existent SkinType: {skinValue}");
								EnumCache<SkinType>.AddMapping(skinValue, (SkinType)_autoidx);
								skinsToReplace[skinValue] = _autoidx;
								gldDictionary[skinValue] = _autoidx;
								_autoidx++;
							}
						}

						foreach (var entry in skinsToReplace)
						{
							if (skinsArray._values.Contains(entry.Key))
							{
								skinsArray._values.Remove(entry.Key);
								skinsArray._values.Add(entry.Value);
							}
						}
					}
				}

				foreach (JToken jtoken in patch.SelectTokens("$.*.*").ToArray())
				{
					JObject token = jtoken.Cast<JObject>();

					if (token["idx"] != null && (int)token["idx"] == -1)
					{
						string id = GetJTokenName(token);
						string dataType = GetJTokenName(token, 2);
						Plugin.logger.LogInfo("Creating mapping for " + dataType + " with id: " + id + "and index: " + (_autoidx + 1));
						switch (dataType)
						{
							case "tribeData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<TribeData.Type>.AddMapping(id, (TribeData.Type)_autoidx);
								climateToTribeData[climateAutoidx] = _autoidx;
								++climateAutoidx;
								break;
							case "techData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)_autoidx);
								break;
							case "unitData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<UnitData.Type>.AddMapping(id, (UnitData.Type)_autoidx);
								PrefabManager.units.TryAdd((int)(UnitData.Type)_autoidx, PrefabManager.units[(int)UnitData.Type.Scout]);
								break;
							case "improvementData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<ImprovementData.Type>.AddMapping(id, (ImprovementData.Type)_autoidx);
								PrefabManager.improvements.TryAdd((ImprovementData.Type)_autoidx, PrefabManager.improvements[ImprovementData.Type.CustomsHouse]);
								break;
							case "terrainData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<Polytopia.Data.TerrainData.Type>.AddMapping(id, (Polytopia.Data.TerrainData.Type)_autoidx);
								break;
							case "resourceData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<ResourceData.Type>.AddMapping(id, (ResourceData.Type)_autoidx);
								PrefabManager.resources.TryAdd((ResourceData.Type)_autoidx, PrefabManager.resources[ResourceData.Type.Game]);
								break;
							case "taskData":
								++_autoidx;
								token["idx"] = _autoidx;
								gldDictionary[id] = _autoidx;
								EnumCache<TaskData.Type>.AddMapping(id, (TaskData.Type)_autoidx);
								break;
						}
					}
				}

				gld.Merge(patch, Plugin.GLD_MERGE_SETTINGS);
			}
			catch (Exception exception)
			{
				Plugin.logger.LogError(exception.Message);
			}
		}

		internal static Sprite? GetSprite(string name, string style = "", int level = 0)
		{
			Sprite? sprite = null;
			name = name.ToLower();
			style = style.ToLower();
			sprite = sprites.GetOrDefault($"{name}__", sprite);
			sprite = sprites.GetOrDefault($"{name}_{style}_", sprite);
			sprite = sprites.GetOrDefault($"{name}__{level}", sprite);
			sprite = sprites.GetOrDefault($"{name}_{style}_{level}", sprite);
			return sprite;
		}

		public static string GetJTokenName(JToken token, int n = 1)
		{
			return token.Path.Split('.')[^n];
		}
	}
}
