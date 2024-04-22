using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal static class BotGame
	{
		public static int bot;
		public static bool unview = false;
		public static LocalClient? localClient = null;

		public static void Init()
		{
			bot = Enum.GetValues(typeof(GameMode)).Length;
			EnumCache<GameMode>.AddMapping("Bot", (GameMode)bot);
		}
	}
}