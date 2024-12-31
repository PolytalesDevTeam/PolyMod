using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolyMod
{
    internal static class Visual
    {
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
        private static void StartScreen_Start()
        {
            GameObject originalButton = GameObject.Find("StartScreen/WeeklyChallengesButton");
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.active = true;
            button.GetComponentInChildren<TMPLocalizer>().Text = "PolyMod Discord";
            Transform iconContainer = button.transform.Find("IconContainer");
            iconContainer.GetComponentInChildren<Image>().sprite
                = SpritesLoader.BuildSprite(Plugin.GetResource("discord_icon.png").ReadBytes(), new Vector2(.5f, .5f));
            iconContainer.localScale = new Vector3(0.55f, 0.6f, 0);
            iconContainer.position -= new Vector3(0, 4, 0);

            UIRoundButton buttonObject = button.GetComponent<UIRoundButton>();
            buttonObject.OnClicked += (UIButtonBase.ButtonAction)
                ((int id, BaseEventData eventdata) => NativeHelpers.OpenURL("https://discord.gg/eWPdhWtfVy", false));
        }

        internal static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(Visual));
        }
    }
}