using HarmonyLib;
using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal static class BotGame
	{
		private static int _mode;
		private static bool _unview = false;
		private static LocalClient? _localClient = null;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateCustomGameModeList))]
		private static bool GameSetupScreen_CreateCustomGameModeList(ref GameSetupScreen __instance, ref UIHorizontalList __result)
		{
			string[] array = new string[]
			{
				Localization.Get(GameModeUtils.GetTitle(GameMode.Perfection)),
				Localization.Get(GameModeUtils.GetTitle(GameMode.Domination)),
				Localization.Get(GameModeUtils.GetTitle(GameMode.Sandbox)),
				Localization.Get(GameModeUtils.GetTitle((GameMode)_mode)),
			};
			__result = __instance.CreateHorizontalList("gamesettings.mode", array, new Action<int>(__instance.OnCustomGameModeChanged), __instance.GetCustomGameModeIndexFromSettings(), null, -1, null);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeUtils), nameof(GameModeUtils.GetTitle))]
		private static bool GameModeUtils_GetTitle(GameMode gameMode, ref string __result)
		{
			if (IsBotGame(gameMode))
			{
				__result = "gamemode.bot";
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeUtils), nameof(GameModeUtils.GetDescription))]
		private static bool GameModeUtils_GetDescription(GameMode gameMode, ref string __result)
		{
			if (IsBotGame(gameMode))
			{
				__result = "gamemode.bot.description";
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.GetCustomGameModeIndexFromSettings))]
		private static bool GameSetupScreen_GetCustomGameModeIndexFromSettings(ref int __result)
		{
			if (IsBotGame(GameManager.PreliminaryGameSettings.RulesGameMode))
			{
				__result = 3;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnCustomGameModeChanged))]
		private static bool GameSetupScreen_OnCustomGameModeChanged(ref GameSetupScreen __instance, int index)
		{
			if (index == 3)
			{
				GameManager.PreliminaryGameSettings.RulesGameMode = (GameMode)_mode;
				__instance.UpdateOpponentList();
				GameManager.PreliminaryGameSettings.SaveToDisk();
				__instance.RefreshInfo();
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.UpdateOpponentList))]
		private static bool GameSetupScreen_UpdateOpponentList(ref GameSetupScreen __instance)
		{
			if (GameManager.PreliminaryGameSettings.BaseGameMode != GameMode.Custom)
			{
				return true;
			}
			if (__instance.opponentList == null)
			{
				return false;
			}
			if (GameManager.PreliminaryGameSettings.GameType == GameType.Matchmaking)
			{
				return false;
			}
			bool flag2 = GameManager.PreliminaryGameSettings.RulesGameMode != GameMode.Domination && !IsBotGame(GameManager.PreliminaryGameSettings.RulesGameMode);
			bool flag4 = !IsBotGame(GameManager.PreliminaryGameSettings.RulesGameMode);
			int num = MapDataExtensions.GetMaximumOpponentCountForMapSize(GameManager.PreliminaryGameSettings.MapSize, GameManager.PreliminaryGameSettings.mapPreset);
			for (int i = 0; i < __instance.opponentList.items.Length; i++)
			{
				bool flag3 = (flag2 && i == 0) || (i > 0 && i <= num);
				__instance.opponentList.items[i].gameObject.SetActive(flag3);
				__instance.opponentList.items[i].text = flag4 ? i.ToString() : (i + 1).ToString();
			}
			__instance.opponentList.RefreshScrollpositions();
			__instance.opponentList.RefreshNavigationIndexes();
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameRules), nameof(GameRules.LoadPreset))]
		private static bool GameRules_LoadPreset(ref GameRules __instance, GameMode gameMode)
		{
			if (IsBotGame(gameMode))
			{
				__instance = new GameRules
				{
					AllowMirrorPick = false,
					AllowTechSharing = true,
					AllowSpecialTribes = true,
					ScoreLimit = 0,
					TurnLimit = 0,
					WinByCapital = false,
					WinByExtermination = true,
					PlayerDeathCondition = GameRules.DeathCondition.Cities
				};
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.OnGameReady))]
		private static void GameManager_OnGameReady()
		{
			if (IsBotGame())
			{
				GameManager.instance.OnFinishedProcessingActions();
				Log.Info("Made move");
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartMatchReaction), nameof(StartMatchReaction.DoWelcomeCinematic))]
		private static bool StartMatchReaction_DoWelcomeCinematic(ref StartMatchReaction __instance, Action onComplete)
		{
			if (!IsBotGame())
			{
				return true;
			}
			__instance.StartGame(onComplete);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientBase), nameof(ClientBase.IsPlayerLocal))]
		public static bool ClientBase_IsPlayerLocal(ref bool __result)
		{
			if (!IsBotGame())
			{
				return true;
			}
			__result = true;
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.IsPlayerViewing))]
		public static bool GameManager_IsPlayerViewing(ref bool __result)
		{
			if (IsBotGame() && _unview)
			{
				__result = false;
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(WipePlayerReaction), nameof(WipePlayerReaction.Execute))]
		public static bool WipePlayerReaction_Execute()
		{
			if (!IsBotGame())
			{
				return true;
			}
			GameManager.Client.ActionManager.isRecapping = true;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(WipePlayerReaction), nameof(WipePlayerReaction.Execute))]
		public static void WipePlayerReaction_Execute_Postfix()
		{
			if (!IsBotGame())
			{
				return;
			}
			GameManager.Client.ActionManager.isRecapping = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.Execute))]
		public static bool StartTurnReaction_Execute()
		{
			if (!IsBotGame())
			{
				return true;
			}
			_localClient = GameManager.Client as LocalClient;
			if (_localClient == null)
			{
				return true;
			}
			GameManager.instance.client = new ReplayClient();
			GameManager.Client.currentGameState = _localClient.GameState;
			GameManager.Client.CreateOrResetActionManager(_localClient.lastSeenCommand);
			GameManager.Client.ActionManager.isRecapping = true;
			LevelManager.GetClientInteraction().ClearSelection();
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.DoStartTurnNotification))]
		public static bool StartTurnReaction_DoStartTurnNotification()
		{
			if (!IsBotGame())
			{
				return true;
			}
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
		public static bool HudScreen_OnMatchStart()
		{
			if (!IsBotGame())
			{
				return true;
			}
			UIManager.Instance.ShowScreen(UIConstants.Screens.Hud, false);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapRenderer), nameof(MapRenderer.Refresh))]
		public static bool MapRenderer_Refresh()
		{
			if (!IsBotGame())
			{
				return true;
			}
			if (_localClient != null)
			{
				GameManager.instance.client = _localClient;
				_localClient = null;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		public static bool TaskCompletedReaction_Execute(ref TaskCompletedReaction __instance, ref byte __state)
		{
			if (!IsBotGame())
			{
				return true;
			}
			__state = __instance.action.PlayerId;
			__instance.action.PlayerId = 255;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		public static void TaskCompletedReaction_Execute_Postfix(ref TaskCompletedReaction __instance, ref byte __state)
		{
			if (!IsBotGame())
			{
				return;
			}
			__instance.action.PlayerId = __state;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		[HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		[HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
		[HarmonyPatch(typeof(InfiltrationRewardReaction), nameof(InfiltrationRewardReaction.Execute))]
		[HarmonyPatch(typeof(EstablishEmbassyReaction), nameof(EstablishEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(DestroyEmbassyReaction), nameof(DestroyEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(ReceiveDiplomacyMessageReaction), nameof(ReceiveDiplomacyMessageReaction.Execute))]
		public static bool Patch_Execute()
		{
			if (!IsBotGame())
			{
				return true;
			}
			_unview = true;
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		[HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		[HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
		[HarmonyPatch(typeof(InfiltrationRewardReaction), nameof(InfiltrationRewardReaction.Execute))]
		[HarmonyPatch(typeof(EstablishEmbassyReaction), nameof(EstablishEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(DestroyEmbassyReaction), nameof(DestroyEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(ReceiveDiplomacyMessageReaction), nameof(ReceiveDiplomacyMessageReaction.Execute))]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.Execute))]
		public static void Patch_Execute_Post()
		{
			if (!IsBotGame())
			{
				if (_unview)
				{
					DebugConsole.Write("Uh, what!?");
				}
				return;
			}
			_unview = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientActionManager), nameof(ClientActionManager.Update))]
		public static void Patch_Update()
		{
			if (!IsBotGame())
			{
				return;
			}
			if (_localClient != null)
			{
				GameManager.instance.client = _localClient;
				_localClient = null;
			}
			_unview = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(ClientBase), nameof(ClientBase.CreateSession))]
		public static void ClientBase_CreateSession(ref ClientBase __instance)
		{
			if (__instance.clientType != ClientBase.ClientType.Local || !IsBotGame())
			{
				return;
			}
			for (int j = 0; j < __instance.GameState.PlayerCount; j++)
			{
				PlayerState playerState = __instance.GameState.PlayerStates[j];
				playerState.AutoPlay = false;
				playerState.UserName = AccountManager.AliasInternal;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientBase), nameof(ClientBase.GetCurrentLocalPlayer))]
		public static bool ClientBase_GetCurrentLocalPlayer(ref PlayerState __result, ref ClientBase __instance)
		{
			if (GameManager.GameState == null || !IsBotGame())
			{
				return true;
			}
			__result = __instance.GameState.PlayerStates[__instance.GameState.CurrentPlayerIndex];
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AI), nameof(AI.GetGameProgress))]
		public static bool AI_GetGameProgress(GameState gameState, PlayerState winningPlayer, ref float __result)
		{
			if (!IsBotGame())
			{
				return true;
			}
			float num = winningPlayer.cities / Math.Max(0.1f, MapDataExtensions.CountCities(gameState));
			float num2 = gameState.CurrentTurn / (float)gameState.Settings.rules.TurnLimit;
			__result = Math.Max(num, num2);
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadLevel))]
		public static void GameManager_LoadLevel()
		{
			GameManager.debugAutoPlayLocalPlayer = IsBotGame();
		}

		public static void Init()
		{
			_mode = Enum.GetValues(typeof(GameMode)).Length;
			EnumCache<GameMode>.AddMapping("Bot", (GameMode)_mode);
		}

		public static bool IsBotGame(GameMode gameMode)
		{
			return gameMode == (GameMode)_mode;
		}

		public static bool IsBotGame(GameState gameState)
		{
			return IsBotGame(gameState.Settings.RulesGameMode);
		}

		public static bool IsBotGame()
		{
			return IsBotGame(GameManager.GameState);
		}
	}
}