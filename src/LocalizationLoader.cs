using HarmonyLib;
using Polytopia.Data;

namespace PolyMod
{
    public class LocalizationLoader
    {
        [HarmonyPostfix]
		[HarmonyPatch(typeof(SelectTribePopup), nameof(SelectTribePopup.SetTribeSkins))]
		private static void SetTribeSkins(SelectTribePopup __instance)
		{
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SelectTribePopup), nameof(SelectTribePopup.SetDescription))] //TODO REVAMP
		private static void SetDescription(SelectTribePopup __instance)
		{
			__instance.uiTextButton.ButtonEnabled = true;
			__instance.uiTextButton.gameObject.SetActive(true);
			if((int)__instance.SkinType > ModLoader.initialSkinsCount){
				__instance.Description = Localization.Get(__instance.SkinType.GetLocalizationDescriptionKey()) + "\n\n" + Localization.GetSkinned(__instance.SkinType, __instance.tribeData.description2, new Il2CppSystem.Object[]
				{
					__instance.tribeName,
					Localization.Get(__instance.startTechSid, Array.Empty<Il2CppSystem.Object>())
				});
			}
			Console.Write(__instance.Description);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SkinTypeExtensions), nameof(SkinTypeExtensions.GetSkinNameKey))]
		public static void GetSkinNameKey(ref string __result)
		{
			__result = "TribeSkins/skinName";
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SkinTypeExtensions), nameof(SkinTypeExtensions.GetLocalizationKey))]
		public static void GetLocalizationKey(ref string __result, SkinType skinType)
		{
			if((int)skinType > ModLoader.initialSkinsCount){
				Console.Write(skinType);
				__result = "tribeskins." + skinType.GetName<SkinType>();
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SkinTypeExtensions), nameof(SkinTypeExtensions.GetLocalizationDescriptionKey))]
		public static void GetLocalizationDescriptionKey(ref string __result, SkinType skinType)
		{
			if((int)skinType > ModLoader.initialSkinsCount){
				Console.Write(skinType);
				__result = "tribeskins." + skinType.GetName<SkinType>() + ".description";
			}
		}

        public static void Init()
		{
			Harmony.CreateAndPatchAll(typeof(LocalizationLoader));
		}
    }
}