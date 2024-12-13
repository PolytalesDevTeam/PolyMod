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
		private static List<JObject> _patches = new();
		private static Dictionary<string, byte[]> _textures = new();
		public static Dictionary<string, Sprite> sprites = new();
		private static Dictionary<string, AudioClip> _audios = new();
		public static Dictionary<string, int> gldDictionary = new();
		public static Dictionary<int, string> gldDictionaryInversed = new();
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

		public static void Init()
		{
			_stopwatch.Start();
			Harmony.CreateAndPatchAll(typeof(ModLoader));
			Directory.CreateDirectory(Plugin.MODS_PATH);
			string[] mods = Directory.GetFiles(Plugin.MODS_PATH, "*.polymod").Union(Directory.GetFiles(Plugin.MODS_PATH, "*.polytale")).Union(Directory.GetFiles(Plugin.MODS_PATH, "*.zip")).ToArray();
			string[] folders = Directory.GetDirectories(Plugin.MODS_PATH);
			Dictionary<int, Tuple<string, byte[]>> filesBytes = new Dictionary<int, Tuple<string, byte[]>>();
			foreach (string modname in mods)
			{
				ZipArchive mod = new(File.OpenRead(modname));
				foreach (var entry in mod.Entries)
				{
					filesBytes.Add(filesBytes.Count, new Tuple<string, byte[]> ( entry.ToString(), entry.ReadBytes() ));
				}
			}
			foreach(var folder in folders){
				foreach(var filePath in Directory.GetFiles(folder)){
					var entry = File.OpenRead(filePath);
					filesBytes.Add(filesBytes.Count, new Tuple<string, byte[]> ( filePath.ToString(), entry.ReadBytes() ));
				}
			}
			for (int i = 0; i < filesBytes.Count; i++)
			{
				string name = filesBytes[i].Item1;
				byte[] bytes = filesBytes[i].Item2;
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
						}
					}
				}
				if (Path.GetFileName(name) == "patch.json")
				{
					Plugin.logger.LogInfo($"Registried patch from {name}"); //TODO: fix
					_patches.Add(JObject.Parse(new StreamReader(new MemoryStream(bytes)).ReadToEnd()));
				}
				if (Path.GetExtension(name) == ".png")
				{
					_textures.Add(name, bytes);
				}
			}
			_stopwatch.Stop();
		}

		public static void Load(JObject gameLogicdata)
		{
			_stopwatch.Start();
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());
			foreach (var patch in _patches)
			{
				try
				{
					GameLogicDataPatch(gameLogicdata, patch);
				}
				catch(Exception ex) {
					Plugin.logger.LogInfo($"Patch error: {ex.Message}");
				}
			}
			gldDictionaryInversed = gldDictionary.ToDictionary((i) => i.Value, (i) => i.Key);
			if(shouldInitializeSprites){
				foreach (var sprite_ in _textures)
				{
					Vector2 pivot = Path.GetFileNameWithoutExtension(sprite_.Key).Split("_")[0] switch
					{
						"field" => new(0.5f, 0.0f),
						"mountain" => new(0.5f, -0.375f),
						_ => new(0.5f, 0.5f),
					};
					Sprite sprite = SpritesLoader.BuildSprite(sprite_.Value, pivot);
					GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(sprite_.Key), sprite);
					sprites.Add(Path.GetFileNameWithoutExtension(sprite_.Key), sprite);
				}
				shouldInitializeSprites = false;
			}
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
