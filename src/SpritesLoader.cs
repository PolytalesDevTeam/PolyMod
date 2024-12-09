using HarmonyLib;
using I2.Loc;
using LibCpp2IL;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.EventSystems;

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
			//__instance.transform.FindChild("SpriteContainer/Head")
			//	.GetComponent<SpriteRenderer>().sprite = ModLoader.GetSprite("head", "minerskagg");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Resource), nameof(Resource.SetVisible))]
		private static void Resource_SetVisible(Resource __instance)
		{
			if (__instance.Owner == null) return;
			__instance.Sprite
					= ModLoader.GetSprite(
						EnumCache<ResourceData.Type>.GetName(__instance.data.type).ToLower(), 
						ModLoader.gldDictionaryInversed[(int)__instance.Owner.tribe]
					);
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