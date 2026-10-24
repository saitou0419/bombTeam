using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ボタンモジュール本体
/// 赤いボタンを左クリックで緑にし、全ボタンが緑になったらMainGameに解除成功を伝える
/// </summary>
public class ButtonModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("このモジュールのボタン")]
    [SerializeField] private ModuleButton[] buttons;

    [Header("初期配置のランダム設定")]
    [Tooltip("オンなら、リセットのたびに赤・緑をランダムに決める、オフなら各ボタンのInitialStateを使う")]
    [SerializeField] private bool randomizeOnReset = true;
    [Tooltip("各ボタンが緑になる確率(0〜1) 仮の値であり正式仕様ではない")]
    [Range(0f, 1f)]
    [SerializeField] private float greenProbability = 0.5f;

    [Header("演出用イベント(音やエフェクト用、任意)")]
    public UnityEvent onCleared;

    private bool isCleared;

    private void Reset()
    {
        //コンポーネント追加時、自動でボタンを拾う
        buttons = GetComponentsInChildren<ModuleButton>();
    }

    private void Awake()
    {
        if (buttons == null) return;
        foreach (var b in buttons)
        {
            if (b != null) b.Initialize(this);
        }

        // MainGame のイベントを接続していなくても、Play開始時に初期配置を決める
        ResetModule();
    }

    ///<summary>
    ///MainGame.onStageStartedから呼ぶ用
    ///ステージ開始時にモジュールを初期状態へ
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        ResetModule();
    }

    ///<summary>モジュールの状態リセット</summary>
    public void ResetModule()
    {
        isCleared = false;

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("[ButtonModule] buttons が空です");
            return;
        }

        if (randomizeOnReset)
        {
            RandomizeButtons();
        }
        else
        {
            foreach (var b in buttons)
            {
                if (b != null) b.ResetButton();
            }
        }

        if (CheckAllGreen())
        {
            Debug.LogWarning("[ButtonModule] 初期状態で赤いボタンが1つもありません 解除できないモジュールになります");
        }
    }

    ///<summary>各ボタンを確率で緑にする、赤が1つも残らなかった場合は1つだけ赤にする</summary>
    private void RandomizeButtons()
    {
        foreach (var b in buttons)
        {
            if (b == null) continue;
            bool green = Random.value < greenProbability;
            b.SetState(green ? ButtonColorState.Green : ButtonColorState.Red);
        }

        //全部緑になった時用
        if (CheckAllGreen())
        {
            ModuleButton pick = PickRandomVaildButton();
            if (pick != null) pick.SetState(ButtonColorState.Red);
        }
    }

    private ModuleButton PickRandomVaildButton()
    {
        int start = Random.Range(0, buttons.Length);
        for (int i = 0;i < buttons.Length;i++)
        {
            ModuleButton b = buttons[(start + i) % buttons.Length];
            if (b != null) return b;
        }
        return null;
    }

    ///<summary>ModuleButtonからクリックを受け取る</summary>
    public void OnButtonClicked(ModuleButton clicked)
    {
        if (isCleared) return;

        //Playing中のみ操作受付
        if (mainGame == null)
        {
            Debug.LogError("[ButtonModule] MainGameが未設定です");
            return;
        }
        if (mainGame.State != GameState.Playing) return;

        if (clicked.IsGreen) return;

        //緑ボタンは一回放置
        if (clicked.IsGreen) return;

        clicked.SetState(ButtonColorState.Green);
        
        if (CheckAllGreen())
        {
            HandleModuleCleared();
        }
    }

    private bool CheckAllGreen()
    {
        foreach (var b in buttons)
        {
            if (b != null && !b.IsGreen) return false;
        }
        return true;
    }

    private void HandleModuleCleared()
    {
        isCleared = true;
        onCleared?.Invoke();
        mainGame.NotifyModuleCleard(ModuleId.Button);
    }
}
