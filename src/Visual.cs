using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolyMod
{
    internal static class Visual
    {
        private static bool isPolymodScreenActive = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SplashController), nameof(SplashController.LoadAndPlayClip))]
        private static bool SplashController_LoadAndPlayClip(SplashController __instance)
        {
            string name = "intro.mp4";
            string path = Path.Combine(Application.persistentDataPath, name);
            File.WriteAllBytesAsync(path, Plugin.GetResource(name).ReadBytes());
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
                isPolymodScreenActive = true;
                __instance.ShowBetaInfo();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIScreenBase), nameof(UIScreenBase.Show))]
        private static void BetaInfoScreen_Show(UIScreenBase __instance, bool instant = false)
        {
            if (isPolymodScreenActive)
            {
                GameObject screenHeader = GameObject.Find("BetaInfoScreen/Header TMP");
                screenHeader.GetComponent<TMPLocalizer>().Text = "-POLYMOD HUB-";

                GameObject headerOne = GameObject.Find("BetaInfoScreen/Scroller/Container/Header TMP (1)");
                headerOne.GetComponent<TMPLocalizer>().Text = "Welcome!";

                GameObject parapgraphOne = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP");
                parapgraphOne.GetComponent<TMPLocalizer>().Text = "Join us! Feel free to discuss mods, create them and ask for help!";

                GameObject headerTwo = GameObject.Find("BetaInfoScreen/Scroller/Container/Header TMP (3)");
                headerTwo.GetComponent<TMPLocalizer>().Text = "Mods";

                GameObject parapgraphTwo = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP (2)");

                GameObject textButtonObject = GameObject.Find("BetaInfoScreen/Scroller/Container/TextButton");
                textButtonObject.GetComponentInChildren<TMPLocalizer>().Text = "OUR DISCORD";
                string modsText = "";
                foreach (var (id, mod) in ModLoader.mods)
                    modsText += $"ID: {id}\nStatus: {mod.GetPrettyStatus()}\n\n";
                parapgraphTwo.GetComponent<TMPLocalizer>().Text = modsText;
                GameObject headerThree = GameObject.Find("BetaInfoScreen/Scroller/Container/Header TMP (4)");
                headerThree.active = false;
                GameObject parapgraphThree = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP (3)");
                parapgraphThree.active = false;
                GameObject divider = GameObject.Find("BetaInfoScreen/Scroller/Container/Divider");
                divider.active = false;
                GameObject dividerOne = GameObject.Find("BetaInfoScreen/Scroller/Container/Divider (1)");
                dividerOne.active = false;
                GameObject backButtonObject = GameObject.Find("BetaInfoScreen/BackButton");
                UIButtonBase backButton = backButtonObject.GetComponent<UIButtonBase>();
                backButton.OnClicked += (UIButtonBase.ButtonAction)BackButtonOnClicked;

                void BackButtonOnClicked(int id, BaseEventData eventdata)
                {
                    UIManager.Instance.OnBack();
                    isPolymodScreenActive = false;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
        private static void GameManager_Update()
        {
            if (isPolymodScreenActive)
            {
                GameObject textButton = GameObject.Find("BetaInfoScreen/Scroller/Container/TextButton");
                GameObject parapgraphTwo = GameObject.Find("BetaInfoScreen/Scroller/Container/Paragraph TMP (2)");
                textButton.transform.position = parapgraphTwo.transform.position + new Vector3(0, 115, 0);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BetaInfoScreen), nameof(BetaInfoScreen.OpenDiscordLink))]
        private static bool BetaInfoScreen_OpenDiscordLink()
        {
            if (isPolymodScreenActive)
            {
                NativeHelpers.OpenURL("https://discord.gg/eWPdhWtfVy", false);
                return false;
            }
            return true;
        }

        internal static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(Visual));
        }
    }
}