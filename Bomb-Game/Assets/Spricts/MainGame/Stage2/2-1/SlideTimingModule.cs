using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// タイミングモジュールの裏面
/// 左右二本の柱の中でそれぞれボタンが上下に動いている
/// ボタンが横棒と同じ高さにある時にクリックするとそのボタンが止まる
/// 全てのボタンを止めたらクリア
/// 判定はローカルオブジェクトのY座標で行う
/// </summary>
public class SlideTimingModule : MonoBehaviour
{
    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("H形のパーツ(すべてこのオブジェクトの直下に置く　Anchor・Pivotは中央)")]
    [Tooltip("横棒(水色の判定)。縦の位置はリセットのたびにランダムに決まる")]
    [SerializeField] private Graphic bar;
    [Tooltip("動くボタン。0番=左の柱、1番=右の柱　個数がそのまま「必要な成功回数」になる")]
    [SerializeField] private Graphic[] buttons;

    [Header("動く範囲(仮の値。UIのピクセル)")]
    [Tooltip("ボタンが動く範囲。中心から上下にこの距離まで動く")]
    [SerializeField] private float travelHalfHeight = 140f;
    [Tooltip("横棒の位置がランダムに決まる範囲。中心から上下にこの距離まで")]
    [SerializeField] private float barRandomRange = 80f;

    [Header("判定(仮の値)")]
    [Tooltip("水色の判定の半分の高さ。ボタンの中心が横棒の中心からこの距離以内なら成功")]
    [SerializeField] private float hitHalfHeight = 14f;

    [Header("ボタンの動き(仮の値)")]
    [Tooltip("速さの最小(ピクセル/秒)　ボタンごとに最小〜最大の間でランダムに決まる")]
    [SerializeField] private float speedMin = 120f;
    [Tooltip("速さの最大(ピクセル/秒)")]
    [SerializeField] private float speedMax = 220f;
    [Tooltip("ボタン同士の速さの最低の差　同じ速さになることを防ぐ")]
    [SerializeField] private float minSpeedDifference = 40f;
    [Tooltip("開始時に、ボタンが横棒からどれだけ離れているか。開始直後に押せてしまうことを防ぐ")]
    [SerializeField] private float startClearance = 40f;
    [Tooltip("オンなら0番(左)が上向き、1番(右)が下向きで始まる　オフなら逆")]
    [SerializeField] private bool firstButtonStartsUp = true;

    [Header("ミスの扱い")]
    [Tooltip("水色の外で押したら、ミスとして数えるか")]
    [SerializeField] private bool missIsMistake = true;

    [Header("色(仮の見た目)")]
    [SerializeField] private Color barColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] private Color buttonColor = new Color(1f, 0.55f, 0.15f);
    [SerializeField] private Color stoppedButtonColor = new Color(0.2f, 0.8f, 0.3f);

    [Header("演出用イベント(任意)")]
    public UnityEvent onSuccessHit;
    public UnityEvent onCleared;

    private float[] positions;
    private float[] speeds;
    private float[] directions;
    private bool[] stopped;
    private float barY;
    private bool isCleared;

    public bool IsCleared => isCleared;

    ///<summary>
    ///裏面を初期状態に戻し、横棒の位置を引き直す
    /// </summary>
    public void ResetModule()
    {
        isCleared = false;

        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("[SlideTimingModule] buttonsが空です 解除できないモジュールになります");
            positions = null;
            return;
        }

        int n = buttons.Length;
        positions = new float[n];
        speeds = new float[n];
        directions = new float[n];
        stopped = new bool[n];

        // 横棒の位置:動く範囲の中に収まるようにランダムに決める
        float limit = Mathf.Max(0f, Mathf.Min(barRandomRange, travelHalfHeight - hitHalfHeight));
        barY = Random.Range(-limit, limit);
        ApplyBar();

        for (int i = 0; i < n; i++)
        {
            speeds[i] = PickSpeed(i);
            positions[i] = PickStartPosition();
            bool up = (i % 2 == 0) == firstButtonStartsUp;
            directions[i] = up ? 1f : -1f;

            if (buttons[i] != null)
            {
                buttons[i].color = buttonColor;

                var press = buttons[i].GetComponent<SlideTimingButton>();
                if (press != null) press.Setup(this, i);
                else Debug.LogWarning($"[SlideTimingModule] buttons[{i}]にSlideTimingButtonが付いていません クリックできません");
            }
            ApplyButton(i);
        }
    }

    /// <summary>他のボタンと速さの差が十分あるように、ランダムな速さを決める</summary>
    private float PickSpeed(int index)
    {
        float s = Random.Range(speedMin, speedMax);
        for (int t = 0; t < 50; t++)
        {
            bool ok = true;
            for (int j = 0; j < index; j++)
            {
                if (Mathf.Abs(s - speeds[j]) < minSpeedDifference) { ok = false; break; }
            }
            if (ok) return s;
            s = Random.Range(speedMin, speedMax);
        }
        return s; // 見つからなければ最後の値をそのまま使う
    }

    /// <summary>横棒から十分離れた、ランダムな開始位置を決める</summary>
    private float PickStartPosition()
    {
        for (int t = 0; t < 50; t++)
        {
            float p = Random.Range(-travelHalfHeight, travelHalfHeight);
            if (Mathf.Abs(p - barY) >= startClearance) return p;
        }
        // 見つからなければ、横棒と反対側の端から始める
        return barY >= 0f ? -travelHalfHeight : travelHalfHeight;
    }

    // 見た目の反映

    private void ApplyBar()
    {
        if (bar == null) return;
        RectTransform rt = bar.rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, barY);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, hitHalfHeight * 2f);
        bar.color = barColor;
    }

    private void ApplyButton(int index)
    {
        if (buttons[index] == null) return;
        RectTransform rt = buttons[index].rectTransform;
        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, positions[index]);
    }

    // 動き(Playing中のみ。非表示の間は Update ごと止まる)

    private void Update()
    {
        if (isCleared || positions == null) return;
        if (mainGame == null || mainGame.State != GameState.Playing) return;

        for (int i = 0; i < positions.Length; i++)
        {
            if (stopped[i]) continue;

            positions[i] += directions[i] * speeds[i] * Time.deltaTime;

            // 端に着いたら折り返す
            if (positions[i] >= travelHalfHeight)
            {
                positions[i] = travelHalfHeight;
                directions[i] = -1f;
            }
            else if (positions[i] <= -travelHalfHeight)
            {
                positions[i] = -travelHalfHeight;
                directions[i] = 1f;
            }

            ApplyButton(i);
        }
    }

    // 押したときの判定

    /// <summary>動くボタンを押した瞬間にSlideTimingButtonから呼ばれる</summary>
    public void OnButtonPressed(int index)
    {
        if (isCleared) return;
        if (mainGame == null)
        {
            Debug.LogError("[SlideTimingModule] MainGameが未設定です");
            return;
        }
        if (mainGame.State != GameState.Playing) return;
        if (positions == null || index < 0 || index >= positions.Length) return;

        // 止まったボタンを押しても何も起きない
        if (stopped[index]) return;

        // 横棒(水色)と同じ高さなら成功、外れていたらミス(ボタンは動き続ける)
        if (Mathf.Abs(positions[index] - barY) <= hitHalfHeight)
        {
            HandleSuccess(index);
        }
        else if (missIsMistake)
        {
            mainGame.NotifyModuleMistake();
        }
    }

    private void HandleSuccess(int index)
    {
        stopped[index] = true;
        if (buttons[index] != null) buttons[index].color = stoppedButtonColor;
        onSuccessHit?.Invoke();

        foreach (bool s in stopped)
        {
            if (!s) return;
        }

        isCleared = true;
        onCleared?.Invoke();
    }
}