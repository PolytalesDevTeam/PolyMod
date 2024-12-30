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
		[HarmonyPatch(typeof(City), nameof(City.UpdateObject))]
		private static void City_UpdateObject(City __instance)
		{
			if (__instance.state.name != null)
			{
				if ((int)__instance.Owner.tribe > 17)
				{
					__instance.cityRenderer.Tribe = TribeData.Type.Imperius;
					__instance.cityRenderer.SkinType = SkinType.Default;
					__instance.cityRenderer.RefreshCity();
				}
			}
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
				if(skin != "default")
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
				if(sprite != null)
				{
					__instance.transform.FindChild("SpriteContainer/Head")
								.GetComponent<SpriteRenderer>().sprite = sprite;
				}
			}
			catch{}
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
			if(tile.data.terrain == Polytopia.Data.TerrainData.Type.Forest || tile.data.terrain == Polytopia.Data.TerrainData.Type.Mountain)
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
			Transform terrainTranform = __instance.transform.parent;
			if (terrainTranform != null)
			{
				Transform tileTransform = terrainTranform.parent;
				if (tileTransform != null)
				{
					Tile tile = tileTransform.GetComponent<Tile>();
					if (tile != null)
					{
						if(__instance.sprite.name.Contains("Forest") || __instance.sprite.name.Contains("Mountain") || __instance.sprite.name.Contains("forest") || __instance.sprite.name.Contains("mountain"))
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
				if(newSprite != null)
				{
					__result = newSprite;
				}
				return;
			}
			catch(Exception)
			{}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWorldPreview), nameof(UIWorldPreview.SetPreview), new Type[] { })]
		private static void UIWorldPreview_SetPreview(UIWorldPreview __instance) // TODO
		{
			//base.Show(origin);
			foreach (var image in GameObject.FindObjectsOfType<UnityEngine.UI.Image>())
			{
				if (image.name == "Head")
				{
					image.Cast<UnityEngine.UI.Image>();
					//string idKey = "druid_worldpreview";
					//string spritesKey = "head_" + idKey + "_";
					//image.sprite = sprites[spritesKey];
					//image.m_Sprite = sprites[spritesKey];
					//image.overrideSprite = sprites[spritesKey];
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
			for (int key = 0; key < __instance.quickActions.buttons.Count; ++key)
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
					catch{}
				}
			}
			// if (tile.Data.resource != null && tile.Data.improvement == null)
			// {
				// using (List<ImprovementData>.Enumerator enumerator2 = __instance.GetSuggestedUnlockableImprovements(tile.Data.resource.type, player).GetEnumerator())
				// {
				// 	while (enumerator2.MoveNext())
				// 	{
				// 		ImprovementData improvementData = enumerator2.Current;
				// 		UIRoundButton uiroundButton2 = __instance.CreateRoundBottomBarButton(Localization.GetSkinned(player.skinType, improvementData.displayName, Array.Empty<object>()), false);
				// 		TribeData.Type tribeTypeFromStyle2 = GameManager.GameState.GameLogicData.GetTribeTypeFromStyle(tile.Climate);
				// 		uiroundButton2.iconSpriteHandle.Request(SpriteData.GetBuildingSpriteAddresses(improvementData.type, tile.GetVisualSkinTypeForTile(), tribeTypeFromStyle2));
				// 		uiroundButton2.buttonActive = false;
				// 		uiroundButton2.OnClicked += (UIButtonBase.ButtonAction)delegate(int index, BaseEventData eventData)
				// 		{
				// 			PopupManager.HideCurrentPopup();
				// 			__instance.OnUnlockableClicked(improvementData, tile, player);
				// 		};
				// 	}
				// }
			// }
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIIconData), nameof(UIIconData.GetImage))]
		private static void UIIconData_GetImage(ref Image __result, UIIconData __instance, string id) // TODO
		{
			//Console.Write(id);
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
				if(skin != "default")
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

				if(sprite != null)
				{
					headImage.sprite = sprite;
				}
				headImage.gameObject.transform.localScale = new Vector3(1.5f, 1.5f, 0);
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

				if(sprite == null)
				{
					sprite = ModLoader.GetSprite("field", EnumCache<Polytopia.Data.TribeData.Type>
						.GetName(tribeTypeFromStyle).ToLower());
					if(sprite == null)
					{
						return;
					}
				}
				fieldSprite = sprite;

				sprite = ModLoader.GetSprite("mountain", EnumCache<Polytopia.Data.SkinType>
					.GetName(skin).ToLower());

				if(sprite == null)
				{
					sprite = ModLoader.GetSprite("mountain", EnumCache<Polytopia.Data.TribeData.Type>
						.GetName(tribeTypeFromStyle).ToLower());
					if(sprite == null)
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
				if(sprite != null)
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
		private static void GetImprovementSprite(ref Sprite __result, ImprovementData.Type improvement, TribeData.Type tribe, SkinType skin, SpriteAtlasManager atlasManager)
		{
			Console.Write(EnumCache<Polytopia.Data.ImprovementData.Type>.GetName(improvement).ToLower());
			if(__result == null || __result.name == "placeholder")
			{
				string style;
				if(skin != SkinType.Default)
				{
					style = EnumCache<Polytopia.Data.SkinType>.GetName(skin).ToLower();
				}
				else
				{
					style = EnumCache<Polytopia.Data.TribeData.Type>.GetName(tribe).ToLower();
				}
				Sprite? sprite = ModLoader.GetSprite(EnumCache<Polytopia.Data.ImprovementData.Type>.GetName(improvement).ToLower(), style);
				if(sprite != null)
				{
					__result = sprite;
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
				if(newSprite != null)
				{
					sprite = newSprite;
				}
			}
			catch
			{}
			return sprite;
		}

		public static Sprite BuildSprite(byte[] data, Vector2 pivot)
		{
			Texture2D texture = new(1, 1);
			texture.filterMode = FilterMode.Trilinear;
			texture.LoadImage(data);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), pivot, 2112);
		}

		public static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(SpritesLoader));
		}
	}
}