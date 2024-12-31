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
		[HarmonyPatch(typeof(TechItem), nameof(TechItem.SetupComplete))] // Crash preventive patch
		private static void TechItem_SetupComplete()
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Unit), nameof(Unit.SetVisible))]
		private static void Unit_SetVisible(Unit __instance)
		{
			try
			{
				string name;
				string skin = EnumCache<Polytopia.Data.SkinType>
					.GetName(__instance.Owner.skinType)
					.ToLower();
				if (skin != "default")
				{
					name = skin;
				}
				else
				{
					name = EnumCache<Polytopia.Data.TribeData.Type>
					.GetName(__instance.Owner.tribe)
					.ToLower();
				}
				Sprite? sprite = ModLoader.GetSprite("head", name);
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
			__instance.Sprite = GetSpriteForTile(__instance.Sprite, __instance.tile, EnumCache<Polytopia.Data.ResourceData.Type>.GetName(__instance.tile.data.resource.type).ToLower());
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Building), nameof(Building.SetVisible))]
		private static void Building_SetVisible(Building __instance, bool value)
		{
			__instance.Sprite = GetSpriteForTile(__instance.Sprite, __instance.tile, EnumCache<Polytopia.Data.ImprovementData.Type>.GetName(__instance.tile.improvement.data.type).ToLower(), __instance.tile.improvement.Level);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TerrainRenderer), nameof(TerrainRenderer.UpdateGraphics))]
		private static void TerrainRenderer_UpdateGraphics(TerrainRenderer __instance, Tile tile)
		{
			string? name;
			if (tile.data.terrain == Polytopia.Data.TerrainData.Type.Forest || tile.data.terrain == Polytopia.Data.TerrainData.Type.Mountain)
			{
				name = "field";
			}
			else
			{
				name = EnumCache<Polytopia.Data.TerrainData.Type>.GetName(tile.data.terrain).ToLower();
			}
			__instance.spriteRenderer.Sprite = GetSpriteForTile(__instance.spriteRenderer.Sprite, tile, name);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PolytopiaSpriteRenderer), nameof(PolytopiaSpriteRenderer.ForceUpdateMesh))]
		private static void PolytopiaSpriteRenderer_ForceUpdateMesh(PolytopiaSpriteRenderer __instance)
		{
			if (__instance.gameObject.name.Contains("Forest") || __instance.gameObject.name.Contains("Mountain") || __instance.gameObject.name.Contains("forest") || __instance.gameObject.name.Contains("mountain"))
			{
				Transform terrainTranform = __instance.transform.parent;
				if (terrainTranform != null)
				{
					Transform tileTransform = terrainTranform.parent;
					if (tileTransform != null)
					{
						Tile tile = tileTransform.GetComponent<Tile>();
						if (tile != null)
						{
							__instance.Sprite = GetSpriteForTile(__instance.Sprite, tile, EnumCache<Polytopia.Data.TerrainData.Type>.GetName(tile.data.terrain).ToLower());
						}
					}
				}
			}

			if (__instance.atlasName != null)
			{
				if (string.IsNullOrEmpty(__instance.atlasName))
				{
					MaterialPropertyBlock materialPropertyBlock;
					materialPropertyBlock = new MaterialPropertyBlock();
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
			catch (Exception)
			{ }
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWorldPreview), nameof(UIWorldPreview.SetPreview), new Type[] { })]
		private static void UIWorldPreview_SetPreview(UIWorldPreview __instance) // TODO
		{
			foreach (var image in GameObject.FindObjectsOfType<UnityEngine.UI.Image>())
			{
				if (image.name == "Head")
				{
					image.Cast<UnityEngine.UI.Image>();
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
			Il2CppSystem.Collections.Generic.List<CommandBase> buildableImprovementsCommands = CommandUtils.GetBuildableImprovements(gameState, player, tile.Data, true);
			for (int key = 0; key < buildableImprovementsCommands.Count; ++key)
			{
				UIRoundButton uiroundButton = __instance.quickActions.buttons[key];
				BuildCommand buildCommand = buildableImprovementsCommands[key].Cast<BuildCommand>();
				ImprovementData improvementData2;
				gameState.GameLogicData.TryGetData(buildCommand.Type, out improvementData2);
				UnitData.Type type = improvementData2.CreatesUnit();
				if (type == UnitData.Type.None && uiroundButton.icon.sprite.name == "placeholder")
				{
					try
					{
						string improvementType = EnumCache<Polytopia.Data.ImprovementData.Type>.GetName(improvementData2.type).ToLower();
						string tribeType = EnumCache<Polytopia.Data.TribeData.Type>.GetName(player.tribe).ToLower();
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
			Transform headTransform = __instance.transform.Find("Head");

			if (headTransform != null)
			{
				GameObject childGameObject = headTransform.gameObject;
				Image headImage = childGameObject.GetComponent<Image>();

				string name;
				string skin = EnumCache<Polytopia.Data.SkinType>
					.GetName(__instance.skin)
					.ToLower();
				if (skin != "default")
				{
					name = skin;
				}
				else
				{
					name = EnumCache<Polytopia.Data.TribeData.Type>
					.GetName(__instance.tribe)
					.ToLower();
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

				sprite = ModLoader.GetSprite("field", EnumCache<Polytopia.Data.SkinType>
					.GetName(skin).ToLower());

				if (sprite == null)
				{
					sprite = ModLoader.GetSprite("field", EnumCache<Polytopia.Data.TribeData.Type>
						.GetName(tribeTypeFromStyle).ToLower());
					if (sprite == null)
					{
						return;
					}
				}
				fieldSprite = sprite;

				sprite = ModLoader.GetSprite("mountain", EnumCache<Polytopia.Data.SkinType>
					.GetName(skin).ToLower());

				if (sprite == null)
				{
					sprite = ModLoader.GetSprite("mountain", EnumCache<Polytopia.Data.TribeData.Type>
						.GetName(tribeTypeFromStyle).ToLower());
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
				sprite = ModLoader.GetSprite(EnumCache<Polytopia.Data.TerrainData.Type>.GetName(type).ToLower(), EnumCache<Polytopia.Data.TribeData.Type>
						.GetName(tribeTypeFromStyle).ToLower());
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
		[HarmonyPatch(typeof(UIUtils), nameof(UIUtils.GetImprovementSprite), new Type[] { typeof(ImprovementData.Type), typeof(TribeData.Type), typeof(SkinType), typeof(SpriteAtlasManager)})]
		private static void UIUtils_GetImprovementSprite(ref Sprite __result, ImprovementData.Type improvement, TribeData.Type tribe, SkinType skin, SpriteAtlasManager atlasManager)
		{
			string style;
			if (skin != SkinType.Default)
			{
				style = EnumCache<Polytopia.Data.SkinType>.GetName(skin).ToLower();
			}
			else
			{
				style = EnumCache<Polytopia.Data.TribeData.Type>.GetName(tribe).ToLower();
			}
			Sprite? sprite = ModLoader.GetSprite(EnumCache<Polytopia.Data.ImprovementData.Type>.GetName(improvement).ToLower(), style);
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
				string style;
				if (skinType != SkinType.Default)
				{
					style = EnumCache<Polytopia.Data.SkinType>.GetName(skinType).ToLower();
				}
				else
				{
					style = EnumCache<Polytopia.Data.TribeData.Type>.GetName(tribe).ToLower();
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
						int.TryParse(tokens[1], out level);
					}

					string style;
					if (skin != SkinType.Default)
					{
						style = EnumCache<Polytopia.Data.SkinType>.GetName(skin).ToLower();
					}
					else
					{
						style = EnumCache<Polytopia.Data.TribeData.Type>.GetName(tribe).ToLower();
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
				string tribe = EnumCache<Polytopia.Data.TribeData.Type>
					.GetName(GameManager.GameState.GameLogicData.GetTribeTypeFromStyle(tile.data.climate))
					.ToLower();

				Sprite? newSprite = ModLoader.GetSprite(name, tribe, level);
				if (newSprite != null)
				{
					sprite = newSprite;
				}
			}
			catch
			{ }
			return sprite;
		}

		public static Sprite BuildSprite(byte[] data, Vector2 pivot)
		{
			Texture2D texture = new(1, 1);
			texture.filterMode = FilterMode.Trilinear;
			texture.LoadImage(data);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), pivot, 2112);
		}

		internal static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(SpritesLoader));
		}
	}
}