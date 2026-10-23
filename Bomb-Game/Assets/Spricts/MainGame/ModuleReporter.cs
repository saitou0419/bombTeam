using UnityEngine;

/// <summary>
/// モジュール関係の通知をMainGameへ通知する中継コンポーネント
/// UnityEventからReportCleared / ReportFailed を呼ぶだけで通知可能
/// モジュールのスクリプトから直接MainGame.NotifyModuleClearedを呼んでも良い
/// </summary>
public class ModuleReporter : MonoBehaviour
{
    [SerializeField] private MainGame mainGame;
    [SerializeField] private ModuleId moduleId = ModuleId.None;

    ///<summary>解除成功通知</summary>
    public void ReportCleared()
    {
        if (!CanReport()) return;
        mainGame.NotifyModuleCleard(moduleId);
    }

    ///<summary>誤操作通知</summary>
    public void ReportFailed()
    {
        if (mainGame == null)
        {
            Debug.LogError($"[ModuleReporter] {name}:MainGameが未設定です");
            return;
        }
        mainGame.NotifyModuleFailed();
    }

    private bool CanReport()
    {
        if (mainGame == null)
        {
            Debug.LogError($"[ModuleReporter] {name}:MainGameが未設定です");
            return false;
        }
        if (moduleId == ModuleId.None)
        {
            Debug.LogError($"[ModuleReporter] {name}:ModuleIdがNoneのままです");
            return false;
        }
        return true;
    }
}
