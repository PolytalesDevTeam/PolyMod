using Cpp2IL.Core.Extensions;
using HarmonyLib;
using I2.Loc;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using LibCpp2IL;
using Newtonsoft.Json.Linq;
using PolyMod.Json;
using Polytopia.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using UnityEngine;

namespace PolyMod
{
	internal static class ModLoader
	{
		internal class Mod
		{
			internal record Dependency(string id, Version min, Version max, bool required = true);
			internal record Manifest(string id, Version version, string[] authors, Dependency[] dependencies);
			internal record File(string name, byte[] bytes);
			internal enum Status { SUCCESS, ERROR };

			internal Version version;
			internal string[] authors;
			internal Status status;
			internal List<File> files;

			internal Mod(Version version, string[] authors, Status status, List<File> files)
			{
				this.version = version;
				this.authors = authors;
				this.status = status;
				this.files = files;
			}

			internal string GetPrettyStatus()
			{
				return status switch
				{
					Status.SUCCESS => "loaded successfully",
					Status.ERROR => "had loading error",
					_ => throw new InvalidOperationException(),
				};
			}
		}

		private static int autoidx = Plugin.AUTOIDX_STARTS_FROM;
		private static readonly Stopwatch stopwatch = new();
		public static Dictionary<string, Sprite> sprites = new();
		public static Dictionary<string, int> gldDictionary = new();
		public static Dictionary<int, string> gldDictionaryInversed = new();
		public static Dictionary<string, Mod> mods = new();
		public static List<Mod.Dependency> dependencies = new();
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
			__result = ((int)skinType >= Plugin.AUTOIDX_STARTS_FROM && (int)skinType != 2000) || __result;
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
			if ((int)__instance.SkinType >= Plugin.AUTOIDX_STARTS_FROM)
			{
				__instance.Description = Localization.Get(__instance.SkinType.GetLocalizationDescriptionKey()) + "\n\n" + Localization.GetSkinned(__instance.SkinType, __instance.tribeData.description2, new Il2CppSystem.Object[]
				{
					__instance.tribeName,
					Localization.Get(__instance.startTechSid, Array.Empty<Il2CppSystem.Object>())
				});
			}
		}

		internal static void Init()
		{
			stopwatch.Start();
			Harmony.CreateAndPatchAll(typeof(ModLoader));

			Directory.CreateDirectory(Plugin.MODS_PATH);
			string[] modFiles = Directory.GetDirectories(Plugin.MODS_PATH)
				.Union(Directory.GetFiles(Plugin.MODS_PATH, "*.polymod"))
				.Union(Directory.GetFiles(Plugin.MODS_PATH, "*.zip"))
				.ToArray();
			foreach (var modFile in modFiles)
			{
				Mod.Manifest? manifest = null;
				List<Mod.File> files = new();

				if (Directory.Exists(modFile))
				{
					foreach (var file in Directory.GetFiles(modFile))
					{
						if (Path.GetFileName(file) == "manifest.json")
						{
							manifest = JsonSerializer.Deserialize<Mod.Manifest>(
								File.ReadAllBytes(file),
								new JsonSerializerOptions()
								{
									Converters = { new VersionJson() },
								}
							);
							continue;
						}
						files.Add(new(Path.GetFileName(file), File.ReadAllBytes(file)));
					}
				}
				else
				{
					foreach (var entry in new ZipArchive(File.OpenRead(modFile)).Entries)
					{
						if (entry.FullName == "manifest.json")
						{
							manifest = JsonSerializer.Deserialize<Mod.Manifest>(
								entry.ReadBytes(),
								new JsonSerializerOptions()
								{
									Converters = { new VersionJson() },
								}
							);
							continue;
						}
						files.Add(new(entry.FullName, entry.ReadBytes()));
					}
				}

				if (manifest != null
					&& manifest.id != null
					&& Regex.IsMatch(manifest.id, @"^[a-zA-Z_]+$")
					&& manifest.version != null
					&& manifest.authors != null
					&& manifest.authors.Length != 0
				)
				{
					if (mods.ContainsKey(manifest.id))
					{
						Plugin.logger.LogError($"Mod {manifest.id} already exists");
						continue;
					}
					if (manifest.dependencies != null) dependencies.AddRange(manifest.dependencies);
					mods.Add(manifest.id, new(
						manifest.version,
						manifest.authors,
						Mod.Status.SUCCESS,
						files
					));
					Plugin.logger.LogInfo($"Registered mod {manifest.id}");
				}
				else
				{
					Plugin.logger.LogError("Error on registering mod");
				}
			}

			foreach (var dependency in dependencies)
			{
				if (!mods.ContainsKey(dependency.id))
				{
					string message = $"Dependency {dependency.id} not found";
					if (dependency.required)
					{
						Plugin.logger.LogFatal(message);
						Environment.Exit(-1);
					}
					Plugin.logger.LogWarning(message);
				}
				Version version = mods[dependency.id].version;
				if (
					(dependency.min != null && version < dependency.min) 
					||
					(dependency.max != null && version > dependency.max)
				)
				{
					string message = $"Need dependency {dependency.id} version {dependency.min} - {dependency.max} found {version}";
					if (dependency.required)
					{
						Plugin.logger.LogFatal(message);
						Environment.Exit(-1);
					}
					Plugin.logger.LogWarning(message);
				}
			}

			foreach (var (id, mod) in mods)
			{
				foreach (var file in mod.files)
				{
					if (Path.GetExtension(file.name) == ".dll")
					{
						try
						{
							Assembly assembly = Assembly.Load(file.bytes);
							foreach (Type type in assembly.GetTypes())
							{
								MethodInfo? method = type.GetMethod("Load");
								if (method != null)
								{
									method.Invoke(null, null);
									Plugin.logger.LogInfo($"Invoked Load method from {assembly.GetName().Name} assembly from {id} mod");
								}
							}
						}
						catch (TargetInvocationException exception)
						{
							if (exception.InnerException != null)
							{
								Plugin.logger.LogInfo($"Error on loading assembly from {id} mod: {exception.InnerException.Message}");
								mod.status = Mod.Status.ERROR;
							}
						}
					}
				}
			}

			stopwatch.Stop();
		}

		internal static void Load(JObject gameLogicdata)
		{
			stopwatch.Start();
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());

			foreach (var (id, mod) in mods)
			{
				foreach (var file in mod.files)
				{
					if (Path.GetFileName(file.name) == "patch.json")
					{
						try
						{
							GameLogicDataPatch(gameLogicdata, JObject.Parse(new StreamReader(new MemoryStream(file.bytes)).ReadToEnd()));
							Plugin.logger.LogInfo($"Registried patch from {id} mod");
						}
						catch (Exception e)
						{
							Plugin.logger.LogInfo($"Error on loading patch from {id} mod: {e.Message}");
							mod.status = Mod.Status.ERROR;
						}
					}
					if (Path.GetExtension(file.name) == ".png" && shouldInitializeSprites)
					{
						Vector2 pivot = Path.GetFileNameWithoutExtension(file.name).Split("_")[0] switch
						{
							"field" => new(0.5f, 0.0f),
							"mountain" => new(0.5f, -0.375f),
							_ => new(0.5f, 0.5f),
						};
						Sprite sprite = SpritesLoader.BuildSprite(file.bytes, pivot);
						GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(file.name), sprite);
						sprites.Add(Path.GetFileNameWithoutExtension(file.name), sprite);
					}
				}
			}

			gldDictionaryInversed = gldDictionary.ToDictionary((i) => i.Value, (i) => i.Key);
			shouldInitializeSprites = false;
			stopwatch.Stop();
			Plugin.logger.LogInfo($"Loaded all mods in {stopwatch.ElapsedMilliseconds}ms");
		}

		private static void GameLogicDataPatch(JObject gld, JObject patch)
		{
			foreach (JToken jtoken in patch.SelectTokens("$.localizationData.*").ToArray())
			{
				JObject token = jtoken.Cast<JObject>();
				string name = GetJTokenName(token).Replace('_', '.');
				if (name.StartsWith("tribeskins")) name = "TribeSkins/" + name;
				TermData term = LocalizationManager.Sources[0].AddTerm(name);

				List<string> strings = new();
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
					JArray skins = token["skins"].Cast<JArray>();
					Dictionary<string, int> skinsToReplace = new();

					foreach (var skin in skins._values)
					{
						string skinValue = skin.ToString();

						if (!Enum.TryParse<SkinType>(skinValue, out _))
						{
							EnumCache<SkinType>.AddMapping(skinValue, (SkinType)autoidx);
							skinsToReplace[skinValue] = autoidx;
							gldDictionary[skinValue] = autoidx;
							Plugin.logger.LogInfo("Created mapping for skinType with id " + skinValue + " and index " + autoidx);
							autoidx++;
						}
					}

					foreach (var entry in skinsToReplace)
					{
						if (skins._values.Contains(entry.Key))
						{
							skins._values.Remove(entry.Key);
							skins._values.Add(entry.Value);
						}
					}

					JToken originalSkins = gld.SelectToken(skins.Path, false);
					if (originalSkins != null)
					{
						skins.Merge(originalSkins);
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
					token["idx"] = autoidx;
					gldDictionary[id] = autoidx;
					switch (dataType)
					{
						case "tribeData":
							EnumCache<TribeData.Type>.AddMapping(id, (TribeData.Type)autoidx);
							climateToTribeData[climateAutoidx++] = autoidx;
							break;
						case "techData":
							EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)autoidx);
							break;
						case "unitData":
							EnumCache<UnitData.Type>.AddMapping(id, (UnitData.Type)autoidx);
							PrefabManager.units.TryAdd((int)(UnitData.Type)autoidx, PrefabManager.units[(int)UnitData.Type.Scout]);
							break;
						case "improvementData":
							EnumCache<ImprovementData.Type>.AddMapping(id, (ImprovementData.Type)autoidx);
							PrefabManager.improvements.TryAdd((ImprovementData.Type)autoidx, PrefabManager.improvements[ImprovementData.Type.CustomsHouse]);
							break;
						case "terrainData":
							EnumCache<Polytopia.Data.TerrainData.Type>.AddMapping(id, (Polytopia.Data.TerrainData.Type)autoidx);
							break;
						case "resourceData":
							EnumCache<ResourceData.Type>.AddMapping(id, (ResourceData.Type)autoidx);
							PrefabManager.resources.TryAdd((ResourceData.Type)autoidx, PrefabManager.resources[ResourceData.Type.Game]);
							break;
						case "taskData":
							EnumCache<TaskData.Type>.AddMapping(id, (TaskData.Type)autoidx);
							break;
					}
					Plugin.logger.LogInfo("Created mapping for " + dataType + " with id " + id + " and index " + autoidx);
					autoidx++;
				}
			}

			gld.Merge(patch, new() { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge });
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
