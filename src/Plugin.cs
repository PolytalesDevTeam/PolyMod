using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
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
		internal static bool mapMakerSettedUp = false;

        [HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update()
		{
			Update();
		}

		public override void Load()
		{
			Start();
			Harmony.CreateAndPatchAll(typeof(Plugin));
			Harmony.CreateAndPatchAll(typeof(BotGame));
			Harmony.CreateAndPatchAll(typeof(MapManager));
			Harmony.CreateAndPatchAll(typeof(ModLoader));
			Harmony.CreateAndPatchAll(typeof(UI));
			logger = Log;
   			//Harmony.CreateAndPatchAll(typeof(Jkdev));
        }

		internal static void Start()
		{
			BotGame.Init();
			MapManager.Init();
   		}

		internal static void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab) && !UI.active)
			{
				UI.Show();
			}

			if (GameManager.Instance.isLevelLoaded && MapManager.isInMapMaker && !mapMakerSettedUp)
			{
				HudButtonBar buttonBar = (UIManager.Instance.GetScreen(UIConstants.Screens.Hud) as HudScreen).buttonBar;
				buttonBar.blockRefreshingNextTurnButton = true;
				buttonBar.blockRefreshingStatsButton = true;
				buttonBar.blockRefreshingTechTreeButton = true;
				buttonBar.positionDisplay.gameObject.SetActive(false);
				buttonBar.nextTurnButton.BlockButton = true;
				buttonBar.techTreeButton.BlockButton = true;
				buttonBar.statsButton.BlockButton = true;
				mapMakerSettedUp = true;
			}
		}

		internal static string GetJTokenName(JToken token, int n = 1)
		{
			return token.Path.Split('.')[^n];
		}
	}
}
