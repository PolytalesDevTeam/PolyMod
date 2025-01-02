using System.Globalization;
using HarmonyLib;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace PolyMod
{
	public class SpritesLoader
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.SetupComplete))]
		private static void TechItem_SetupComplete()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Unit), nameof(Unit.SetVisible))]
		private static void Unit_SetVisible(Unit __instance)
		{
			try
			{
				string style = EnumCache<TribeData.Type>.GetName(__instance.Owner.tribe);
				if (__instance.Owner.skinType != SkinType.Default)
				{
					style = EnumCache<SkinType>.GetName(__instance.Owner.skinType);
				}
				Sprite? sprite = ModLoader.GetSprite("head", style);
				if (sprite != null)
				{
					__instance.transform.FindChild("SpriteContainer/Head")
								.GetComponent<SpriteRenderer>().sprite = sprite;
				}
			}
			catch { }
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Resource), nameof(Resource.SetVisible))]
		private static void Resource_SetVisible(Resource __instance)
		{
			__instance.Sprite = GetSpriteForTile(
				__instance.Sprite,
				__instance.tile,
				EnumCache<ResourceData.Type>.GetName(__instance.tile.data.resource.type)
			);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Building), nameof(Building.SetVisible))]
		private static void Building_SetVisible(Building __instance)
		{
			__instance.Sprite = GetSpriteForTile(
				__instance.Sprite,
				__instance.tile,
				EnumCache<ImprovementData.Type>.GetName(__instance.tile.improvement.data.type),
				__instance.tile.improvement.Level
			);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TerrainRenderer), nameof(TerrainRenderer.UpdateGraphics))]
		private static void TerrainRenderer_UpdateGraphics(TerrainRenderer __instance, Tile tile)
		{
			string? name = EnumCache<Polytopia.Data.TerrainData.Type>.GetName(tile.data.terrain);
			if (tile.data.terrain == Polytopia.Data.TerrainData.Type.Forest
				|| tile.data.terrain == Polytopia.Data.TerrainData.Type.Mountain
			)
			{
				name = "field";
			}
			__instance.spriteRenderer.Sprite = GetSpriteForTile(__instance.spriteRenderer.Sprite, tile, name);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PolytopiaSpriteRenderer), nameof(PolytopiaSpriteRenderer.ForceUpdateMesh))]
		private static void PolytopiaSpriteRenderer_ForceUpdateMesh(PolytopiaSpriteRenderer __instance)
		{
			string name = __instance.gameObject.name.ToLower();
			if (name.Contains("forest") || name.Contains("mountain"))
			{
				Transform? terrainTranform = __instance.transform.parent;
				if (terrainTranform != null)
				{
					Transform? tileTransform = terrainTranform.parent;
					if (tileTransform != null)
					{
						Tile? tile = tileTransform.GetComponent<Tile>();
						if (tile != null)
						{
							__instance.Sprite = GetSpriteForTile(
								__instance.Sprite,
								tile,
								EnumCache<Polytopia.Data.TerrainData.Type>.GetName(tile.data.terrain)
							);
						}
					}
				}
			}

			if (__instance.atlasName != null)
			{
				if (string.IsNullOrEmpty(__instance.atlasName))
				{
					MaterialPropertyBlock materialPropertyBlock = new();
					materialPropertyBlock.SetVector("_Flip", new Vector4(1f, 1f, 0f, 0f));
					materialPropertyBlock.SetTexture("_MainTex", __instance.sprite.texture);
					__instance.meshRenderer.SetPropertyBlock(materialPropertyBlock);
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteAtlasManager), nameof(SpriteAtlasManager.GetSpriteFromAtlas), new Type[] { typeof(SpriteAtlas), typeof(string) })]
		private static void SpriteAtlasManager_GetSpriteFromAtlas(ref Sprite __result, SpriteAtlas spriteAtlas, string sprite)
		{
			try
			{
				string[] names = sprite.Split('_');
				Sprite? newSprite = ModLoader.GetSprite(names[0], names[1]);
				if (newSprite != null)
				{
					__result = newSprite;
				}
				return;
			}
			catch { }
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWorldPreview), nameof(UIWorldPreview.SetPreview), new Type[] { })]
		private static void UIWorldPreview_SetPreview(UIWorldPreview __instance) // TODO
		{
			if(Plugin.config.debug)
			{
				RectMask2D mask = __instance.gameObject.GetComponent<UnityEngine.UI.RectMask2D>();
				UnityEngine.GameObject.Destroy(mask);
				__instance.gameObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
				__instance.gameObject.transform.position -= new Vector3(-5f, 35f, 0f);
			}

			string style = EnumCache<TribeData.Type>.GetName(__instance.tribeData.type);
			if (__instance.skinType != SkinType.Default)
			{
				style = EnumCache<SkinType>.GetName(__instance.skinType);
			}

			List<PolyMod.ModLoader.PreviewTile>? preview = null;
			if(ModLoader.tribePreviews.ContainsKey(style))
			{
				preview = ModLoader.tribePreviews[style];
			}
			List<UITile> tiles = __instance.tiles.ToArray().ToList();
			List<UITile> sortedTiles = tiles
				.OrderBy(tile => tile.Position.y)
				.ThenBy(tile => tile.Position.x)
				.ToList();

			int i = 0;
			foreach (UITile tile in sortedTiles)
			{
				if(Plugin.config.debug)
				{
					tile.DebugText.text = i.ToString();
					tile.DebugText.gameObject.SetActive(true);
				}
				if(preview != null && preview.Count - 1 >= i)
				{
					ModLoader.PreviewTile? previewTile = preview.FirstOrDefault(tile => tile.idx == i); //TODO: FIX IDX
					if(previewTile != null)
					{
						tile.Tile.gameObject.SetActive(false);
						tile.Mountain.gameObject.SetActive(false);
						tile.Forest.gameObject.SetActive(false);
						tile.Resource.gameObject.SetActive(false);
						tile.Animal.gameObject.SetActive(false);
						tile.Improvement.gameObject.SetActive(false);


						SkinVisualsTransientData data = new SkinVisualsTransientData();
						data.tileClimateSettings = new TribeAndSkin(__instance.tribeData.type, __instance.skinType);
						UIUtils.SkinnedTerrainSprites skinnedTerrainSprites = UIUtils.GetTerrainSprite(data, previewTile.terrainType, GameManager.GetSpriteAtlasManager());
						if(skinnedTerrainSprites.groundTerrain != null)
						{
							tile.Tile.sprite = skinnedTerrainSprites.groundTerrain;
						}
						if(skinnedTerrainSprites.forestOrMountainTerrain != null)
						{
							tile.Mountain.sprite = skinnedTerrainSprites.forestOrMountainTerrain;
							tile.Forest.sprite = skinnedTerrainSprites.forestOrMountainTerrain;
						}
						switch(previewTile.terrainType)
						{
							case Polytopia.Data.TerrainData.Type.None:
								break;
							case Polytopia.Data.TerrainData.Type.Mountain:
								tile.Tile.gameObject.SetActive(true);
								tile.Mountain.gameObject.SetActive(true);
								break;
							case Polytopia.Data.TerrainData.Type.Forest:
								tile.Tile.gameObject.SetActive(true);
								tile.Forest.gameObject.SetActive(true);
								break;
							default:
								tile.Tile.gameObject.SetActive(true);
								break;
						}
						Sprite? resourceSprite = UIUtils.GetResourceSprite(data, previewTile.resourceType, GameManager.GetSpriteAtlasManager());
						if(resourceSprite != null)
						{
							tile.Animal.sprite = resourceSprite;
							tile.Resource.sprite = resourceSprite;
						}
						switch(previewTile.resourceType)
						{
							case Polytopia.Data.ResourceData.Type.Game:
								tile.Animal.gameObject.SetActive(true);
								break;
							default:
								tile.Resource.gameObject.SetActive(true);
								break;
						}
						if(previewTile.improvementType != Polytopia.Data.ImprovementData.Type.None)
						{
							tile.Improvement.sprite = UIUtils.GetImprovementSprite(previewTile.improvementType, __instance.tribeData.type, __instance.skinType, GameManager.GetSpriteAtlasManager()); // TODO: FIX
							tile.Improvement.gameObject.SetActive(true);
						}
					}
				}
				i++;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.AddImprovementButtons))]
		private static void InteractionBar_AddImprovementButtons(InteractionBar __instance, Tile tile)
		{
			PlayerState player = GameManager.LocalPlayer;
			if (player.AutoPlay)
			{
				return;
			}
			GameState gameState = GameManager.GameState;
			Il2CppSystem.Collections.Generic.List<CommandBase> buildableImprovementsCommands
				= CommandUtils.GetBuildableImprovements(gameState, player, tile.Data, true);
			for (int key = 0; key < buildableImprovementsCommands.Count; ++key)
			{
				UIRoundButton uiroundButton = __instance.quickActions.buttons[key];
				BuildCommand buildCommand = buildableImprovementsCommands[key].Cast<BuildCommand>();
				gameState.GameLogicData.TryGetData(buildCommand.Type, out ImprovementData improvementData2);
				UnitData.Type type = improvementData2.CreatesUnit();
				if (type == UnitData.Type.None && uiroundButton.icon.sprite.name == "placeholder")
				{
					try
					{
						string improvementType = EnumCache<ImprovementData.Type>.GetName(improvementData2.type);
						string tribeType = EnumCache<TribeData.Type>.GetName(player.tribe);
						uiroundButton.SetSprite(ModLoader.GetSprite(improvementType, tribeType));
					}
					catch { }
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIIconData), nameof(UIIconData.GetImage))]
		private static void UIIconData_GetImage(ref Image __result, UIIconData __instance, string id) // TODO
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIUnitRenderer), nameof(UIUnitRenderer.CreateUnit))]
		private static void UIUnitRenderer_CreateUnit(UIUnitRenderer __instance)
		{
			Transform? headTransform = __instance.transform.Find("Head");

			if (headTransform != null)
			{
				GameObject childGameObject = headTransform.gameObject;
				Image headImage = childGameObject.GetComponent<Image>();

				string name = EnumCache<TribeData.Type>.GetName(__instance.tribe);
				if (__instance.skin != SkinType.Default)
				{
					name = EnumCache<SkinType>.GetName(__instance.skin);
				}
				Sprite? sprite = ModLoader.GetSprite("head", name);

				if (sprite != null)
				{
					headImage.sprite = sprite;
					headImage.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 0);
				}
			}
		}


		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIUtils), nameof(UIUtils.GetTile))]
		private static void UIUtils_GetTile(ref RectTransform __result, Polytopia.Data.TerrainData.Type type, int climate, SkinType skin)
		{
			RectTransform rectTransform = __result;
			TribeData.Type tribeTypeFromStyle = GameManager.GameState.GameLogicData.GetTribeTypeFromStyle(climate);
			SkinVisualsTransientData data = new SkinVisualsTransientData();
			data.tileClimateSettings = new TribeAndSkin(tribeTypeFromStyle, skin);
			UIUtils.SkinnedTerrainSprites skinnedTerrainSprites = UIUtils.GetTerrainSprite(data, type, GameManager.GetSpriteAtlasManager());

			int count = 0;
			foreach (Il2CppSystem.Object child in rectTransform)
			{
				Transform childTransform = child.Cast<Transform>();
				Image image = childTransform.GetComponent<Image>();
				Sprite? sprite = count == 0 ? skinnedTerrainSprites.groundTerrain : skinnedTerrainSprites.forestOrMountainTerrain;
				image.name = sprite.name;
				image.sprite = sprite;
				image.SetNativeSize();
				count++;
			}
			__result = rectTransform;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIUtils), nameof(UIUtils.GetImprovementSprite), new Type[] { typeof(ImprovementData.Type), typeof(TribeData.Type), typeof(SkinType), typeof(SpriteAtlasManager) })]
		private static void UIUtils_GetImprovementSprite(ref Sprite __result, ImprovementData.Type improvement, TribeData.Type tribe, SkinType skin, SpriteAtlasManager atlasManager)
		{
			string style = EnumCache<TribeData.Type>.GetName(tribe);
			if (skin != SkinType.Default)
			{
				style = EnumCache<SkinType>.GetName(skin);
			}
			Sprite? sprite = ModLoader.GetSprite(EnumCache<ImprovementData.Type>.GetName(improvement), style);
			if (sprite != null)
			{
				__result = sprite;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIUtils), nameof(UIUtils.GetResourceSprite))]
		private static void UIUtils_GetResourceSprite(ref Sprite __result, SkinVisualsTransientData data, ResourceData.Type resource, SpriteAtlasManager atlasManager)
		{
			TribeData.Type tribe = data.tileClimateSettings.tribe;
			SkinType skin = data.tileClimateSettings.skin;
			string style = EnumCache<TribeData.Type>.GetName(tribe);
			if (skin != SkinType.Default)
			{
				style = EnumCache<SkinType>.GetName(skin);
			}
			Sprite? sprite = ModLoader.GetSprite(EnumCache<ResourceData.Type>.GetName(resource), style);
			if (sprite != null)
			{
				__result = sprite;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIUtils), nameof(UIUtils.GetTerrainSprite))]
		private static void UIUtils_GetTerrainSprite(ref UIUtils.SkinnedTerrainSprites __result, SkinVisualsTransientData data, Polytopia.Data.TerrainData.Type terrain, SpriteAtlasManager atlasManager)
		{
			TribeData.Type tribe = data.tileClimateSettings.tribe;
			SkinType skin = data.tileClimateSettings.skin;
			string style = EnumCache<TribeData.Type>.GetName(tribe);
			Sprite? sprite;
			Sprite? groundSprite = __result.groundTerrain;
			Sprite? forestOrMountainSprite = __result.forestOrMountainTerrain;
			if (skin != SkinType.Default)
			{
				style = EnumCache<SkinType>.GetName(skin);
			}
			if(terrain == Polytopia.Data.TerrainData.Type.Mountain || terrain == Polytopia.Data.TerrainData.Type.Forest)
			{
				sprite = ModLoader.GetSprite("field", style);
				if (sprite != null)
				{
					groundSprite = sprite;
				}
				sprite = ModLoader.GetSprite(EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), style);
				if (sprite != null)
				{
					forestOrMountainSprite = sprite;
				}
			}
			else
			{
				sprite = ModLoader.GetSprite(EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), style);
				if (sprite != null)
				{
					groundSprite = sprite;
				}
			}
			__result.groundTerrain = groundSprite;
			__result.forestOrMountainTerrain = forestOrMountainSprite;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CityRenderer), nameof(CityRenderer.GetHouse))]
		private static void CityRenderer_GetHouse(ref PolytopiaSpriteRenderer __result, CityRenderer __instance, TribeData.Type tribe, int type, SkinType skinType)
		{
			PolytopiaSpriteRenderer polytopiaSpriteRenderer = __result;

			if (type != __instance.HOUSE_WORKSHOP && type != __instance.HOUSE_PARK)
			{
				string style = EnumCache<TribeData.Type>.GetName(tribe);
				if (skinType != SkinType.Default)
				{
					style = EnumCache<SkinType>.GetName(skinType);
				}
				Sprite? sprite = ModLoader.GetSprite("house", style, type);
				if (sprite != null)
				{
					polytopiaSpriteRenderer.Sprite = sprite;
					TerrainMaterialHelper.SetSpriteSaturated(polytopiaSpriteRenderer, __instance.IsEnemyCity);
					__result = polytopiaSpriteRenderer;
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UICityRenderer), nameof(UICityRenderer.GetResource))]
		private static void UICityRenderer_GetResource(ref GameObject __result, string baseName, Polytopia.Data.TribeData.Type tribe, Polytopia.Data.SkinType skin)
		{
			GameObject resourceObject = __result;
			Image imageComponent = resourceObject.GetComponent<Image>();
			string[] tokens = baseName.Split('_');
			if (tokens.Length > 0)
			{
				if (tokens[0] == "House")
				{
					int level = 0;
					if (tokens.Length > 1)
					{
						_ = int.TryParse(tokens[1], out level);
					}

					string style;
					if (skin != SkinType.Default)
					{
						style = EnumCache<SkinType>.GetName(skin);
					}
					else
					{
						style = EnumCache<TribeData.Type>.GetName(tribe);
					}

					Sprite? sprite = ModLoader.GetSprite("house", style, level);
					if (sprite == null)
					{
						return;
					}
					imageComponent.sprite = sprite;
					imageComponent.SetNativeSize();

					__result = resourceObject;
				}
			}
		}

		private static Sprite GetSpriteForTile(Sprite sprite, Tile tile, string name, int level = 0)
		{
			try
			{
				string tribe = EnumCache<TribeData.Type>
					.GetName(GameManager.GameState.GameLogicData.GetTribeTypeFromStyle(tile.data.climate));

				Sprite? newSprite = ModLoader.GetSprite(name, tribe, level);
				if (newSprite != null)
				{
					sprite = newSprite;
				}
			}
			catch { }
			return sprite;
		}

		public static Sprite BuildSprite(byte[] data, Vector2? pivot = null, float pixelsPerUnit = 256f)
		{
			Texture2D texture = new(1, 1);
			texture.filterMode = FilterMode.Trilinear;
			texture.LoadImage(data);
			return Sprite.Create(
				texture, 
				new(0, 0, texture.width, texture.height), 
				pivot ?? new(0.5f, 0.5f), 
				pixelsPerUnit
			);
		}

		internal static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(SpritesLoader));
		}
	}
}