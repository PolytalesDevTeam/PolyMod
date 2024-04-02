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
            if(GameManager.Instance.isLevelLoaded)
            {
                if (GameManager.GameState.Settings.GameType == GameType.SinglePlayer || GameManager.GameState.Settings.GameType == GameType.PassAndPlay)
                {
                    return new PopupBase.PopupButtonData[]
                    {
                        new PopupBase.PopupButtonData(Localization.Get("buttons.back"), PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction) OnBackButtonClicked, -1, true, null),
                        new PopupBase.PopupButtonData("GET STARS", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction) OnGetStarsUiButtonClicked, -1, true, null),
                        new PopupBase.PopupButtonData("REVEAL MAP", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction) OnMapRevealButtonClicked, -1, true, null),
                    };
                }
            }
            return new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData(Localization.Get("buttons.back"), PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction) OnBackButtonClicked, -1, true, null)
            };

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

            void OnGetStarsUiButtonClicked(int buttonId, BaseEventData eventData)
            {
                GameManager.LocalPlayer.Currency += 1000;
                Console.Write("+1000 stars");
                isUIActive = false;
            }
        }
    }
}
