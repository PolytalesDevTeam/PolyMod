using HarmonyLib;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.U2D;

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
			string tribe;
			try
			{
				if(ModLoader.gldDictionaryInversed.ContainsKey((int)__instance.Owner.skinType))
				{
					tribe = ModLoader.gldDictionaryInversed[(int)__instance.Owner.skinType];
				}
				else if(ModLoader.gldDictionaryInversed.ContainsKey((int)__instance.Owner.tribe))
				{
					tribe = ModLoader.gldDictionaryInversed[(int)__instance.Owner.tribe];
				}
				else //TODO: add a check if there is a sprite for a skin
				{
					tribe = __instance.Owner.tribe.ToString();
				}
				Sprite? sprite = ModLoader.GetSprite("head", tribe);
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
			Sprite? sprite = GetSpriteForTerrainOrResource(__instance.tile.data, EnumCache<ResourceData.Type>.GetName(__instance.data.type).ToLower());
			if(sprite != null)
			{
				__instance.Sprite = sprite;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TerrainRenderer), nameof(TerrainRenderer.UpdateGraphics))]
		private static void TerrainRenderer_UpdateGraphics(TerrainRenderer __instance, Tile tile)
		{
			string? terrainName = null;
			if(tile.data.terrain == Polytopia.Data.TerrainData.Type.Forest || tile.data.terrain == Polytopia.Data.TerrainData.Type.Mountain)
			{
				terrainName = "field";
			}
			Sprite? sprite = GetSpriteForTerrainOrResource(tile.data, terrainName);
			if(sprite != null)
			{
				__instance.spriteRenderer.Sprite  = sprite;
			}
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
							Sprite? sprite = GetSpriteForTerrainOrResource(tile.data);
							if(sprite != null)
							{
								__instance.Sprite = sprite;
							}
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
		private static void UIWorldPreview_SetPreview(UIWorldPreview __instance) //bad idea to do it here, i will find better place later.
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
					string name;
					try
					{
						if(ModLoader.gldDictionaryInversed.ContainsKey((int)improvementData2.type))
						{
							name = ModLoader.gldDictionaryInversed[(int)improvementData2.type];
						}
						else
						{
							name = improvementData2.type.ToString();
						}
						uiroundButton.SetSprite(ModLoader.GetSprite(name, player.tribe.ToString()));
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

		public static Sprite? GetSpriteForTerrainOrResource(TileData tileData, string? name = null)
		{
			string tribe;
			if(name == null)
			{
				name = EnumCache<Polytopia.Data.TerrainData.Type>.GetName(tileData.terrain).ToLower();
			}
			try
			{
				if(ModLoader.gldDictionaryInversed.ContainsKey(ModLoader.climateToTribeData[tileData.climate]))
				{
					tribe = ModLoader.gldDictionaryInversed[ModLoader.climateToTribeData[tileData.climate]];
				}
				else
				{
					tribe = ((TribeData.Type)tileData.climate).ToString();
				}
				Sprite? sprite = ModLoader.GetSprite(
					name,
					tribe
				);
				return sprite;
			}
			catch{}
			return null;
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