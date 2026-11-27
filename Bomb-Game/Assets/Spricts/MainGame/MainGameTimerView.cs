using TMPro;
using UnityEngine;

public class MainGameTimerView : MonoBehaviour
{
    [SerializeField] private MainGame mainGame;
    [SerializeField] private TMP_Text timerText;

    private int displayedSeconds = -1;

    private void LateUpdate()
    {
        if (mainGame == null || timerText == null) return;

        if (mainGame.CurrentStage == null)
        {
            timerText.text = "";
            displayedSeconds = -1;
            return;
        }

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, mainGame.RemainingTime));
        if (seconds == displayedSeconds) return;

        displayedSeconds = seconds;
        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }
}