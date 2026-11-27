using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ボタンモジュール本体(ModuleId.Button)。
/// 赤いボタンを上の列の左から右へ、次の列の左から右へ、という順番で左クリックすると緑になる
/// 全ボタンが緑になったら MainGame に解除成功を通知する
/// 順番はボタンの位置から自動で決まる
/// </summary>
public class ButtonModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("このモジュールのボタン(いったん8個)")]
    [SerializeField] private ModuleButton[] buttons;

    [Header("初期配置のランダム設定")]
    [Tooltip("オンなら、リセットのたびに赤・緑をランダムに決める。オフなら各ボタンの Initial State を使う")]
    [SerializeField] private bool randomizeOnReset = true;
    [Tooltip("各ボタンが緑になる確率(0〜1)。仮の値であり正式仕様ではない")]
    [Range(0f, 1f)]
    [SerializeField] private float greenProbability = 0.5f;

    [Header("押す順番(位置で決まる)")]
    [Tooltip("同じ列(行)とみなす縦方向の差。列の間隔より小さく、同じ列のずれより大きくする")]
    [SerializeField] private float rowTolerance = 30f;

    [Header("ミスの扱い")]
    [Tooltip("順番と違う赤いボタンを押したとき、ミスとして数えるか")]
    [SerializeField] private bool wrongOrderIsMistake = true;
    [Tooltip("緑のボタンをクリックしたとき、ミスとして数えるか(未確定のため初期値はオフ)")]
    [SerializeField] private bool greenClickIsMistake = false;

    [Header("演出用イベント(任意。音やエフェクトなどに使う)")]
    public UnityEvent onCleared;

    private bool isCleared;
    private readonly List<ModuleButton> pressSequence = new List<ModuleButton>(); // 押す順番に並べた赤いボタン
    private int nextIndex;                                                         // 次に押すべき番号(0から)

    private void Reset()
    {
        // コンポーネント追加時に、子のボタンを自動で拾う
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

    /// <summary>
    /// MainGame.onStageStarted から呼ぶ用(StageData引数はInspectorの動的バインド用)
    /// ステージ開始時にモジュールを初期状態へ戻す
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        ResetModule();
    }

    /// <summary>モジュールの状態をリセットする(配置を引き直し、押す順番を作り直す)</summary>
    public void ResetModule()
    {
        isCleared = false;
        nextIndex = 0;
        pressSequence.Clear();

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("[ButtonModule] buttonsが空です");
            return;
        }

        // 以前の数字表示は使わない(ラベルが残っていても隠れる)
        foreach (var b in buttons)
        {
            if (b != null) b.ClearOrder();
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

        BuildPressSequence();

        if (CheckAllGreen())
        {
            Debug.LogWarning("[ButtonModule] 初期状態で赤いボタンが1つもありません 解除できないモジュールになります");
        }
    }

    /// <summary>各ボタンを確率で緑にし、赤が1つも残らなかった場合は1つだけ赤にする</summary>
    private void RandomizeButtons()
    {
        foreach (var b in buttons)
        {
            if (b == null) continue;
            bool green = Random.value < greenProbability;
            b.SetState(green ? ButtonColorState.Green : ButtonColorState.Red);
        }

        // 全部緑になってしまった場合は、ランダムな1つを赤に戻す
        if (CheckAllGreen())
        {
            ModuleButton pick = PickRandomValidButton();
            if (pick != null) pick.SetState(ButtonColorState.Red);
        }
    }

    private ModuleButton PickRandomValidButton()
    {
        // null要素を飛ばして、ランダムに1つ選ぶ
        int start = Random.Range(0, buttons.Length);
        for (int i = 0; i < buttons.Length; i++)
        {
            ModuleButton b = buttons[(start + i) % buttons.Length];
            if (b != null) return b;
        }
        return null;
    }

    /// <summary>
    /// 赤いボタンを、「上の列から下の列へ、同じ列の中では左から右へ」の順に並べる
    /// 位置は、このオブジェクトを基準にしたローカル座標で比べる
    /// </summary>
    private void BuildPressSequence()
    {
        pressSequence.Clear();

        // レイアウト(Grid Layout Groupなど)が反映されてから位置を読む
        Canvas.ForceUpdateCanvases();

        var reds = new List<(ModuleButton button, Vector2 pos)>();
        foreach (var b in buttons)
        {
            if (b == null || b.IsGreen) continue;
            Vector3 local = transform.InverseTransformPoint(b.transform.position);
            reds.Add((b, new Vector2(local.x, local.y)));
        }

        // 上(Yが大きい)から順に並べ、近い高さのものを同じ列にまとめる
        reds.Sort((a, c) => c.pos.y.CompareTo(a.pos.y));

        var rows = new List<List<(ModuleButton button, Vector2 pos)>>();
        float rowTopY = 0f;
        foreach (var item in reds)
        {
            if (rows.Count == 0 || rowTopY - item.pos.y > rowTolerance)
            {
                rows.Add(new List<(ModuleButton button, Vector2 pos)>());
                rowTopY = item.pos.y;
            }
            rows[rows.Count - 1].Add(item);
        }

        // 各列の中では、左(Xが小さい)から順に並べる
        foreach (var row in rows)
        {
            row.Sort((a, c) => a.pos.x.CompareTo(c.pos.x));
            foreach (var item in row) pressSequence.Add(item.button);
        }
    }

    /// <summary>ModuleButton からクリックを受け取る</summary>
    public void OnButtonClicked(ModuleButton clicked)
    {
        if (isCleared) return;

        // Playing中のみ操作を受け付ける
        if (mainGame == null)
        {
            Debug.LogError("[ButtonModule] MainGameが未設定です");
            return;
        }
        if (mainGame.State != GameState.Playing) return;

        // 緑のボタンを押したときの扱いは未確定のため、今は何もしない(Inspectorでミスにもできる)
        if (clicked.IsGreen)
        {
            if (greenClickIsMistake) mainGame.NotifyModuleMistake();
            return;
        }

        // 順番違い:ミスとして数える ボタンは赤のまま
        if (nextIndex >= pressSequence.Count || clicked != pressSequence[nextIndex])
        {
            if (wrongOrderIsMistake) mainGame.NotifyModuleMistake();
            return;
        }

        // 正しい順番:緑にして、次の順番へ
        clicked.SetState(ButtonColorState.Green);
        nextIndex++;

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
        mainGame.NotifyModuleCleared(ModuleId.Button);
    }
}