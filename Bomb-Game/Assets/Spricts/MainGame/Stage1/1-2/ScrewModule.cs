using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>ネジを外すためにマウスを動かす方向</summary>
public enum ScrewRemoveDirection
{
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// ネジモジュール本体(ModuleId.Screw)
/// プラス/マイナスの切り替え、外す順番(種類ごと)の管理、全ネジが外れたかの判定
/// </summary>
public class ScrewModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("このモジュールのネジ")]
    [SerializeField] private ModuleScrew[] screws;

    [Header("ドライバー(プラス/マイナス)")]
    [Tooltip("ステージ開始時のドライバーの種類")]
    [SerializeField] private ScrewType initialMode = ScrewType.Plus;
    [Tooltip("今どちらのドライバーかを色で表示する(任意)")]
    [SerializeField] private Graphic modeIndicator;

    [Header("ネジを外す操作(左クリックを押したままマウスを動かす)")]
    [Tooltip("マウスを動かす方向。仮の値であり正式仕様ではない")]
    [SerializeField] private ScrewRemoveDirection removeDirection = ScrewRemoveDirection.Left;
    [Tooltip("外れるまでに動かす距離(画面上のピクセル)。仮の値であり正式仕様ではない")]
    [SerializeField] private float dragDistance = 200f;
    [Tooltip("外れる直前までにネジが回って見える角度。見た目だけの仮の値")]
    [SerializeField] private float visualRotationDegrees = 360f;

    [Header("ネジの初期配置のランダム設定")]
    [Tooltip("オンなら、リセットのたびにプラス/マイナスをランダムに決める オフなら各ネジの Initial Type を使う")]
    [SerializeField] private bool randomizeOnReset = true;
    [Tooltip("各ネジがプラスになる確率(0〜1)。仮の値であり正式仕様ではない")]
    [Range(0f, 1f)]
    [SerializeField] private float plusProbability = 0.5f;

    [Header("ミスの扱い")]
    [Tooltip("ドライバーが合っているネジを、順番と違うのに押したとき、ミスとして数えるか(押した瞬間に1回)")]
    [SerializeField] private bool wrongOrderIsMistake = true;
    [Tooltip("ドライバーと違う種類のネジを押したとき、ミスとして数えるか(未確定のため初期値はオフ)")]
    [SerializeField] private bool wrongDriverIsMistake = false;

    [Header("種類の見分け用の色(スプライトが届くまでの仮)")]
    [SerializeField] private Color plusColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color minusColor = new Color(1f, 0.85f, 0.2f);

    [Header("演出用イベント(任意)")]
    public UnityEvent onCleared;

    private ScrewType currentMode;
    private bool isCleared;
    private bool initialized;
    private int nextPlusOrder = 1;    // プラスのネジで次に外す順番
    private int nextMinusOrder = 1;   // マイナスのネジで次に外す順番

    public float DragDistance => Mathf.Max(1f, dragDistance);
    public float VisualRotationDegrees => visualRotationDegrees;
    public ScrewType CurrentMode => currentMode;

    /// <summary>外す方向の単位ベクトル(画面座標)</summary>
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

    /// <summary>
    /// MainGame.onStageStarted から呼ぶ用(StageData引数はInspectorの動的バインド用)
    /// ステージ開始時にモジュールを初期状態へ戻す
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        EnsureInitialized();
        ResetModule();
    }

    /// <summary>モジュールの状態をリセットする(種類と順番を引き直す)</summary>
    public void ResetModule()
    {
        isCleared = false;
        nextPlusOrder = 1;
        nextMinusOrder = 1;
        SetMode(initialMode);

        if (screws == null || screws.Length == 0)
        {
            Debug.LogWarning("[ScrewModule] screwsが空です。");
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

        AssignOrders();
    }

    /// <summary>プラス・マイナスそれぞれの中で、1から始まる外す順番をランダムに割り振る</summary>
    private void AssignOrders()
    {
        var plus = new List<ModuleScrew>();
        var minus = new List<ModuleScrew>();

        foreach (var s in screws)
        {
            if (s == null) continue;
            if (s.Type == ScrewType.Plus) plus.Add(s);
            else minus.Add(s);
        }

        Shuffle(plus);
        Shuffle(minus);

        for (int i = 0; i < plus.Count; i++) plus[i].SetOrder(i + 1);
        for (int i = 0; i < minus.Count; i++) minus[i].SetOrder(i + 1);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private int GetNextOrder(ScrewType type)
    {
        return type == ScrewType.Plus ? nextPlusOrder : nextMinusOrder;
    }

    // プラス/マイナスの切り替え
    /// <summary>
    /// 切り替えボタンから呼ぶ(UIのButtonのOnClickに登録する)
    /// 押すたびにプラスとマイナスが交互に切り替わる
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


    // ネジを外す
    /// <summary>ネジを押した瞬間に ModuleScrew から1回だけ呼ばれる。ドライバー違い・順番違いを判定する</summary>
    public void OnScrewPressed(ModuleScrew screw)
    {
        if (isCleared || screw == null || screw.IsRemoved) return;
        if (mainGame == null || mainGame.State != GameState.Playing) return;

        // ドライバーと違う種類のネジ:順番の判定の対象外(ミスにするかはInspectorで選ぶ)
        if (screw.Type != currentMode)
        {
            if (wrongDriverIsMistake) mainGame.NotifyModuleMistake();
            return;
        }

        // ドライバーは合っているが、順番が違う:ミス
        if (screw.Order != GetNextOrder(screw.Type))
        {
            if (wrongOrderIsMistake) mainGame.NotifyModuleMistake();
        }
    }

    /// <summary>このネジを今外せるか。ModuleScrew から操作中に問い合わせる</summary>
    public bool CanRemove(ModuleScrew screw)
    {
        if (isCleared) return false;
        if (mainGame == null || mainGame.State != GameState.Playing) return false;
        if (screw == null || screw.IsRemoved) return false;

        // ドライバーとネジの種類が違うときは外せない
        if (screw.Type != currentMode) return false;

        // 順番違いのネジは外せない
        return screw.Order == GetNextOrder(screw.Type);
    }

    /// <summary>ModuleScrew からネジが外れた通知を受け取る</summary>
    public void OnScrewRemoved(ModuleScrew screw)
    {
        if (isCleared) return;

        // 同じ種類の、次の順番へ進める
        if (screw.Type == ScrewType.Plus) nextPlusOrder++;
        else nextMinusOrder++;

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
            Debug.LogError("[ScrewModule] MainGameが未設定です。");
            return;
        }
        mainGame.NotifyModuleCleared(ModuleId.Screw);
    }
}