using System.Reflection;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolyMod
{
    internal static class Visual {
        private static bool isPolyModScreenOpened = false;

        [HarmonyPrefix]
		[HarmonyPatch(typeof(SplashController), nameof(SplashController.LoadAndPlayClip))]
		private static bool SplashController_LoadAndPlayClip(SplashController __instance)
		{
            string path = Application.persistentDataPath + "/intro.mp4";
            File.WriteAllBytesAsync(path, Plugin.GetResource("intro.mp4").ReadBytes());
            __instance.lastPlayTime = Time.realtimeSinceStartup;
			__instance.videoPlayer.url = path;
            __instance.videoPlayer.Play();
            return false;
		}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartScreen), nameof(StartScreen.Start))]
        private static void StartScreen_Start(StartScreen __instance)
        {
            GameObject originalButton = GameObject.Find("StartScreen/WeeklyChallengesButton");
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.active = true;
            button.GetComponentInChildren<TMPLocalizer>().Text = "PolyMod";
            button.transform.Find("IconContainer").GetComponentInChildren<Image>().sprite 
                = SpritesLoader.BuildSprite(Plugin.GetResource("icon.png").ReadBytes(), new Vector2(.5f, .5f));
            UIRoundButton buttonObject = button.GetComponent<UIRoundButton>();
            buttonObject.OnClicked += (UIButtonBase.ButtonAction)OnPolyModButtonClick;

            void OnPolyModButtonClick(int id, BaseEventData eventdata)
            {
                isPolyModScreenOpened = true;
                __instance.ShowBetaInfo();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIScreenBase), nameof(UIScreenBase.Show))]
        private static void BetaInfoScreen_Show(UIScreenBase __instance, bool instant = false)
        {
            if(isPolyModScreenOpened) // will change
            {
                GameObject screenHeader = GameObject.Find("BetaInfoScreen/Header TMP");
                screenHeader.GetComponent<TMPLocalizer>().Text = "PolyMod Hub";
                GameObject headerOne = GameObject.Find("BetaInfoScreen/Scroller/Container/Header TMP (1)");
                headerOne.GetComponent<TMPLocalizer>().Text = "Welcome!";
                GameObject parapgraphOne = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP");
                parapgraphOne.GetComponent<TMPLocalizer>().Text = "";
                GameObject headerTwo = GameObject.Find("BetaInfoScreen/Scroller/Container/Header TMP (3)");
                headerTwo.GetComponent<TMPLocalizer>().Text = "Mods";
                GameObject parapgraphTwo = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP (2)");
                parapgraphTwo.GetComponent<TMPLocalizer>().Text = "";
                GameObject textButton = GameObject.Find("BetaInfoScreen/Scroller/Container/TextButton");
                textButton.GetComponentInChildren<TMPLocalizer>().Text = "OUR DISCORD";
                isPolyModScreenOpened = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BetaInfoScreen), nameof(BetaInfoScreen.OpenDiscordLink))]
        private static bool BetaInfoScreen_OpenDiscordLink() // will change
        {
            NativeHelpers.OpenURL("https://discord.gg/eWPdhWtfVy", false);
            return false;
        }

        internal static void Init() {
            Harmony.CreateAndPatchAll(typeof(Visual));
        }
    }
}