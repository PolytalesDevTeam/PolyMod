using Il2CppSystem.Runtime.CompilerServices;
using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal static class ReplayResumer
	{
		public static void Resume()
		{
			ClientBase replayClient = GameManager.Client;
			if (!replayClient.IsReplay)
			{
				Log.Warning("{0} Command used outside of replay game, client is {1}", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>", GameManager.Client.GetType().ToString() });
				GameManager.instance.SetLoadingGame(false);
				return;
			}

			GameManager.instance.SetLoadingGame(true);
			Log.Info("{0} Loading new Hotseat {1} Game from replay", new Il2CppSystem.Object[]
			{
				"<color=#FFFFFF>[GameManager]</color>",
				GameManager.instance.settings.BaseGameMode.ToString()
			});

			HotseatClient hotseatClient = SetHotseatClient();
			if (hotseatClient == null)
			{
				Log.Warning("{0} Failed to create Hotseat game", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" });
				GameManager.instance.SetLoadingGame(false);
				return;
			}
			Log.Info("{0} Created new Hotseat game", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" });
			TaskAwaiter<bool> taskAwaiter = TransformClient(replayClient, hotseatClient).GetAwaiter();
			if (taskAwaiter.GetResult())
			{
				GameManager.instance.LoadLevel();
				PopupManager.GetBasicPopup(new PopupManager.BasicPopupData
				{
					header = "Resuming from Replay",
					description = "The game has been turned from a replay into a hotseat game. You can now continue playing.",
					buttonData = new PopupBase.PopupButtonData[]
				{
					new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.Selected, null, -1, true, null)
				}
				}).Show();
			}
		}

		public static HotseatClient SetHotseatClient()
		{
			GameManager.instance.settings.GameType = GameType.PassAndPlay;
			Log.Info("{0} Setting up hotseat client...", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>" });
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

		public static Il2CppSystem.Threading.Tasks.Task<bool> TransformClient(ClientBase replayClient, HotseatClient hotseatClient)
		{
			GameState initialGameState = replayClient.initialGameState;
			GameState lastTurnGameState;
			GameState currentGameState;
			GameState otherCurrentGameState;
			initialGameState.Settings.GameType = GameType.PassAndPlay;
			byte[] array = SerializationHelpers.ToByteArray(initialGameState, replayClient.initialGameState.Version);
			SerializationHelpers.FromByteArray(array, out currentGameState);
			SerializationHelpers.FromByteArray(array, out lastTurnGameState);
			SerializationHelpers.FromByteArray(array, out otherCurrentGameState);
			for (int i = 0; i < replayClient.GetLastSeenCommand(); i++)
			{
				otherCurrentGameState.CommandStack.Add(replayClient.currentGameState.CommandStack[i]);
			}
			Il2CppSystem.Collections.Generic.List<CommandBase> executedCommands = new Il2CppSystem.Collections.Generic.List<CommandBase>();
			Il2CppSystem.Collections.Generic.List<CommandResultEvent> events = new Il2CppSystem.Collections.Generic.List<CommandResultEvent>();
			string error;
			ExecuteCommands(currentGameState, otherCurrentGameState.CommandStack, out executedCommands, out events, out error);
			if (error != null)
			{
				Log.Error("{0} Failed to execute commands: {1}", new Il2CppSystem.Object[] { "<color=#FFFFFF>[GameManager]</color>", error });
				return Il2CppSystem.Threading.Tasks.Task.FromResult<bool>(false);
			}
			Log.Info("{0} Transforming replay session...", new Il2CppSystem.Object[]
				{HotseatClient.LOG_PREFIX,
			});
			hotseatClient.Reset();
			hotseatClient.gameId = Il2CppSystem.Guid.NewGuid();
			hotseatClient.SetSavedSeenCommands(new ushort[replayClient.currentGameState.PlayerStates.Count]);
			hotseatClient.initialGameState = initialGameState;
			hotseatClient.currentGameState = currentGameState;
			hotseatClient.lastTurnGameState = initialGameState;
			hotseatClient.lastSeenCommands = new ushort[replayClient.currentGameState.PlayerStates.Count];
			hotseatClient.currentLocalPlayerIndex = hotseatClient.currentGameState.CurrentPlayerIndex;
			hotseatClient.hasInitializedSaveData = true;
			hotseatClient.UpdateGameStateImmediate(hotseatClient.currentGameState, StateUpdateReason.GameJoined);
			hotseatClient.PrepareSession();
			return Il2CppSystem.Threading.Tasks.Task.FromResult<bool>(true);
		}

		private static bool ExecuteCommands(GameState gameState, Il2CppSystem.Collections.Generic.List<CommandBase> commands, out Il2CppSystem.Collections.Generic.List<CommandBase> executedCommands, out Il2CppSystem.Collections.Generic.List<CommandResultEvent> events, out string error)
		{
			executedCommands = new Il2CppSystem.Collections.Generic.List<CommandBase>();
			events = new Il2CppSystem.Collections.Generic.List<CommandResultEvent>();
			error = null;
			byte currentPlayer = gameState.CurrentPlayer;
			try
			{
				ActionManager actionManager = new ActionManager(gameState);
				foreach (CommandBase commandBase in commands)
				{
					GameState.State currentState = gameState.CurrentState;
					uint currentTurn = gameState.CurrentTurn;
					if (!actionManager.ExecuteCommand(commandBase, out error))
					{
						return false;
					}
					executedCommands.Add(commandBase);
					CommandResultEvent commandResultEvent;
					if (GameStateUtils.RegisterCommandResultEvent(gameState, currentState, currentTurn, commandBase, out commandResultEvent, false))
					{
						events.Add(commandResultEvent);
					}
				}
				PlayerState playerState;
				while (gameState.TryGetPlayer(gameState.CurrentPlayer, out playerState) && playerState.AutoPlay && gameState.CurrentState != GameState.State.Ended)
				{
					CommandBase move;
					if (!CommandTriggerUtils.TryGetTriggerCommand(gameState, out move))
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
					CommandResultEvent commandResultEvent2;
					if (GameStateUtils.RegisterCommandResultEvent(gameState, currentState2, currentTurn2, move, out commandResultEvent2, playerState.Id == currentPlayer))
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