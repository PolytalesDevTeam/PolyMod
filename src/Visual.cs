using Cpp2IL.Core.Extensions;
using HarmonyLib;
using UnityEngine;

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

        internal static void Init()
        {
            Harmony.CreateAndPatchAll(typeof(Visual));
        }
    }
}