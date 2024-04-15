﻿using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using static PopupBase;

namespace PolyMod
{
	internal static class UI
	{
		internal static bool active = false;
		internal static int width;
		internal static int height = 200;
		internal static int inputValue = 0;

		public static void Show()
		{
			width = 600;
			active = true;

			SearchFriendCodePopup polymodPopup = PopupManager.GetSearchFriendCodePopup();

			polymodPopup.Header = "PolyMod";
			polymodPopup.Description = "";

			polymodPopup.buttonData = CreatePopupButtonData();
			polymodPopup.Show(new Vector2(NativeHelpers.Screen().x * 0.5f, NativeHelpers.Screen().y * 0.5f));

			UINavigationManager.Select(polymodPopup.inputfield);
			polymodPopup.CurrentSelectable = polymodPopup.inputfield;
		}

		public static void OnInputChanged(SearchFriendCodePopup polymodPopup)
		{
			if (int.TryParse(polymodPopup.inputfield.text, out int _))
			{
				polymodPopup.Buttons[1].ButtonEnabled = (!string.IsNullOrEmpty(polymodPopup.inputfield.text) && polymodPopup.inputfield.text.Length <= 10);
				inputValue = int.Parse(polymodPopup.inputfield.text);
			}
			else
			{
				polymodPopup.Buttons[1].ButtonEnabled = false;
			}
		}

		public static PopupButtonData[] CreatePopupButtonData()
		{
			List<PopupButtonData> popupButtons = new()
			{
				new(Localization.Get("buttons.back"), PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnBackButtonClicked, -1, true, null)
			};

			if (GameManager.Instance.isLevelLoaded)
			{
				if (GameManager.GameState.Settings.GameType == GameType.PassAndPlay && GameManager.GameState.Settings.GameName.StartsWith(ReplayResumer.nameStart))
				{
					width += 250;
					popupButtons.Add(new PopupButtonData("BACK TO REPLAY", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnBackToReplayButtonClicked, -1, true, null));
				}
				if (GameManager.GameState.Settings.GameType == GameType.SinglePlayer || GameManager.GameState.Settings.GameType == GameType.PassAndPlay)
				{
					popupButtons.Add(new PopupButtonData("GET STARS", PopupButtonData.States.Disabled, (UIButtonBase.ButtonAction)OnGetStarsButtonClicked, -1, true, null));
					popupButtons.Add(new PopupButtonData("REVEAL MAP", PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnMapRevealButtonClicked, -1, true, null));
				}
				if (GameManager.Instance.client.IsReplay)
				{
					popupButtons.Add(new PopupButtonData("RESUME", PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnResumeButtonClicked, -1, true, null));
				}
			}
			else
			{
				popupButtons.Add(new PopupButtonData("CHANGE VERSION", PopupButtonData.States.Disabled, (UIButtonBase.ButtonAction)OnChangeVersionButtonClicked, -1, true, null));
			}

			return popupButtons.ToArray();

			void OnMapRevealButtonClicked(int buttonId, BaseEventData eventData)
			{
				for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
				{
					GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
				}
				MapRenderer.Current.Refresh(false);
				NotificationManager.Notify("Map has been revealed.", "POLYMOD", null, null);
				active = false;
			}

			void OnGetStarsButtonClicked(int buttonId, BaseEventData eventData)
			{
				GameManager.LocalPlayer.Currency += inputValue;
				NotificationManager.Notify($"{inputValue} stars has been added to player's currency amount.", "POLYMOD", null, null);
				active = false;
			}

			void OnResumeButtonClicked(int buttonId, BaseEventData eventData)
			{
				ReplayResumer.Resume();
				NotificationManager.Notify("Replay had been resumed.", "POLYMOD", null, null);
				active = false;
			}

			void OnChangeVersionButtonClicked(int buttonId, BaseEventData eventData)
			{
				if (inputValue >= 0)
				{
					Plugin.version = inputValue;
					NotificationManager.Notify($"Changed version to {Plugin.version}.", "POLYMOD", null, null);
				}
				else
				{
					NotificationManager.Notify("Cant set Game Version lower than 0.", "POLYMOD", null, null);
				}
				active = false;
			}

			void OnBackButtonClicked(int buttonId, BaseEventData eventData)
			{
				active = false;
			}

			void OnBackToReplayButtonClicked(int buttonId, BaseEventData eventData)
			{
				ReplayResumer.BackToReplay();
				active = false;
			}
		}
	}
}