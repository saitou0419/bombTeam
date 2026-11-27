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
/// ネジモジュール本体(ModuleId.Screw)。
/// プラス/マイナスの切り替え、ネジを「プラス → マイナス → プラス → …」と交互に外すルールの管理
/// 全ネジが外れたかの判定、MainGame への解除通知を行う
/// </summary>
public class ScrewModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("このモジュールのネジ(いったん8個)")]
    [SerializeField] private ModuleScrew[] screws;

    [Header("ドライバー(プラス/マイナス)")]
    [Tooltip("ステージ開始時のドライバーの種類")]
    [SerializeField] private ScrewType initialMode = ScrewType.Plus;
    [Tooltip("今どちらのドライバーかを色で表示する(任意)")]
    [SerializeField] private Graphic modeIndicator;

    [Header("交互に外す順番")]
    [Tooltip("最初に外すネジの種類。そのあとは交互になる(仮の値 プラスから)")]
    [SerializeField] private ScrewType firstRemoveType = ScrewType.Plus;

    [Header("ネジを外す操作(左クリックを押したままマウスを動かす)")]
    [Tooltip("マウスを動かす方向。仮の値であり正式仕様ではない")]
    [SerializeField] private ScrewRemoveDirection removeDirection = ScrewRemoveDirection.Left;
    [Tooltip("外れるまでに動かす距離(画面上のピクセル)。仮の値であり正式仕様ではない")]
    [SerializeField] private float dragDistance = 200f;
    [Tooltip("外れる直前までにネジが回って見える角度。見た目だけの仮の値")]
    [SerializeField] private float visualRotationDegrees = 360f;

    [Header("ネジの初期配置のランダム設定")]
    [Tooltip("オンなら、リセットのたびにプラス/マイナスを半々にランダム配置する(奇数なら最初の種類が1つ多い) オフなら各ネジの Initial Type を使う")]
    [SerializeField] private bool randomizeOnReset = true;

    [Header("ミスの扱い")]
    [Tooltip("ドライバーが合っているネジを、交互の順番と違う種類なのに押したとき、ミスとして数えるか(押した瞬間に1回)")]
    [SerializeField] private bool wrongOrderIsMistake = true;
    [Tooltip("ドライバーと違う種類のネジを押したとき、ミスとして数えるか(未確定のため初期値はオフ)")]
    [SerializeField] private bool wrongDriverIsMistake = false;

    [Header("種類の見分け用の色(スプライトが届くまでの仮)")]
    [SerializeField] private Color plusColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color minusColor = new Color(1f, 0.85f, 0.2f);

    [Header("演出用イベント(任意)")]
    public UnityEvent onCleared;

    private ScrewType currentMode;
    private ScrewType nextRequiredType;   // 次に外すべきネジの種類
    private bool isCleared;
    private bool initialized;

    public float DragDistance => Mathf.Max(1f, dragDistance);
    public float VisualRotationDegrees => visualRotationDegrees;
    public ScrewType CurrentMode => currentMode;

    /// <summary>次に外すべきネジの種類(指示書などUI側が参照する用)</summary>
    public ScrewType NextRequiredType => nextRequiredType;

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

    /// <summary>
    /// MainGame.onStageStarted から呼ぶ用(StageData引数はInspectorの動的バインド用)
    /// ステージ開始時にモジュールを初期状態へ戻す
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        EnsureInitialized();
        ResetModule();
    }

    /// <summary>モジュールの状態をリセットする(種類を引き直し、交互の順番を最初に戻す)</summary>
    public void ResetModule()
    {
        isCleared = false;
        nextRequiredType = firstRemoveType;
        SetMode(initialMode);

        if (screws == null || screws.Length == 0)
        {
            Debug.LogWarning("[ScrewModule] screws が空です。");
            return;
        }

        foreach (var s in screws)
        {
            if (s != null) s.ResetScrew();
        }

        if (randomizeOnReset) AssignTypesBalanced();
        else CheckBalanceWarning();
    }

    /// <summary>
    /// 交互に外し切れるように、プラスとマイナスを半々に割り振る
    /// 奇数なら、最初に外す種類が1つ多くなる
    /// </summary>
    private void AssignTypesBalanced()
    {
        var valid = new List<ModuleScrew>();
        foreach (var s in screws)
        {
            if (s != null) valid.Add(s);
        }

        Shuffle(valid);

        int firstCount = (valid.Count + 1) / 2;
        ScrewType other = (firstRemoveType == ScrewType.Plus) ? ScrewType.Minus : ScrewType.Plus;

        for (int i = 0; i < valid.Count; i++)
        {
            valid[i].SetType(i < firstCount ? firstRemoveType : other);
        }
    }

    /// <summary>ランダム配置をオフにしているとき、交互に外し切れる構成かを確認する</summary>
    private void CheckBalanceWarning()
    {
        int first = 0;
        int other = 0;
        foreach (var s in screws)
        {
            if (s == null) continue;
            if (s.Type == firstRemoveType) first++;
            else other++;
        }

        if (first != other && first != other + 1)
        {
            Debug.LogWarning($"[ScrewModule] 交互に外し切れない構成です(最初の種類 {first}個、もう一方 {other}個) 数を同じか、最初の種類を1個多くしてください");
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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

        // ドライバーは合っているが、今外すべき種類ではない:ミス
        if (screw.Type != nextRequiredType)
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

        // ドライバーが合っていて、かつ今外すべき種類のときだけ外せる
        return screw.Type == currentMode && screw.Type == nextRequiredType;
    }

    /// <summary>ModuleScrew からネジが外れた通知を受け取る</summary>
    public void OnScrewRemoved(ModuleScrew screw)
    {
        if (isCleared) return;

        // 次に外すべき種類を切り替える(プラス → マイナス → プラス …)
        nextRequiredType = (nextRequiredType == ScrewType.Plus) ? ScrewType.Minus : ScrewType.Plus;

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
            Debug.LogError("[ScrewModule] MainGameが未設定です");
            return;
        }
        mainGame.NotifyModuleCleared(ModuleId.Screw);
    }
}