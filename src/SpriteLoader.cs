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
			string style = EnumCache<TribeData.Type>.GetName(__instance.tribeData.type);
			if (__instance.skinType != SkinType.Default)
			{
				style = EnumCache<SkinType>.GetName(__instance.skinType);
			}
			foreach (UITile tile in __instance.tiles)
			{
				if ((int)__instance.tribeData.type >= Plugin.AUTOIDX_STARTS_FROM)
				{
					if ((tile.Position.x == -1 && tile.Position.y == 3) || (tile.Position.x == 1 && tile.Position.y == 2))
					{
						tile.Forest.gameObject.SetActive(true);
						tile.Animal.gameObject.SetActive(true);
					}
					else if ((tile.Position.x == -1 && tile.Position.y == -1) || (tile.Position.x == 1 && tile.Position.y == 5) || (tile.Position.x == 0 && tile.Position.y == 2))
					{
						tile.Mountain.gameObject.SetActive(true);
					}
					else if ((tile.Position.x == 0 && tile.Position.y == -1) || (tile.Position.x == 1 && tile.Position.y == 0))
					{
						tile.Resource.gameObject.SetActive(true);
					}
				}
				string terrainType = tile.Tile.sprite.name;
				if (terrainType == "ground")
				{
					terrainType = "field";
				}
				Sprite? tileSprite = ModLoader.GetSprite(terrainType, style);
				if (tileSprite != null)
				{
					tile.Tile.sprite = tileSprite;
				}
				Sprite? forestSprite = ModLoader.GetSprite("forest", style);
				if (forestSprite != null)
				{
					tile.Forest.sprite = forestSprite;
				}
				Sprite? mountainSprite = ModLoader.GetSprite("mountain", style);
				if (mountainSprite != null)
				{
					tile.Mountain.sprite = mountainSprite;
					tile.Mountain.transform.localScale = new Vector3(1f, 0.7f, 0);
				}
				string resourceType = EnumCache<Polytopia.Data.ResourceData.Type>.GetName(ResourceData.Type.Fruit).ToLower();
				foreach (var enumValue in Enum.GetValues<ResourceData.Type>())
				{
					string resource = EnumCache<Polytopia.Data.ResourceData.Type>.GetName((ResourceData.Type)enumValue).ToLower();
					if (tile.Resource.sprite.name.Contains(resource))
					{
						resourceType = resource;
						break;
					}
				}
				Sprite? resourceSprite = ModLoader.GetSprite(resourceType, style);
				if (resourceSprite != null)
				{
					tile.Resource.sprite = resourceSprite;
					tile.Resource.transform.localScale = new Vector3(0.6f, 1.2f, 0);
				}
				Sprite? animalSprite = ModLoader.GetSprite("game", style);
				if (animalSprite != null)
				{
					tile.Animal.sprite = animalSprite;
					tile.Animal.transform.localScale = new Vector3(0.9f, 0.6f, 0);
				}
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
			Sprite? sprite;
			TribeData.Type tribeTypeFromStyle = GameManager.GameState.GameLogicData.GetTribeTypeFromStyle(climate);
			if (type == Polytopia.Data.TerrainData.Type.Mountain)
			{
				Sprite? fieldSprite;
				Sprite? mountainSprite;
				rectTransform = new GameObject
				{
					name = "UIMountainContainer"
				}.AddComponent<RectTransform>();

				sprite = ModLoader.GetSprite("field", EnumCache<SkinType>.GetName(skin));
				if (sprite == null)
				{
					sprite = ModLoader.GetSprite("field", EnumCache<TribeData.Type>.GetName(tribeTypeFromStyle));
					if (sprite == null)
					{
						return;
					}
				}
				fieldSprite = sprite;

				sprite = ModLoader.GetSprite("mountain", EnumCache<SkinType>.GetName(skin));
				if (sprite == null)
				{
					sprite = ModLoader.GetSprite(
						"mountain",
						EnumCache<TribeData.Type>.GetName(tribeTypeFromStyle)
					);
					if (sprite == null)
					{
						return;
					}
				}
				mountainSprite = sprite;
				Image fieldImage = UIUtils.GetImage();
				fieldImage.name = fieldSprite.name;
				fieldImage.sprite = fieldSprite;
				fieldImage.SetNativeSize();

				Image mountainImage = UIUtils.GetImage();
				mountainImage.name = mountainSprite.name;
				mountainImage.sprite = mountainSprite;
				mountainImage.SetNativeSize();
				fieldImage.SetNativeSize();
				mountainImage.SetNativeSize();
				fieldImage.raycastTarget = false;
				mountainImage.raycastTarget = false;
				RectTransform rectTransform2 = fieldImage.rectTransform;
				RectTransform rectTransform3 = mountainImage.rectTransform;
				rectTransform2.SetParent(rectTransform, false);
				rectTransform3.SetParent(rectTransform, false);
				rectTransform2.anchoredPosition = Vector2.zero;
				rectTransform3.anchoredPosition = new Vector2(0.19f, 15.52f);
			}
			else
			{
				Image image = UIUtils.GetImage();
				sprite = ModLoader.GetSprite(
					EnumCache<Polytopia.Data.TerrainData.Type>.GetName(type),
					EnumCache<TribeData.Type>.GetName(tribeTypeFromStyle)
				);
				if (sprite != null)
				{
					image.name = sprite.name;
					image.sprite = sprite;
					image.SetNativeSize();
					rectTransform = image.rectTransform;
				}
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