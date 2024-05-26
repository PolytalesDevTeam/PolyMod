using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MoonSharp.Interpreter;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PolyMod
{
	[BepInPlugin("com.polymod", "PolyMod", "0.0.0")]
	public class Plugin : BepInEx.Unity.IL2CPP.BasePlugin
	{
		internal const int MAP_MAX_PLAYERS = 100;
		internal const float CAMERA_MAXZOOM_CONSTANT = 1000;
		internal const int AUTOIDX_STARTS_FROM = 1000;
		internal static readonly string BASE_PATH = Path.Combine(BepInEx.Paths.BepInExRootPath, "..");
		internal static readonly string MODS_PATH = Path.Combine(BASE_PATH, "Mods");
		internal static readonly string MAPS_PATH = Path.Combine(BASE_PATH, "Maps");
		internal static readonly JsonMergeSettings GLD_MERGE_SETTINGS = new() { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

#pragma warning disable CS8618
		internal static ManualLogSource logger;
#pragma warning restore CS8618
		internal static int version = -1;
		internal static bool start = false;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update()
		{
			Update();
		}

		public override void Load()
		{
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Harmony.CreateAndPatchAll(typeof(BotGame));
			Harmony.CreateAndPatchAll(typeof(MapLoader));
			Harmony.CreateAndPatchAll(typeof(ModLoader));
			Harmony.CreateAndPatchAll(typeof(UI));
			logger = Log;
		}

		internal static void Start()
		{
			BotGame.Init();
			MapLoader.Init();
		}

		internal static void Update()
		{
			if (!start)
			{
				Start();
				start = true;
			}

			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab) && !UI.active)
			{
				UI.Show();
			}

			foreach (var script in ModLoader.scripts)
			{
				object? dynValue = script.Globals["update"];
				if (dynValue != null)
				{
					script.Call(dynValue);
				}
			}
		}

		internal static string GetJTokenName(JToken token, int n = 1)
		{
			return token.Path.Split('.')[^n];
		}
	}
}
