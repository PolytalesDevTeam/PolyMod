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
#pragma warning disable CS8604
            File.WriteAllBytes(path, Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "PolyMod.data.intro.mp4"
            ).ReadBytes());
#pragma warning restore CS8604
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