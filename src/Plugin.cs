using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;

namespace PolyMod
{
	[BepInPlugin("com.polymod", "PolyMod", VERSION)]
	public class Plugin : BepInEx.Unity.IL2CPP.BasePlugin
	{
		internal const string VERSION = "0.0.0";
		internal const int AUTOIDX_STARTS_FROM = 1000;
		internal static readonly string BASE_PATH = Path.Combine(BepInEx.Paths.BepInExRootPath, "..");
		internal static readonly string MODS_PATH = Path.Combine(BASE_PATH, "Mods");

#pragma warning disable CS8618
		internal static ManualLogSource logger;
#pragma warning restore CS8618

		public override void Load()
		{
			logger = Log;
			ConfigFile.CoreConfig[new("Logging.Disk", "WriteUnityLog")].BoxedValue = true;
			AudioClipLoader.Init();
			ModLoader.Init();
			SpritesLoader.Init();
			Visual.Init();
			//PolyBreaker.Init();
		}

		internal static Stream GetResource(string id)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(
				$"{typeof(Plugin).Namespace}.resources.{id}"
			)!;
		}

		internal static Il2CppSystem.Type WrapType<T>() where T : class
		{
			if (!ClassInjector.IsTypeRegisteredInIl2Cpp<T>())
				ClassInjector.RegisterTypeInIl2Cpp<T>();
			return Il2CppType.From(typeof(T));
		}
	}
}
