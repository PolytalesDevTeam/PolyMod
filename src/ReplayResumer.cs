using Il2CppSystem.Runtime.CompilerServices;
using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal static class ReplayResumer
	{
		private static ClientBase? _replayClient;
		internal static string nameStart = "(From move ";
		private static string _nameEnd = ")";

		internal static void BackToReplay()
		{
			ClientBase passAndPlayClient = GameManager.Client;
			if (!passAndPlayClient.GameState.Settings.GameName.StartsWith(nameStart))
			{
				Plugin.logger.LogWarning(string.Format("{0} Command used outside of resumed game, name is {1}", "<color=#FFFFFF>[GameManager]</color>", passAndPlayClient.GameState.Settings.GameName));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			if (passAndPlayClient.GameState.Settings.GameType != GameType.PassAndPlay)
			{
				Plugin.logger.LogWarning(string.Format("{0} Command used outside of resumed game, type is {1}", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>", passAndPlayClient.GameState.Settings.GameType.ToString() }));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			if (_replayClient == null)
			{
				Plugin.logger.LogWarning(string.Format("No replay client to return to", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" }));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			if (_replayClient.gameId.ToString() != passAndPlayClient.GameState.Settings.GameName[^36..])
			{
				Plugin.logger.LogWarning(string.Format("{0} Replay client game id does not match resumed game id", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" }));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			Plugin.logger.LogInfo(string.Format("{0} Loading replay {1} Game", new Il2CppSystem.Object[]
			{
				"<color=#FFFFFF>[GameManager]</color>",
				_replayClient.initialGameState.Settings.BaseGameMode.ToString()
			}));
			GameManager.instance.SetLoadingGame(true);
			GameManager.instance.client = _replayClient;
			GameManager.instance.LoadLevel();
		}
		internal static void Resume()
		{
			ClientBase replayClient = GameManager.Client;
			if (!replayClient.IsReplay)
			{
				Plugin.logger.LogWarning(string.Format("{0} Command used outside of replay game, client is {1}", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>", GameManager.Client.GetType().ToString() }));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			GameManager.instance.SetLoadingGame(true);
			Plugin.logger.LogInfo(string.Format("{0} Loading new Hotseat {1} Game from replay", new Il2CppSystem.Object[]
			{
				"<color=#FFFFFF>[GameManager]</color>",
				GameManager.instance.settings.BaseGameMode.ToString()
			}));

			HotseatClient hotseatClient = SetHotseatClient();
			if (hotseatClient == null)
			{
				Plugin.logger.LogWarning(string.Format("{0} Failed to create Hotseat game", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" }));
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			Plugin.logger.LogInfo(string.Format("{0} Created new Hotseat game", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" }));
			TaskAwaiter<bool> taskAwaiter = TransformClient(replayClient, hotseatClient).GetAwaiter();
			if (taskAwaiter.GetResult())
			{
				GameManager.instance.LoadLevel();
				BasicPopup resumePopup = PopupManager.GetBasicPopup();
				resumePopup.Header = "Resuming from Replay";
				resumePopup.Description = "The game has been turned from a replay into a hotseat game. You can now continue playing.";
				resumePopup.buttonData = new PopupBase.PopupButtonData[] { new(Localization.Get("buttons.ok"), PopupBase.PopupButtonData.States.Selected, null, -1, true, null) };
				resumePopup.Show();
			}
		}

		internal static HotseatClient SetHotseatClient()
		{
			GameManager.instance.settings.GameType = GameType.PassAndPlay;
			Plugin.logger.LogInfo(string.Format("{0} Setting up hotseat client...", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" }));
			HotseatClient hotseatClient = new()
			{
				OnConnected = new Action(GameManager.instance.OnLocalClientConnected),
				OnDisconnected = new Action(GameManager.instance.OnClientDisconnected),
				OnSessionOpened = new Action(GameManager.instance.OnClientSessionOpened),
				OnStateUpdated = new Action<StateUpdateReason>(GameManager.instance.OnStateUpdated),
				OnStartedProcessingActions = new Action(GameManager.instance.OnStartedProcessingActions),
				OnFinishedProcessingActions = new Action(GameManager.instance.OnFinishedProcessingActions)
			};
			GameManager.instance.client = hotseatClient;
			return hotseatClient;
		}

		internal static Il2CppSystem.Threading.Tasks.Task<bool> TransformClient(ClientBase replayClient, HotseatClient hotseatClient)
		{
			ReplayResumer._replayClient = replayClient;
			GameState initialGameState = replayClient.initialGameState;
			initialGameState.Settings.GameType = GameType.PassAndPlay;
			initialGameState.Settings.gameName = nameStart + replayClient.GetLastSeenCommand().ToString() + _nameEnd + initialGameState.Settings.gameName + replayClient.gameId.ToString();
			byte[] array = SerializationHelpers.ToByteArray(initialGameState, replayClient.initialGameState.Version);
			SerializationHelpers.FromByteArray(array, out GameState currentGameState);
			SerializationHelpers.FromByteArray(array, out GameState _);
			SerializationHelpers.FromByteArray(array, out GameState otherCurrentGameState);
			for (int i = 0; i < replayClient.GetLastSeenCommand(); i++)
			{
				otherCurrentGameState.CommandStack.Add(replayClient.currentGameState.CommandStack[i]);
			}
			ExecuteCommands(currentGameState, otherCurrentGameState.CommandStack, out _, out _, out string? error);
			if (error != null)
			{
				Plugin.logger.LogError(string.Format("{0} Failed to execute commands: {1}", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>", error }));
				return Il2CppSystem.Threading.Tasks.Task.FromResult<bool>(false);
			}
			Plugin.logger.LogInfo(string.Format("{0} Transforming replay session...", new Il2CppSystem.Object[]
				{HotseatClient.LOG_PREFIX,
			}));
			hotseatClient.Reset();
			hotseatClient.gameId = Il2CppSystem.Guid.NewGuid();
			hotseatClient.SetSavedSeenCommands(new ushort[replayClient.currentGameState.PlayerStates.Count]);
			hotseatClient.initialGameState = initialGameState;
			hotseatClient.currentGameState = currentGameState;
			hotseatClient.lastTurnGameState = initialGameState;
			hotseatClient.lastSeenCommands = new ushort[replayClient.currentGameState.PlayerStates.Count];
			for (int j = 0; j < replayClient.currentGameState.PlayerStates.Count; j++)
			{
				hotseatClient.lastSeenCommands[j] = (ushort)replayClient.GetLastSeenCommand();
			}
			hotseatClient.currentLocalPlayerIndex = hotseatClient.currentGameState.CurrentPlayerIndex;
			hotseatClient.hasInitializedSaveData = true;
			hotseatClient.UpdateGameStateImmediate(hotseatClient.currentGameState, StateUpdateReason.GameJoined);
			hotseatClient.PrepareSession();
			return Il2CppSystem.Threading.Tasks.Task.FromResult(true);
		}

		private static bool ExecuteCommands(GameState gameState, Il2CppSystem.Collections.Generic.List<CommandBase> commands, out Il2CppSystem.Collections.Generic.List<CommandBase> executedCommands, out Il2CppSystem.Collections.Generic.List<CommandResultEvent> events, out string? error)
		{
			executedCommands = new Il2CppSystem.Collections.Generic.List<CommandBase>();
			events = new Il2CppSystem.Collections.Generic.List<CommandResultEvent>();
			error = null;
			byte currentPlayer = gameState.CurrentPlayer;
			try
			{
				ActionManager actionManager = new(gameState);
				foreach (CommandBase commandBase in commands)
				{
					GameState.State currentState = gameState.CurrentState;
					uint currentTurn = gameState.CurrentTurn;
					if (!actionManager.ExecuteCommand(commandBase, out error))
					{
						return false;
					}
					executedCommands.Add(commandBase);
					if (GameStateUtils.RegisterCommandResultEvent(gameState, currentState, currentTurn, commandBase, out CommandResultEvent commandResultEvent, false))
					{
						events.Add(commandResultEvent);
					}
				}
				while (gameState.TryGetPlayer(gameState.CurrentPlayer, out PlayerState playerState) && playerState.AutoPlay && gameState.CurrentState != GameState.State.Ended)
				{
					if (!CommandTriggerUtils.TryGetTriggerCommand(gameState, out CommandBase move))
					{
						move = AI.GetMove(gameState, playerState, CommandType.None);
					}
					GameState.State currentState2 = gameState.CurrentState;
					uint currentTurn2 = gameState.CurrentTurn;
					if (!actionManager.ExecuteCommand(move, out error))
					{
						throw new System.Exception(string.Format("AI Failed to perform command: {0} with error {1})", move.ToString(), error));
					}
					executedCommands.Add(move);
					if (GameStateUtils.RegisterCommandResultEvent(gameState, currentState2, currentTurn2, move, out CommandResultEvent commandResultEvent2, playerState.Id == currentPlayer))
					{
						events.Add(commandResultEvent2);
					}
				}
			}
			catch (Exception ex)
			{
				error = ex.ToString();
				Console.WriteLine(ex);
				executedCommands = new Il2CppSystem.Collections.Generic.List<CommandBase>();
				return false;
			}
			return true;
		}
	}
}