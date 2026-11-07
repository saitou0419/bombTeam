using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// タイミングモジュール本体
/// 回転する円の上にある青い弧が、真上の目印を通るタイミングで真ん中の〇を押すと、電球が1つ付く
/// 全ての電球が付いたらMainGameに解除通知を鳴らす
/// 判定は角度で行う、見た目はその角度から決まる
/// </summary>
public class TimingModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("回転する円")]
    [Tooltip("回転させる親 青い弧はこれ AnchorとPivotは中央に")]
    [SerializeField] private RectTransform ring;
    [Tooltip("青い弧(image) 個数がそのまま「必要な成功回数になる」")]
    [SerializeField] private Graphic[] arcs;
    [Tooltip("電球(image) 青い弧と同じ数用意する")]
    [SerializeField] private Graphic[] bulbs;

    [Header("回転")]
    [Tooltip("回転の速さ(角度/秒) 一旦仮")]
    [SerializeField] private float rotationSpeed = 90f;
    [Tooltip("オンなら時計回り")]
    [SerializeField] private bool clockwise = true;

    [Header("青い弧")]
    [Tooltip("弧の長さ(角度) この範囲が真上と重なっているときに成功")]
    [SerializeField] private float arcDegrees = 30f;
    [Tooltip("弧を置く半径")]
    [SerializeField] private float ringRadius = 170f;
    [Tooltip("弧の太さ(見た目のみ)")]
    [SerializeField] private float arcThickness = 24f;
    [Tooltip("弧同氏の最低の間隔 2つが同時に真上にきて1回で両方押せるのを防ぐ")]
    [SerializeField] private float minArcseparation = 90f;
    [Tooltip("開始時に弧が真上の目印からどれだけ離れてるか 開始直後に押すことを防ぐ")]
    [SerializeField] private float startClearance = 45f;

    [Header("連続成功の防止")]
    [Tooltip("成功した後次の入力を受け付けない秒数")]
    [SerializeField] private float successCooldown = 1.0f;

    [Header("ミスの扱い")]
    [Tooltip("弧が真上じゃないときに押したらミスとして数えるか")]
    [SerializeField] private bool missIsMistake = true;

    [Header("色(仮の見た目)")]
    [SerializeField] private Color arcColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color bulb0ffColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color bulbOnColor = new Color(1f, 0.9f, 0.2f);

    [Header("演出用イベント(任意)")]
    public UnityEvent onSuccessHit;
    public UnityEvent onCleared;

    private float[] baseAngles;
    private bool[] arcDone;
    private float ringAngle;
    private float cooldownTimer;
    private int successCount;
    private bool isCleared;

    private void Awake()
    {
        ResetModule();   
    }

    ///<summary>
    ///MainGame.onStageStartから呼ぶ用
    ///ステージ開始時にモジュールを初期状態へ戻す
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        ResetModule();
    }

    ///<summary>モジュールの状態をリセットし、青い弧の位置を戻す</summary>
    public void ResetModule()
    {
        isCleared = false;
        successCount = 0;
        cooldownTimer = 0f;
        ringAngle = 0f;

        if (arcs == null || arcs.Length == 0)
        {
            Debug.LogWarning("[TimingModule] arcsが空です 解除できないモジュールになります");
            return;
        }
        if (bulbs == null || bulbs.Length == 0)
        {
            Debug.LogWarning("[TimingModule] bulbsがarcsより少ないです 電球の数を合わせてください");
        }

        arcDone = new bool[arcs.Length];
        PlaceArcs();
        ApplyRingRotation();

        if (bulbs != null)
        {
            foreach (var b in bulbs)
            {
                if (b != null) b.color = bulb0ffColor;
            }
        }
    }

    //青い弧の位置
    private void PlaceArcs()
    {
        int n = arcs.Length;
        baseAngles = new float[n];

        for (int i = 0; i < n; i++)
        {
            float angle = 0f;
            bool ok = false;

            for (int t = 0; t < 100 && !ok; t++)
            {
                angle = Random.Range(0f, 360f);
                ok = IsFarFromTop(angle) && IsFarFromPlaced(angle, i);
            }

            //条件を満たせなかったら等間隔に置く
            if (!ok) angle = Mathf.Repeat(90f + 360f / n * 1, 360f);

            baseAngles[i] = angle;
            ApplyArcVisual(i);
        }
    }

    private bool IsFarFromTop(float angle)
    {
        return Mathf.Abs(Mathf.DeltaAngle(angle, 0f)) >= startClearance;
    }

    private bool IsFarFromPlaced(float angle, int placedCount)
    {
        for (int j = 0; j < placedCount; j++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(angle, baseAngles[j])) < startClearance) return false;
        }
        return true;
    }

    ///<summary>角度から弧の位置・向き・長さを決める(見た目のみ)</summary>
    private void ApplyArcVisual(int index)
    {
        Graphic g = arcs[index];
        if (g == null) return;

        RectTransform rt = g.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        float rad = baseAngles[index] * Mathf.Deg2Rad;
        rt.anchoredPosition = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)) * ringRadius;
        rt.localRotation = Quaternion.Euler(0f, 0f, -baseAngles[index]);

        float chord = 2f * ringRadius * Mathf.Sin(arcDegrees * 0.5f * Mathf.Deg2Rad);
        rt.sizeDelta = new Vector2(Mathf.Max(1f, chord), arcThickness);

        g.color = arcColor;
    }

    //回転
    private void Update()
    {
        //Playing中のみ回す
        if (isCleared || mainGame == null || mainGame.State != GameState.Playing) return;

        float direction = clockwise ? 1f : -1f;
        ringAngle = Mathf.Repeat(ringAngle + direction * rotationSpeed * Time.deltaTime, 360f);
        ApplyRingRotation();

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    private void ApplyRingRotation()
    {
        if (ring != null) ring.localRotation = Quaternion.Euler(0f, 0f, -ringAngle);
    }

    //押したときの判定
    ///<summary>中央の〇を押した瞬間にTimingPresstargetから呼ばれる</summary>
    public void OnPressed()
    {
        if (isCleared) return;
        if (mainGame == null)
        {
            Debug.LogError("[TimingModule] MainGameが未設定です");
            return;
        }
        if (mainGame.State != GameState.Playing) return;
        if (arcs == null || arcs.Length == 0 || arcDone == null) return;

        //成功直後は入力を受け付けない(ミスにも数えない)
        if (cooldownTimer > 0f) return;

        bool insideDoneArc = false;
        for (int i = 0; i < arcs.Length; i++)
        {
            if (!IsArcAtTop(i)) continue;

            if (arcDone[i])
            {
                insideDoneArc = true;
                continue;
            }

            HandleSuccess(i);
            return;
        }

        if (insideDoneArc) return;

        if (missIsMistake) mainGame.NotifyModuleMistake();
    }

    ///<summary>指定の弧の中心が、真上の目印の範囲に入っているか</summary>
    private bool IsArcAtTop(int index)
    {
        float effective = Mathf.Repeat(baseAngles[index] + ringAngle, 360f);
        float diff = Mathf.Abs(Mathf.DeltaAngle(effective, 0f));
        return diff <= arcDegrees * 0.5f;
    }

    private void HandleSuccess(int arcIndex)
    {
        arcDone[arcIndex] = true;//弧は青のまま
        successCount++;
        cooldownTimer = successCooldown;

        int bulbIndex = successCount - 1;
        if (bulbs != null && bulbIndex < bulbs.Length && bulbs[bulbIndex] != null)
        {
            bulbs[bulbIndex].color = bulbOnColor;
        }
        onSuccessHit?.Invoke();

        if (successCount >= arcs.Length)
        {
            HandleModuleCleared();
        }
    }

    private void HandleModuleCleared()
    {
        isCleared = true;
        onCleared?.Invoke();
        mainGame.NotifyModuleCleared(ModuleId.Timing);
    }
}
