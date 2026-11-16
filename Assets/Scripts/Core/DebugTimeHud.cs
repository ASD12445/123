using Game.Player;
using UnityEngine;

namespace Game.Core
{
    public class DebugTimeHud : MonoBehaviour
    {
        private void OnGUI()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.WorldTime == null)
                return;

            var worldTime = gameManager.WorldTime;
            string phase = worldTime.IsNight ? "Night" : "Day";
            GUI.Label(new Rect(10f, 10f, 600f, 22f), $"Day {worldTime.DayCount}  {worldTime.GetTimeString()}  {phase}");
            GUI.Label(new Rect(10f, 32f, 700f, 22f), "WASD move | Shift sprint | Space jump | Tab inv | 1-8/scroll hotbar | F use | LMB harvest | Esc cursor");

            if (!string.IsNullOrEmpty(PlayerInteractor.HintText))
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
                style.normal.textColor = Color.white;

                float width = 520f;
                float x = (Screen.width - width) / 2f;
                float y = Screen.height * 0.62f;
                GUI.Label(new Rect(x, y, width, 30f), PlayerInteractor.HintText, style);

                if (PlayerInteractor.HintProgress >= 0f)
                {
                    float barWidth = 240f;
                    float barX = (Screen.width - barWidth) / 2f;
                    float barY = y + 34f;
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(new Rect(barX, barY, barWidth, 10f), Texture2D.whiteTexture);
                    GUI.color = Color.Lerp(Color.red, Color.green, PlayerInteractor.HintProgress);
                    GUI.DrawTexture(new Rect(barX, barY, barWidth * PlayerInteractor.HintProgress, 10f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
            }
        }
    }
}
