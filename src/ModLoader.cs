using Cpp2IL.Core.Extensions;
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
		private static Dictionary<int, string> _styles = new();

		internal static void Init(JObject gld)
		{
			Directory.CreateDirectory(Plugin.MODS_PATH);
			GameManager.GetSpriteAtlasManager().cachedSprites.TryAdd("Heads", new());

			foreach (string modname in Directory.GetFiles(Plugin.MODS_PATH, "*.polymod"))
			{
				ZipArchive mod = new(File.OpenRead(modname));

				foreach (var entry in mod.Entries)
				{
					string name = entry.ToString();

					if (Path.GetExtension(name) == ".png")
					{
						GameManager.GetSpriteAtlasManager().cachedSprites["Heads"].Add(Path.GetFileNameWithoutExtension(name), BuildSprite(entry.ReadBytes()));
					}
				}

				ZipArchiveEntry? patch = mod.GetEntry("patch.json");
				if (patch != null)
				{
					Patch(gld, JObject.Parse(new StreamReader(patch.Open()).ReadToEnd()));
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

            Minerskagg.AddMappings();

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
							PrefabManager.units.TryAdd((UnitData.Type)idx, PrefabManager.units[UnitData.Type.Scout]);
							break;
						case "techData":
							EnumCache<TechData.Type>.AddMapping(id, (TechData.Type)idx);
							break;
					}
				}
			}

			gld.Merge(patch, Plugin.GLD_MERGE_SETTINGS);
		}

		internal static SpriteAddress GetSprite(SpriteAddress sprite, string name, string style = "", int level = 0)
		{
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

		private static Sprite BuildSprite(byte[] data)
		{
			Texture2D texture = new(1, 1);
			texture.LoadImage(data);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), 2112);
		}
	}
}
