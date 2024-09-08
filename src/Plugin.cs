using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;

namespace PolyMod
{
	[BepInPlugin("com.polytalesteam.polymod", "PolyMod", "0.0.0")]
	public class Plugin : BepInEx.Unity.IL2CPP.BasePlugin
	{
		internal static readonly string BASE_PATH = Path.Combine(BepInEx.Paths.BepInExRootPath, "..");
		internal static readonly string MODS_PATH = Path.Combine(BASE_PATH, "Mods");
		internal static readonly JsonMergeSettings GLD_MERGE_SETTINGS = new() { MergeArrayHandling = MergeArrayHandling.Replace, MergeNullValueHandling = MergeNullValueHandling.Merge };

#pragma warning disable CS8618
		internal static ManualLogSource logger;
#pragma warning restore CS8618

		public override void Load()
		{
			ModLoader.Init();
			VisualsManager.Init();
			logger = Log;
		}
	}
}
