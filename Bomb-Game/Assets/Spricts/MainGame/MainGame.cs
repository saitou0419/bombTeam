using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ゲーム進行を管理する
/// 状態管理/制限時間/モジュール解除数/クリア・失敗判定/リザルト作成(値)/リセットを担当
/// モジュールの操作はここではない
/// </summary>

public class MainGame : MonoBehaviour
{
    [Header("ステージ設定")]
    [Tooltip("プレイ可能なステージ")]
    [SerializeField] private StageData[] stages;

    [Header("仮の自動開始 ステージ選択が完成したらoffに")]
    [SerializeField] private bool autoStageOnPlay = true;
    [Tooltip("自動開始するステージのstages配列内の番号")]
    [SerializeField] private int autoStartStageIndex = 0;

    [Header("スコア(計算は未定)")]
    [Tooltip("ステージ開始時のスコア")]
    [SerializeField] private int initialScore = 0;

    [Header("イベント関係")]
    public UnityEvent<StageData> onStageStarted;
    public UnityEvent onStageCleared;
    public UnityEvent onStageFailed;
    public UnityEvent<ResultData> onResultReady;

    ///<summary>内部データ ResetStageで初期化</summary>
    private StageData currentStage;
    private GameState currentGameState = GameState.Ready;
    private float remainingTIme;
    private int totalModuleCount;
    private int currentScore;
    private ResultData resultData;
    private bool hasResult;

    private readonly HashSet<ModuleId> requiredModules = new HashSet<ModuleId>();
    private readonly HashSet<ModuleId> clearedModules = new HashSet<ModuleId>();

    ///<summary>外部から読み取り用</summary>
    public GameState State => currentGameState;
    public StageData CurrentStage => currentStage;
    public float RemainingTime => remainingTIme;
    public int ClearedModuleCount => clearedModules.Count;
    public int TotalModuleCount => totalModuleCount;
    public int CurrentScore => currentScore;
    public bool HasResult => hasResult;
    public ResultData Result => resultData;
    public IReadOnlyCollection<ModuleId> RequiredModules => requiredModules;

    private void Start()
    {
        if (autoStageOnPlay)
        {
            SelectStage(autoStartStageIndex);
        }
    }

    private void Update()
    {
        if (currentGameState != GameState.Playing) return;
        UpdateTimer();
    }

    //ステージ選択・開始
    /// <summary>stages配列の番号でステージを選んで開始</summary>
    public void SelectStage(int stageIndex)
    {
        if (stages == null || stageIndex < 0 || stageIndex >= stages.Length)
        {
            Debug.LogError($"[MainGame] ステージ番号{stageIndex}はstagesに存在しません");
            return;
        }
        StartStage(stages[stageIndex]);
    }

    ///<summary>ステージ識別子でステージを選んで開始する</summary>
    public void SelectStageById(string stageId)
    {
        if (stages != null)
        {
            foreach (var stage in stages)
            {
                if (stage != null && stage.stageId == stageId)
                {
                    StartStage(stage);
                    return;
                }
            }
        }
        Debug.LogError($"[MainGame] ステージId'{stageId}'はstagesに存在しません");
    }

    ///<summary>現在のステージを最初からやりなおす</summary>
    public void RestartStage()
    {
        if (currentStage == null)
        {
            Debug.LogError("[MainGame] 再開始するステージがありません");
            return;
        }
        StartStage(currentStage);
    }

    ///<summary>ステージ開始処理 前回の状態を必ずリセット</summary>
    public void StartStage(StageData stage)
    {
        if (!ValidateStage(stage)) return;

        ResetStage();
        currentStage = stage;
        InitializeStage(stage);

        SetState(GameState.Playing);
        onStageStarted?.Invoke(stage);
    }

    ///<summary>開始前にステージ設定が正しいか確認する</summary>
    private bool ValidateStage(StageData stage)
    {
        if (stage == null)
        {
            Debug.LogError("[MainGame] StageDataがnullです");
            return false;
        }
        if (stage.timeLimit <= 0f)
        {
            Debug.LogError($"[MainGame] ステージ'{stage.stageId}'の制限時間が未設定(0以下)です StageDataで設定してください");
            return false;
        }
        if (stage.requiredModules == null || stage.requiredModules.Count == 0)
        {
            Debug.LogError($"[MainGame] ステージ'{stage.stageId}'のrequiredModules が空です");
            return false;
        }
        return true;
    }

    ///<summary>ステージ特有の初期値を設定</summary>
    private void InitializeStage(StageData stage)
    {
        remainingTIme = stage.timeLimit;
        currentScore = initialScore;

        requiredModules.Clear();
        foreach (var id in stage.requiredModules)
        {
            if (id == ModuleId.None)
            {
                Debug.LogWarning($"[MainGame] ステージ'{stage.stageId}'にModuleId.Noneが含まれています 無視する");
                continue;
            }
            if (!requiredModules.Add(id))
            {
                Debug.LogWarning($"[MainGame] ステージ'{stage.stageId}'に{id}が重複しています 1つとして扱う");
            }
        }
        totalModuleCount = requiredModules.Count;
    }

    //制限時間
    private void UpdateTimer()
    {
        remainingTIme -= Time.deltaTime;
        if (remainingTIme <= 0f)
        {
            remainingTIme = 0f;
            HandleStageFailure();
        }
    }

    //モジュールからの通知

    ///<summary>モジュール解除通知</summary>
    public void NotifyModuleCleared(ModuleId id)
    {
        if (currentGameState != GameState.Playing) return;

        if (!requiredModules.Contains(id))
        {
            Debug.LogWarning($"[MainGame] このステージに存在しないモジュール{id}から解除通知が届きました 無視");
            return;
        }

        if (!clearedModules.Add(id)) return;

        if (CheakAllModulesCleared())
        {
            HandleStageClear();
        }
    }

    ///<summary>誤操作 即失敗</summary>
    public void NotifyModuleFailed()
    {
        if (currentGameState != GameState.Playing) return;
        HandleStageFailure();
    }

    private bool CheakAllModulesCleared()
    {
        return totalModuleCount > 0 && clearedModules.Count >= totalModuleCount;
    }

    //スコア
    ///<summary>スコアを加算</summary>
    public void AddScore(int amount)
    {
        if (currentGameState != GameState.Playing) return;
        currentScore += amount;
    }

    //クリア・失敗判定
    private void HandleStageClear() 
    {
        if (currentGameState != GameState.Playing) return;

        SetState(GameState.Cleared);
        onStageCleared?.Invoke();
        FinalizeResult(true);
        ShowResult();
    }

    private void HandleStageFailure()
    {
        if (currentGameState != GameState.Playing) return;

        SetState(GameState.Failed);
        onStageFailed?.Invoke();
        FinalizeResult(false);
        ShowResult();
    }

    ///<summary>リザルト情報を確定</summary>
    private void FinalizeResult(bool isCleared)
    {
        resultData = new ResultData
        {
            stageId = currentStage != null ? currentStage.stageId : string.Empty,
            isCleared = isCleared,
            remainingTime = remainingTIme,
            score = currentScore
        };
        hasResult = true;
        ResultHolder.Set(resultData);
    }

    /// <summary>Result状態へ移行、UI側へ結果を通知</summary>
    private void ShowResult()
    {
        SetState(GameState.Result);
        onResultReady?.Invoke(resultData);
    }

    private void ResetStage()
    {
        clearedModules.Clear();
        requiredModules.Clear();
        totalModuleCount = 0;
        remainingTIme = 0f;
        currentScore = 0;
        resultData = default;
        hasResult = false;
        ResultHolder.Clear();
        currentStage = null;
        currentGameState = GameState.Ready;
    }

    private void SetState(GameState next)
    {
        currentGameState = next;
    }
}
