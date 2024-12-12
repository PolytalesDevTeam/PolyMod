using System.Reflection;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace PolyMod
{
    internal static class Visual {
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
        private static void StartScreen_Start()
        {
            GameObject originalButton = GameObject.Find("StartScreen/WeeklyChallengesButton");
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.active = true;
            button.GetComponentInChildren<TMPLocalizer>().Text = "PolyMod";
            button.transform.Find("IconContainer").GetComponentInChildren<Image>().sprite 
                = SpritesLoader.BuildSprite(Plugin.GetResource("icon.png").ReadBytes(), new Vector2(.5f, .5f));
        }

        internal static void Init() {
            Harmony.CreateAndPatchAll(typeof(Visual));
        }
    }
}