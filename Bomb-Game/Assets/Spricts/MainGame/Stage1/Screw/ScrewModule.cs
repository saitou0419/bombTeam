using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>ネジを外すためにマウスを動かす方向。</summary>
public enum ScrewRemoveDirection
{
    Left,
    Right,
    Up,
    Down
}


/// <summary>
/// ネジモジュール本体
/// プラス/マイナスの切替と全ネジが外れたかの判定、MainGameへの解除通知を行う
/// </summary>
public class ScrewModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("このモジュールのネジ")]
    [SerializeField] private ModuleScrew[] screws;

    [Header("ドライバー(プラス/マイナス")]
    [Tooltip("ステージ開始時のドライバーの種類")]
    [SerializeField] private ScrewType initialMode = ScrewType.Plus;
    [Tooltip("今どちらのドライバーか色で表示(任意)")]
    [SerializeField] private Graphic modeIndicator;

    [Header("ネジを外す動作")]
    [Tooltip("マウスを動かす方向")]
    [SerializeField] private ScrewRemoveDirection removeDirection = ScrewRemoveDirection.Left;
    [Tooltip("外れるまでに動かす距離")]
    [SerializeField] private float dragDistance = 200f;
    [Tooltip("外れる直前までにネジが回って見える角度")]
    [SerializeField] private float visualRotationDegrees = 360f;

    [Header("初期配置のランダム設定")]
    [Tooltip("オンならリセットの度にプラス/マイナスをランダムに決める、オフなら書くネジのInitialTypeを使う")]
    [SerializeField] private bool randomizeOnReset = true;
    [Tooltip("各ネジがプラスになる確率(0〜1) 仮の値であり正式仕様ではない")]
    [Range(0f, 1f)]
    [SerializeField] private float plusProbability = 0.5f;

    [Header("見分け用の色(Sprite届くまでの仮)")]
    [SerializeField] private Color plusColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color minusColor = new Color(1f, 0.95f, 0.2f);

    [Header("演出用イベント(音やエフェクト用、任意)")]
    public UnityEvent onCleared;

    private ScrewType currentMode;
    private bool isCleared;
    private bool initialized;

    public float DragDistance => Mathf.Max(1f, dragDistance);
    public float VisualRotationDegrees => visualRotationDegrees;
    public ScrewType CurrentMode => currentMode;

    /// <summary>外す方向の単位ベクトル(画面座標)。</summary>
    public Vector2 RemoveDirection
    {
        get
        {
            switch (removeDirection)
            {
                case ScrewRemoveDirection.Right: return Vector2.right;
                case ScrewRemoveDirection.Up: return Vector2.up;
                case ScrewRemoveDirection.Down: return Vector2.down;
                default: return Vector2.left;
            }
        }
    }

    private void Reset()
    {
        screws = GetComponentsInChildren<ModuleScrew>();
    }

    private void Awake()
    {
        EnsureInitialized();
        ResetModule();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (screws == null) return;
        foreach (var s in screws)
        {
            if (s != null) s.Initialize(this);
        }
    }

    ///<summary>
    ///MainGame.onStageStartedから呼ぶ用
    ///ステージ開始時にモジュールを初期状態へ
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        EnsureInitialized();
        ResetModule();
    }

    ///<summary>モジュールの状態リセット</summary>
    public void ResetModule()
    {
        isCleared = false;
        SetMode(initialMode);

        if (screws == null || screws.Length == 0)
        {
            Debug.LogWarning("[ScrewModule] screwsが空です");
            return;
        }

        foreach (var s in screws)
        {
            if (s == null) continue;
            s.ResetScrew();
            if (randomizeOnReset)
            {
                s.SetType(Random.value < plusProbability ? ScrewType.Plus : ScrewType.Minus);
            }
        }
    }

    //プラスマイナスの切替
    ///<summary>
    ///切替ボタンから呼ぶ
    ///押すたびに交互に変わる
    /// </summary>
    public void ToggleMode()
    {
        if (isCleared) return;
        if (mainGame == null || mainGame.State != GameState.Playing) return;

        SetMode(currentMode == ScrewType.Plus ? ScrewType.Minus : ScrewType.Plus);
    }

    private void SetMode(ScrewType mode)
    {
        currentMode = mode;
        if (modeIndicator != null) modeIndicator.color = GetColor(mode);
    }

    public Color GetColor(ScrewType type)
    {
        return type == ScrewType.Plus ? plusColor : minusColor;
    }

    //ネジを外す
    ///<summary>このネジを今外せるかをModuleScrewから毎フレーム問い合わせる</summary>
    public bool CanRemove(ModuleScrew screw)
    {
        if (isCleared) return false;
        if (mainGame == null || mainGame.State != GameState.Playing) return false;
        if (screw == null || screw.IsRemoved) return false;

        //ドライバーとネジが違うときは外せない(誤操作扱いではない)
        return screw.Type == currentMode;
    }

    ///<summary>ModuleScrewからネジが外れた通知を受け取る</summary>
    public void OnScrewRemoved(ModuleScrew screw)
    {
        if (isCleared) return;

        if (CheckAllRemoved())
        {
            HandleModuleCleared();
        }
    }

    private bool CheckAllRemoved()
    {
        foreach (var s in screws)
        {
            if (s != null && !s.IsRemoved) return false;
        }
        return true;
    }

    private void HandleModuleCleared()
    {
        isCleared = true;
        onCleared?.Invoke();

        if (mainGame == null)
        {
            Debug.LogError("[ScrewModule] MainGame が未設定です。");
            return;
        }
        mainGame.NotifyModuleCleared(ModuleId.Screw);
    }
}
