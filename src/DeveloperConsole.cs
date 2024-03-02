using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;

namespace PolyMod
{
	internal class DeveloperConsole
	{
		private static bool _console = false;

		internal static void Init()
		{
			AddCommand("stars_get", "(amount)", (args) =>
			{
				if (args.Length < 1)
				{
					DebugConsole.Write("Too few args!");
					return;
				}

				int amount = int.Parse(args[0]);
				GameManager.LocalPlayer.Currency += amount;
				DebugConsole.Write($"+{amount} stars");
			});
			AddCommand("map_set", "(name)", (args) =>
			{
				if (args.Length < 1)
				{
					DebugConsole.Write("Too few args!");
					return;
				}

				MapLoader.map = JObject.Parse(File.ReadAllText(Path.Combine(Plugin.MAPS_PATH, args[0] + ".json")));
				DebugConsole.Write($"Map set");
			});
			AddCommand("map_unset", "", (args) =>
			{
				MapLoader.map = null;
				DebugConsole.Write($"Map unset");
			});
			AddCommand("version_change", "(version)", (args) =>
			{
				if (args.Length < 1)
				{
					DebugConsole.Write("Too few args!");
					return;
				}

				Plugin.version = int.Parse(args[0].ToString());
				DebugConsole.Write($"Next game will start with version {Plugin.version}");
			});
			AddCommand("replay_resume", "", (args) =>
			{
				ReplayResumer.Resume();
			});
		}

		internal static void Toggle()
		{
			if (_console)
			{
				DebugConsole.Hide();
			}
			else
			{
				DebugConsole.Show();
			}
			_console = !_console;
		}
		private static void AddCommand(string name, string description, Action<Il2CppStringArray> container)
		{
			DebugConsole.AddCommand(name, DelegateSupport.ConvertDelegate<DebugConsole.CommandDelegate>(container), description);
		}
	}
}
