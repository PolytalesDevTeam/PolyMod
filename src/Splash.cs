using System.Reflection;
using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;

namespace PolyMod
{
    internal static class Splash {
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

        internal static void Init() {
            Harmony.CreateAndPatchAll(typeof(Splash));
        }
    }
}