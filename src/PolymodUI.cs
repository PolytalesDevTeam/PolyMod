using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;
using static PopupBase;

namespace PolyMod
{
    internal class PolymodUI
    {
        internal static bool isUIActive = false;
        internal static int width = 600;
        internal static int height = 200;
        public static void OnKeysPressed() {

            isUIActive = true;

            BasicPopup modUi = PopupManager.GetBasicPopup();

            modUi.Header = "POLYMOD UI";
            modUi.Description = "Here u can do shit.";

            modUi.buttonData = CreatePopupButtonData();
            
            modUi.Show();
        }
        public static PopupButtonData[] CreatePopupButtonData()
        {
            List<PopupBase.PopupButtonData> popupButtons = new List<PopupBase.PopupButtonData>();
            popupButtons.Add(new PopupBase.PopupButtonData(Localization.Get("buttons.back"), PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnBackButtonClicked, -1, true, null));

            if (GameManager.Instance.isLevelLoaded)
            {
                if (GameManager.GameState.Settings.GameType == GameType.SinglePlayer || GameManager.GameState.Settings.GameType == GameType.PassAndPlay)
                {
                    popupButtons.Add(new PopupBase.PopupButtonData("GET STARS", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnGetStarsButtonClicked, -1, true, null));
                    popupButtons.Add(new PopupBase.PopupButtonData("REVEAL MAP", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnMapRevealButtonClicked, -1, true, null));
                }
                if (GameManager.Instance.client.IsReplay)
                {
                    popupButtons.Add(new PopupBase.PopupButtonData("RESUME", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnResumeButtonClicked, -1, true, null));
                }
            }
            else
            {
                popupButtons.Add(new PopupBase.PopupButtonData("CHANGE VERSION", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnChangeVersionButtonClicked, -1, true, null));
            }
            return popupButtons.ToArray();

            void OnBackButtonClicked(int buttonId, BaseEventData eventData)
            {
                isUIActive = false;
            }

            void OnMapRevealButtonClicked(int buttonId, BaseEventData eventData)
            {
                for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
                {
                    GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
                }
                MapRenderer.Current.Refresh(false);
                Console.Write("Map Revealed");
                isUIActive = false;
            }

            void OnGetStarsButtonClicked(int buttonId, BaseEventData eventData)
            {
                int starsAmount = 1000;
                GameManager.LocalPlayer.Currency += starsAmount;
                Console.Write($"{starsAmount} stars has been add to player's currency amount.");
                isUIActive = false;
            }

            void OnResumeButtonClicked(int buttonId, BaseEventData eventData)
            {
                ReplayResumer.Resume();
                Console.Write("Replay had been resumed.");
                isUIActive = false;
            }

            void OnChangeVersionButtonClicked(int buttonId, BaseEventData eventData)
            {
                if(Plugin.version > 0)
                {
                    --Plugin.version;
                    Console.Write($"Changed version to {Plugin.version}.");
                }
                else
                {
                    Console.Write("Cant set Game Version lower than 0.");
                }
                isUIActive = false;
            }
        }
    }
}
