using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2-2の表面を管理する。3つの円がすべて停止したときに
/// 表面の解除イベントを発行する。
/// </summary>
public class NotchWheelModule : MonoBehaviour
{
    [SerializeField] private MainGame mainGame;
    [SerializeField] private ModuleNotchWheel[] wheels = new ModuleNotchWheel[3];
    [SerializeField] private UnityEvent onCleared;

    /// <summary>表面を解除済みかどうか。</summary>
    public bool IsCleared { get; private set; }

    /// <summary>円の回転と入力を受け付けられる状態かどうか。</summary>
    public bool CanOperate =>
        isActiveAndEnabled && !IsCleared &&
        mainGame != null && mainGame.State == GameState.Playing;

    /// <summary>
    /// テスト用にMainGameの開始イベントから呼べる入口。
    /// 表裏を接続した後は、2-2の親から初期化する。
    /// </summary>
    /// <param name="stage">開始した面の設定。</param>
    public void OnStageStarted(StageData stage)
    {
        if (stage != null && stage.stageId == "2-2")
        {
            ResetModule();
        }
    }

    /// <summary>表面の解除状態と、各円の角度・停止状態を初期化する。</summary>
    public void ResetModule()
    {
        IsCleared = false;

        foreach (var wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.ResetWheel(this);
            }
        }
    }

    /// <summary>
    /// 円の停止が成功したら解除条件を確認し、失敗したらミスを加算する。
    /// </summary>
    /// <param name="aligned">必要なくぼみがすべて黄色い線に合っていたか。</param>
    public void ReportPress(bool aligned)
    {
        if (!CanOperate) return;

        if (!aligned)
        {
            mainGame.NotifyModuleMistake();
            return;
        }

        if (wheels.Length != 3) return;

        foreach (var wheel in wheels)
        {
            if (wheel == null || !wheel.IsStopped) return;
        }

        IsCleared = true;
        onCleared?.Invoke();
    }
}