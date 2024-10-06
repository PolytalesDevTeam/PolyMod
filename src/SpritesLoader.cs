using HarmonyLib;
using Polytopia.Data;
using UnityEngine;

namespace PolyMod
{
    public class SpritesLoader
    {
		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetTileSpriteAddress), new Type[] { typeof(Polytopia.Data.TerrainData.Type), typeof(string) })]
		private static void SpriteData_GetTileSpriteAddress(ref SpriteAddress __result, Polytopia.Data.TerrainData.Type terrain, string skinId)
		{
			__result = GetSprite(__result, EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetResourceSpriteAddress), new Type[] { typeof(ResourceData.Type), typeof(string) })]
		private static void SpriteData_GetResourceSpriteAddress(ref SpriteAddress __result, ResourceData.Type type, string skinOrTribeAsString)
		{
			__result = GetSprite(__result, EnumCache<ResourceData.Type>.GetName(type), skinOrTribeAsString);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetBuildingSpriteAddress), new Type[] { typeof(ImprovementData.Type), typeof(string) })]
		private static void SpriteData_GetBuildingSpriteAddress(ref SpriteAddress __result, ImprovementData.Type type, string climateOrSkinAsString)
		{
			__result = GetSprite(__result, EnumCache<ImprovementData.Type>.GetName(type), climateOrSkinAsString);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetUnitIconAddress))]
		private static void SpriteData_GetUnitIconAddress(ref SpriteAddress __result, UnitData.Type type)
		{
			__result = GetSprite(__result, "icon", EnumCache<UnitData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(SkinType) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, SkinType skin)
		{
			__result = GetSprite(__result, "head", EnumCache<SkinType>.GetName(skin));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(TribeData.Type) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, TribeData.Type type)
		{
			__result = GetSprite(__result, "head", EnumCache<TribeData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(SpriteData.SpecialFaceIcon) })]
		private static void SpriteData_GetHeadSpriteAddress(ref SpriteAddress __result, SpriteData.SpecialFaceIcon specialId)
		{
			__result = GetSprite(__result, "head", specialId.ToString());
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAvatarPartSpriteAddress))]
		private static void SpriteData_GetAvatarPartSpriteAddress(ref SpriteAddress __result, string sprite)
		{
			__result = GetSprite(__result, sprite, "");
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
		[HarmonyPatch(typeof(City), nameof(City.UpdateObject))]
		private static void City_UpdateObject(City __instance)
		{
			if(__instance.state.name != null){
				if ((int)__instance.Owner.tribe > 17){
					__instance.cityRenderer.Tribe = TribeData.Type.Imperius;
					__instance.cityRenderer.SkinType = SkinType.Default;
					__instance.cityRenderer.RefreshCity();
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Unit), nameof(Unit.SetVisible))]
		private static void UpdateObject(Unit __instance)
		{
			try
			{
				Transform unitTransform = new Transform();
				if(__instance.transform.parent.childCount > 0)
				{
					foreach (var unit in __instance.transform.parent)
					{
						Type type = typeof(Transform);
						if(unit.GetType() == type)
						{
							unitTransform = (Transform)unit;
							unitTransform = unitTransform.gameObject.transform;
						}
						else
						{
							return;
						}
					}
					Transform spriteContainerTransform = unitTransform.Find("SpriteContainer");
					if (spriteContainerTransform != null)
					{
						GameObject spriteContainer = spriteContainerTransform.gameObject;
						Transform headTransform = spriteContainer.transform.Find("Head");

						if(headTransform != null)
						{
							SpriteRenderer sr = headTransform.gameObject.GetComponent<SpriteRenderer>();

							if(sr != null)
							{
								var dictionaryEntry = ModLoader.gldDictionary.FirstOrDefault(x => x.Value == (int)__instance.Owner.skinType);
								if (!string.IsNullOrEmpty(dictionaryEntry.Key))
								{
									string idKey = dictionaryEntry.Key.ToLower();

									if (!string.IsNullOrEmpty(idKey))
									{
										string spritesKey = "head_" + idKey + "_";

										if (ModLoader.sprites.ContainsKey(spritesKey))
										{
											sr.sprite = ModLoader.sprites[spritesKey];
										}
									}
								}
							}
							else
							{
								return;
							}
						}
					}
				}
			}
			catch {}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIWorldPreview), nameof(UIWorldPreview.SetPreview), new Type[] {})]
		private static void UIWorldPreview_SetPreview(UIWorldPreview __instance) //bad idea to do it here, i will find better place later.
		{
			//base.Show(origin);
			foreach(var image in GameObject.FindObjectsOfType<UnityEngine.UI.Image>())
			{
				if(image.name == "Head")
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

        public static Sprite BuildSprite(byte[] data, Vector2 pivot)
		{
			Texture2D texture = new(1, 1);
			texture.filterMode = FilterMode.Trilinear;
			texture.LoadImage(data);
			return Sprite.Create(texture, new(0, 0, texture.width, texture.height), pivot, 2112);
		}

		private static SpriteAddress GetSprite(SpriteAddress sprite, string name, string style = "", int level = 0)
		{
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

		public static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(SpritesLoader));
		}
    }
}