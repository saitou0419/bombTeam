using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2-1の表裏切り替えと解除判定を管理する。
/// 面の開始時に裏面をH型・I型から1つ抽選し、
/// 表面と選ばれた裏面の両方を解除したときにMainGameへ通知する。
/// </summary>
public class RandomSideTimingModule : MonoBehaviour
{
    [SerializeField] private MainGame mainGame;
    [SerializeField] private TimingModule front;
    [SerializeField] private SlideTimingModule[] backs;

    public UnityEvent onCleared;

    private SlideTimingModule selectedBack;
    private bool isCleared;

    public bool IsFrontSelected { get; private set; } = true;

    public void OnStageStarted(StageData stage)
    {
        RemoveListeners();
        selectedBack = null;
        isCleared = false;
        IsFrontSelected = true;

        if (mainGame == null || front == null ||
            backs == null || backs.Length != 2 ||
            backs[0] == null || backs[1] == null || backs[0] == backs[1])
        {
            Debug.LogError("2-1のMain Game、Front、Backs（H型とI型）を設定してください。",this);
            return;
        }

        selectedBack = backs[Random.Range(0, 2)];

        // 表だけ解除しても、MainGameのクリア判定はまだ行わない。
        front.SetReportToMainGame(false);

        if (front.onCleared == null) front.onCleared = new UnityEvent();
        if (selectedBack.onCleared == null) selectedBack.onCleared = new UnityEvent();

        front.onCleared.AddListener(CheckBothSidesCleared);
        selectedBack.onCleared.AddListener(CheckBothSidesCleared);

        ApplySideVisibility();
        front.ResetModule();
        selectedBack.ResetModule();
    }

    /// <summary>
    /// プレイ中に表面と裏面の表示を切り替える。
    /// 抽選結果と各面の解除状況は保持する。
    /// UnityのFlipButtonのOn Click()から呼び出す。
    /// </summary>
    public void ToggleSide()
    {
        if (selectedBack == null || mainGame == null ||
            mainGame.State != GameState.Playing) return;

        IsFrontSelected = !IsFrontSelected;
        ApplySideVisibility();
    }

    private void ApplySideVisibility()
    {
        front.gameObject.SetActive(IsFrontSelected);

        foreach (var back in backs)
        {
            back.gameObject.SetActive(!IsFrontSelected && back == selectedBack);
        }
    }

    private void CheckBothSidesCleared()
    {
        if (isCleared || selectedBack == null || mainGame == null ||
            mainGame.State != GameState.Playing) return;

        if (!front.IsCleared || !selectedBack.IsCleared) return;

        isCleared = true;
        mainGame.NotifyModuleCleared(ModuleId.Timing);
        onCleared?.Invoke();
    }

    private void RemoveListeners()
    {
        if (front != null)
            front.onCleared?.RemoveListener(CheckBothSidesCleared);

        if (selectedBack != null)
            selectedBack.onCleared?.RemoveListener(CheckBothSidesCleared);
    }

    private void OnDestroy()
    {
        RemoveListeners();
    }
}