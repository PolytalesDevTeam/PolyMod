﻿using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using static PopupBase;

namespace PolyMod
{
    internal static class PolymodUI
    {
        internal static bool isUIActive = false;
        internal static int width = 600;
        internal static int height = 200;
        internal static int inputValue = 0;

        public static void Show()
        {
            isUIActive = true;

            SearchFriendCodePopup polymodPopup = PopupManager.GetSearchFriendCodePopup();

            polymodPopup.Header = "POLYMOD UI";
            polymodPopup.Description = "Here u can do shit.";

            polymodPopup.buttonData = CreatePopupButtonData();
            polymodPopup.Show(new Vector2((float)NativeHelpers.Screen().x * 0.5f, (float)NativeHelpers.Screen().y * 0.5f));

            UINavigationManager.Select(polymodPopup.inputfield);
            polymodPopup.CurrentSelectable = polymodPopup.inputfield;
        }

        public static void OnInputChanged(SearchFriendCodePopup polymodPopup, string value)
        {
            if (int.TryParse(polymodPopup.inputfield.text, out int ignoreValue))
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
            List<PopupBase.PopupButtonData> popupButtons = new List<PopupBase.PopupButtonData>
            {
                new PopupBase.PopupButtonData(Localization.Get("buttons.back"), PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnBackButtonClicked, -1, true, null)
            };

            if (GameManager.Instance.isLevelLoaded)
            {
                if (GameManager.GameState.Settings.GameType == GameType.SinglePlayer || GameManager.GameState.Settings.GameType == GameType.PassAndPlay)
                {
                    popupButtons.Add(new PopupBase.PopupButtonData("GET STARS", PopupBase.PopupButtonData.States.Disabled, (UIButtonBase.ButtonAction)OnGetStarsButtonClicked, -1, true, null));
                    popupButtons.Add(new PopupBase.PopupButtonData("REVEAL MAP", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnMapRevealButtonClicked, -1, true, null));
                }
                if (GameManager.Instance.client.IsReplay)
                {
                    popupButtons.Add(new PopupBase.PopupButtonData("RESUME", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnResumeButtonClicked, -1, true, null));
                }
            }
            else
            {
                popupButtons.Add(new PopupBase.PopupButtonData("CHANGE VERSION", PopupBase.PopupButtonData.States.Disabled, (UIButtonBase.ButtonAction)OnChangeVersionButtonClicked, -1, true, null));
            }

            return popupButtons.ToArray();

            void OnMapRevealButtonClicked(int buttonId, BaseEventData eventData)
            {
                for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
                {
                    GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
                }
                MapRenderer.Current.Refresh(false);
                NotificationManager.Notify("Map has been revealed.", "Map Reveal", null, null);
                isUIActive = false;
            }

            void OnGetStarsButtonClicked(int buttonId, BaseEventData eventData)
            {
                GameManager.LocalPlayer.Currency += inputValue;
                NotificationManager.Notify($"{inputValue} stars has been added to player's currency amount.", "Star Hack", null, null);
                isUIActive = false;
            }

            void OnResumeButtonClicked(int buttonId, BaseEventData eventData)
            {
                ReplayResumer.Resume();
                NotificationManager.Notify("Replay had been resumed.", "Replay Resume", null, null);
                isUIActive = false;
            }

            void OnChangeVersionButtonClicked(int buttonId, BaseEventData eventData)
            {
                if (inputValue >= 0)
                {
                    Plugin.version = inputValue;
                    Console.Write($"Changed version to {Plugin.version}.");
                    NotificationManager.Notify($"Changed version to {Plugin.version}.", "Version Changer", null, null);
                }
                else
                {
                    NotificationManager.Notify("Cant set Game Version lower than 0.", "Version Changer", null, null);
                }
                isUIActive = false;
            }

            void OnBackButtonClicked(int buttonId, BaseEventData eventData)
            {
                isUIActive = false;
            }
        }
    }
}