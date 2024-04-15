using System.Diagnostics;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase;
using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;

namespace PolyMod
{
	internal class Patcher
	{
		// Bot mode code
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateCustomGameModeList))]
		private static bool GameSetupScreen_CreateCustomGameModeList(ref GameSetupScreen __instance, ref UIHorizontalList __result)
		{
			string[] array = new string[]
			{
				Localization.Get(GameModeUtils.GetTitle(GameMode.Perfection)),
				Localization.Get(GameModeUtils.GetTitle(GameMode.Domination)),
				Localization.Get(GameModeUtils.GetTitle(GameMode.Sandbox)),
				Localization.Get(GameModeUtils.GetTitle((GameMode)BotGame.bot)),
			};
			__result = __instance.CreateHorizontalList("gamesettings.mode", array, new Action<int>(__instance.OnCustomGameModeChanged), __instance.GetCustomGameModeIndexFromSettings(), null, -1, null);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeUtils), nameof(GameModeUtils.GetTitle))]
		private static bool GameModeUtils_GetTitle(GameMode gameMode, ref string __result)
		{
			if (gameMode == (GameMode)BotGame.bot)
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
			if (gameMode == (GameMode)BotGame.bot)
			{
				__result = "gamemode.bot.description";
				return false;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.GetCustomGameModeIndexFromSettings))]
		private static bool GameSetupScreen_GetCustomGameModeIndexFromSettings(ref GameSetupScreen __instance, ref int __result)
		{
			if (GameManager.PreliminaryGameSettings.RulesGameMode == (GameMode)BotGame.bot)
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
				GameManager.PreliminaryGameSettings.RulesGameMode = (GameMode)BotGame.bot;
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
			bool flag = GameManager.PreliminaryGameSettings.BaseGameMode == GameMode.Custom;
			bool flag2 = flag && GameManager.PreliminaryGameSettings.RulesGameMode != GameMode.Domination && GameManager.PreliminaryGameSettings.RulesGameMode != (GameMode)BotGame.bot;
			bool flag4 = GameManager.PreliminaryGameSettings.RulesGameMode != (GameMode)BotGame.bot;
			int num = flag ? MapDataExtensions.GetMaximumOpponentCountForMapSize(GameManager.PreliminaryGameSettings.MapSize, GameManager.PreliminaryGameSettings.mapPreset) : GameManager.GetMaxOpponents();
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
			if (gameMode == (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode == (GameMode)BotGame.bot)
			{
				GameManager.instance.OnFinishedProcessingActions();
				Log.Info("Made move");
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartMatchReaction), nameof(StartMatchReaction.DoWelcomeCinematic))]
		private static bool StartMatchReaction_DoWelcomeCinematic(ref StartMatchReaction __instance, Action onComplete)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			__instance.StartGame(onComplete);
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientBase), nameof(ClientBase.IsPlayerLocal))]
		public static bool ClientBase_IsPlayerLocal(ref bool __result, byte playerId)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode == (GameMode)BotGame.bot && BotGame.unview)
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return;
			}
			GameManager.Client.ActionManager.isRecapping = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.Execute))]
		public static bool StartTurnReaction_Execute()
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			BotGame.localClient = GameManager.Client as LocalClient;
			if (BotGame.localClient == null)
			{
				return true;
			}
			// Replace the client (temporarily)
			GameManager.instance.client = new ReplayClient();
			GameManager.Client.currentGameState = BotGame.localClient.GameState;
			GameManager.Client.CreateOrResetActionManager(BotGame.localClient.lastSeenCommand);
			GameManager.Client.ActionManager.isRecapping = true;
			LevelManager.GetClientInteraction().ClearSelection(); // Clear selection circles.
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(StartTurnReaction), nameof(StartTurnReaction.DoStartTurnNotification))]
		public static bool StartTurnReaction_DoStartTurnNotification()
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
		public static bool HudScreen_OnMatchStart(ref HudScreen __instance)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			if (BotGame.localClient != null)
			{ // Repair the client as soon as possible
				GameManager.instance.client = BotGame.localClient;
				BotGame.localClient = null;
			}
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(TaskCompletedReaction), nameof(TaskCompletedReaction.Execute))]
		public static bool TaskCompletedReaction_Execute(ref TaskCompletedReaction __instance, ref byte __state)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return;
			}
			__instance.action.PlayerId = __state;
		}

		// Patch multiple classes with the same method
		[HarmonyPrefix]
		[HarmonyPatch(typeof(MeetReaction), nameof(MeetReaction.Execute))]
		[HarmonyPatch(typeof(EnableTaskReaction), nameof(EnableTaskReaction.Execute))]
		[HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
		[HarmonyPatch(typeof(InfiltrationRewardReaction), nameof(InfiltrationRewardReaction.Execute))]
		[HarmonyPatch(typeof(EstablishEmbassyReaction), nameof(EstablishEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(DestroyEmbassyReaction), nameof(DestroyEmbassyReaction.Execute))]
		[HarmonyPatch(typeof(ReceiveDiplomacyMessageReaction), nameof(ReceiveDiplomacyMessageReaction.Execute))]
		public static bool Patch_Execute(ReactionBase __instance)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			BotGame.unview = true;
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				if (BotGame.unview)
				{
					DebugConsole.Write("Uh, what!?");
				}
				return;
			}
			BotGame.unview = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientActionManager), nameof(ClientActionManager.Update))]
		public static void Patch_Update()
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				if (BotGame.unview)
				{
					DebugConsole.Write("Uh, what!?");
				}
				if (BotGame.localClient != null)
				{
					DebugConsole.Write("Sorry, what!?");
				}
				return;
			}
			if (BotGame.localClient != null)
			{
				GameManager.instance.client = BotGame.localClient;
				BotGame.localClient = null;
			}
			BotGame.unview = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(ClientBase), nameof(ClientBase.CreateSession))]
		public static void ClientBase_CreateSession(ref Il2CppSystem.Threading.Tasks.Task<CreateSessionResult> __result, ref ClientBase __instance, GameSettings settings, List<PlayerState> players)
		{
			if (__instance.clientType != ClientBase.ClientType.Local || GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
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
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			__result = __instance.GameState.PlayerStates[__instance.GameState.CurrentPlayerIndex];
			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AI), nameof(AI.GetGameProgress))]
		public static bool AI_GetGameProgress(GameState gameState, PlayerState winningPlayer, ref float __result, ref AI __instance)
		{
			if (GameManager.GameState.Settings.RulesGameMode != (GameMode)BotGame.bot)
			{
				return true;
			}
			float num = (float)winningPlayer.cities / Math.Max(0.1f, (float)MapDataExtensions.CountCities(gameState));
			float num2 = gameState.CurrentTurn / (float)gameState.Settings.rules.TurnLimit;
			__result = Math.Max(num, num2);
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadLevel))]
		public static void GameManager_LoadLevel(ref GameManager __instance)
		{
			GameManager.debugAutoPlayLocalPlayer = GameManager.Client.GameState.Settings.RulesGameMode == (GameMode)BotGame.bot;
		}

		// Other code
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameStateUtils), nameof(GameStateUtils.GetRandomPickableTribe), new System.Type[] { typeof(GameState) })]
		public static bool GameStateUtils_GetRandomPickableTribe(GameState gameState)
		{
			gameState.Version = Plugin.version;
			Plugin.version = 104; //will be changed in next commit
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update(GameManager __instance)
		{
			Plugin.Update();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			MapLoader.PreGenerate(ref state, ref settings);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate_(ref GameState state)
		{
			MapLoader.PostGenerate(ref state);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GeneratePlayerCapitalPositions))]
		private static void MapGenerator_GeneratePlayerCapitalPositions(ref Il2CppSystem.Collections.Generic.List<int> __result, int width, int playerCount)
		{
			__result = MapLoader.GetCapitals(__result, width, playerCount);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GenerateShoreLines))]
		public static bool MapDataExtensions_GenerateShoreLines(MapData map)
		{
			int width = (int)map.Width;
			int num = width * (int)(map.Height - 1);
			for (int i = 0; i < map.Tiles.Length; i++)
			{
				map.Tiles[i].shoreLines = TileData.ShorelineFlag.None;
			}
			return false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetMaxOpponents))]
		private static void GameManager_GetMaxOpponents(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS - 1;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GetMaximumOpponentCountForMapSize))]
		private static void MapDataExtensions_GetMaximumOpponentCountForMapSize(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.GetUnlockedTribeCount))]
		private static void PurchaseManager_GetUnlockedTribeCount(ref int __result)
		{
			__result = Plugin.MAP_MAX_PLAYERS + 2;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.IsTribeUnlocked))]
		private static void PurchaseManager_IsTribeUnlocked(ref bool __result, TribeData.Type type)
		{
			__result = (int)type >= Plugin.AUTOIDX_STARTS_FROM || __result;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
		private static void GameLogicData_Parse(JObject rootObject)
		{
			ModLoader.Init(rootObject);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = Plugin.CAMERA_MAXZOOM_CONSTANT;
			//CameraController.Instance.minZoom = Plugin.CAMERA_MINZOOM_CONSTANT;
			CameraController.Instance.techViewBounds = new(
				new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT), CameraController.Instance.techViewBounds.size
			);
			GameObject.Find("TechViewWorldSpace").transform.position = new(Plugin.CAMERA_MAXZOOM_CONSTANT, Plugin.CAMERA_MAXZOOM_CONSTANT);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetTileSpriteAddress), new Type[] { typeof(Polytopia.Data.TerrainData.Type), typeof(string) })]
		private static void SpriteData_GetTileSpriteAddress(ref SpriteAddress __result, Polytopia.Data.TerrainData.Type terrain, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<Polytopia.Data.TerrainData.Type>.GetName(terrain), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetResourceSpriteAddress), new Type[] { typeof(ResourceData.Type), typeof(string) })]
		private static void SpriteData_GetResourceSpriteAddress(ref SpriteAddress __result, ResourceData.Type type, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<ResourceData.Type>.GetName(type), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetBuildingSpriteAddress), new Type[] { typeof(ImprovementData.Type), typeof(string) })]
		private static void SpriteData_GetBuildingSpriteAddress(ref SpriteAddress __result, ImprovementData.Type type, string skinId)
		{
			__result = ModLoader.GetSprite(__result, EnumCache<ImprovementData.Type>.GetName(type), skinId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetUnitIconAddress))]
		private static void SpriteData_GetUnitIconAddress(ref SpriteAddress __result, UnitData.Type type)
		{
			__result = ModLoader.GetSprite(__result, "icon", EnumCache<UnitData.Type>.GetName(type));
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(int) })]
		private static void SpriteData_GetHeadSpriteAddress_1(ref SpriteAddress __result, int tribe)
		{
			__result = ModLoader.GetSprite(__result, "head", $"{tribe}");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHeadSpriteAddress), new Type[] { typeof(string) })]
		private static void SpriteData_GetHeadSpriteAddress_2(ref SpriteAddress __result, string specialId)
		{
			__result = ModLoader.GetSprite(__result, "head", specialId);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetAvatarPartSpriteAddress))]
		private static void SpriteData_GetAvatarPartSpriteAddress(ref SpriteAddress __result, string sprite)
		{
			__result = ModLoader.GetSprite(__result, sprite, "");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(SpriteData), nameof(SpriteData.GetHouseAddresses))]
		private static void SpriteData_GetHouseAddresses(ref Il2CppReferenceArray<SpriteAddress> __result, int type, string styleId, SkinType skinType)
		{
			List<SpriteAddress> sprites = new()
			{
				ModLoader.GetSprite(__result[0], "house", styleId, type)
			};
			if (skinType != SkinType.Default)
			{
				sprites.Add(ModLoader.GetSprite(__result[1], "house", EnumCache<SkinType>.GetName(skinType), type));
			}
			__result = sprites.ToArray();
		}


		[HarmonyPrefix]
		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.SetAmbienceClimate))]
		private static void AudioManager_SetAmbienceClimatePrefix(ref int climate)
		{
			if (climate > 16)
			{
				climate = 1;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen.MapPresetDataSource), nameof(GameSetupScreen.MapPresetDataSource.GetData))]
		private static void GameSetupScreen_MapPresetDataSource_GetData(ref Il2CppStructArray<MapPreset> __result)
		{
			List<MapPreset> presets = __result.ToList();
			presets.Add((MapPreset)500);
			__result = presets.ToArray();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnMapPresetChanged))]
		private static void GameSetupScreen_OnMapPresetChanged(GameSetupScreen __instance, int index)
		{
			MapLoader.map = null;
			MapPreset[] array = GameSetupScreen.MapPresetDataSource.GetData(GameManager.PreliminaryGameSettings.GameType == GameType.Matchmaking);

			if (MapLoader.isListInstantiated)
			{
				UnityEngine.Object.Destroy(MapLoader.customMapsList.gameObject);
				MapLoader.isListInstantiated = false;
			}

			if ((int)array[index] == 500)
			{
				string[] maps = Directory.GetFiles(Plugin.MAPS_PATH, "*.json");
				if (maps.Length != 0)
				{
					GameManager.PreliminaryGameSettings.mapPreset = array[index];
					MapLoader.customMapsList = __instance.CreateHorizontalList("Maps", maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToArray(), new Action<int>(MapLoader.OnCustomMapChanged), 0, null, 500);
					MapLoader.isListInstantiated = true;
				}
				else
				{
					GameManager.PreliminaryGameSettings.mapPreset = MapPreset.None;
					NotificationManager.Notify(Localization.Get("No maps found"), Localization.Get("gamesettings.notavailable"), null, null);
				}
			}

			__instance.UpdateOpponentList();
			GameManager.PreliminaryGameSettings.SaveToDisk();
			__instance.RefreshInfo();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnStartGameClicked))]
		private static void GameSetupScreen_OnStartGameClicked()
		{
			MapLoader.isListInstantiated = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapPresetExtensions), nameof(MapPresetExtensions.GetLocalizationName))]
		private static void MapPresetExtensions_GetLocalizationName(ref string __result, MapPreset mapPreset)
		{
			if (mapPreset == (MapPreset)500)
			{
				__result = "Custom";
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.RedrawScreen))]
		private static void GameSetupScreen_RedrawScreen()
		{
			MapLoader.map = null;
		}

        //do not touch TechItem patches, they prevent game from crashing when custom tribe(idfk how this works)

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechItem), nameof(TechItem.GetUnlockItems))]
        private static void TechItem_GetUnlockItems(TechData data, PlayerState playerState, bool onlyPickFirstItem = false)
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(TechItem), nameof(TechItem.SetupComplete))]
        private static void TechItem_SetupComplete()
        {
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BasicPopup), nameof(BasicPopup.Update))]
        private static void BasicPopup_Update(BasicPopup __instance)
        {
			if (PolymodUI.isUIActive)
			{
                __instance.rectTransform.SetWidth(PolymodUI.width);
                __instance.rectTransform.SetHeight(PolymodUI.height);
            }
        }

		//shitty patch I know, should be refactored after
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PopupButtonContainer), nameof(PopupButtonContainer.SetButtonData))]
        private static bool PopupButtonContainer_SetButtonData(PopupButtonContainer __instance, Il2CppReferenceArray<PopupBase.PopupButtonData> buttonData)
        {
            int num = buttonData.Length;
            __instance.buttons = new UITextButton[num];
            for (int i = 0; i < num; i++)
            {
                UITextButton uitextButton = UnityEngine.Object.Instantiate<UITextButton>(__instance.buttonPrefab, __instance.transform);
                
                Vector2 vector = new Vector2((num == 1) ? 0.5f : (((float)i / ((float)num - 1.0f))), 0.5f); // literally one line i have to patch here
                uitextButton.rectTransform.anchorMin = vector;
                uitextButton.rectTransform.anchorMax = vector;
                uitextButton.rectTransform.pivot = vector;
                uitextButton.rectTransform.anchoredPosition = Vector2.zero;
                uitextButton.Key = buttonData[i].text;
                uitextButton.name = string.Format("PopupButton_{0}", uitextButton.text);
                uitextButton.id = buttonData[i].id;
                if (buttonData[i].closesPopup)
                {
                    uitextButton.OnClicked += __instance.hideCallback;
                }
                if (buttonData[i].callback != null)
                {
                    uitextButton.OnClicked += buttonData[i].callback;
                }
                if (buttonData[i].customColorStates != null)
                {
                    uitextButton.BgColorStates = buttonData[i].customColorStates;
                }
                __instance.buttons[i] = uitextButton;
                if (buttonData[i].state == PopupBase.PopupButtonData.States.Selected)
                {
                    __instance.startSelection = i;
                }
                else if (buttonData[i].state == PopupBase.PopupButtonData.States.Disabled)
                {
                    uitextButton.ButtonEnabled = false;
                }
                __instance.buttons[i].AnimationsEnabled = __instance.buttonAnimationsEnabled;
            }
            if (num >= 2 && buttonData[0].customColorStates == null)
            {
                __instance.buttons[0].LabelColorStates = new UIButtonBase.ColorStates(__instance.leftButtonLabelColors);
                __instance.buttons[0].BgColorStates = new UIButtonBase.ColorStates(__instance.leftButtonBgColors);
            }
            if (__instance.startSelection >= 0)
            {
                PolytopiaInput.Omnicursor.AffixToUIElement(__instance.buttons[__instance.startSelection].GetComponent<RectTransform>());
            }
            __instance.gameObject.SetActive(true);

            return false;
        }
    }
}
