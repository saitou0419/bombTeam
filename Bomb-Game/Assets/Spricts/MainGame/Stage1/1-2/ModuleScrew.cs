using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>ネジの種類</summary>
public enum ScrewType
{
    Plus,
    Minus
}

/// <summary>
/// ネジ一個のみ
/// 種類の保持・表示、外す順番の数字の表示、左クリック長押し+マウス移動の検出、取り外し状態を担当
/// 外せるかどうかの判定(ドライバーの種類・順番・ゲーム状態)は ScrewModule に問い合わせる
/// </summary>
public class ModuleScrew : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("初期状態(ランダム配置がオフのときに使う)")]
    [SerializeField] private ScrewType initialType = ScrewType.Plus;

    [Header("見た目を変える対象(UIのImageなど)")]
    [SerializeField] private Graphic targetGraphic;

    [Header("外す順番の数字(任意。子のTextMeshProを自動で探す)")]
    [SerializeField] private TMP_Text orderLabel;

    private ScrewModule owner;
    private bool isPressing;
    private Vector2 dragOrigin;
    private float dragProgress;

    public ScrewType Type { get; private set; }
    public bool IsRemoved { get; private set; }
    public ScrewType InitialType => initialType;

    /// <summary>外す順番(同じ種類の中で1から) 0は順番なし</summary>
    public int Order { get; private set; }

    /// <summary>外す操作の進み具合(0〜1) 演出を付けるとき用</summary>
    public float DragProgress01
    {
        get
        {
            if (owner == null) return 0f;
            return Mathf.Clamp01(dragProgress / owner.DragDistance);
        }
    }

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
        orderLabel = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnDisable()
    {
        CancelPress();
    }

    /// <summary>ScrewModule から呼ばれる初期化</summary>
    public void Initialize(ScrewModule screwModule)
    {
        owner = screwModule;
        if (orderLabel == null) orderLabel = GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>取り付け直して初期状態に戻す 種類は initialType になり、順番はなくなる</summary>
    public void ResetScrew()
    {
        IsRemoved = false;
        isPressing = false;
        dragProgress = 0f;
        ApplyRotation(0f);
        if (targetGraphic != null) targetGraphic.enabled = true;
        ClearOrder();
        SetType(initialType);
    }

    public void SetType(ScrewType newType)
    {
        Type = newType;
        ApplyVisual();
    }

    /// <summary>外す順番を設定し、数字を表示する</summary>
    public void SetOrder(int order)
    {
        Order = order;
        if (orderLabel != null)
        {
            orderLabel.text = order.ToString();
            orderLabel.gameObject.SetActive(order > 0 && !IsRemoved);
        }
    }

    /// <summary>順番をなくし、数字を消す</summary>
    public void ClearOrder()
    {
        Order = 0;
        if (orderLabel != null) orderLabel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 見た目の反映。今は色だけ
    /// </summary>
    private void ApplyVisual()
    {
        if (targetGraphic == null || owner == null) return;
        targetGraphic.color = owner.GetColor(Type);
    }

    // 操作(左クリックを押す → 押したまま外す方向へ動かす)

    public void OnPointerDown(PointerEventData eventData)
    {
        // 左クリックのみ
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsRemoved) return;

        isPressing = true;
        dragOrigin = eventData.position;
        SetProgress(0f);

        // 押した瞬間に1回だけ、ドライバー違い・順番違いをScrewModuleに判定してもらう
        if (owner != null) owner.OnScrewPressed(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressing || IsRemoved || owner == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // ドライバーが違う・順番が違う・Playing以外のときは進めず、その位置から数え直す
        if (!owner.CanRemove(this))
        {
            dragOrigin = eventData.position;
            SetProgress(0f);
            return;
        }

        // 外す方向にどれだけ動かしたか
        float moved = Vector2.Dot(eventData.position - dragOrigin, owner.RemoveDirection);

        // 逆方向に動かしたときは、その位置を新しい基準にする
        if (moved < 0f)
        {
            dragOrigin = eventData.position;
            moved = 0f;
        }

        SetProgress(moved);

        if (moved >= owner.DragDistance)
        {
            Remove();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 離したら進行はゼロに戻る(ネジの外で離した場合も同じ)
        CancelPress();
    }

    private void CancelPress()
    {
        isPressing = false;
        SetProgress(0f);
    }

    /// <summary>進行量を更新し、見た目の回転に反映する</summary>
    private void SetProgress(float pixels)
    {
        dragProgress = pixels;

        float degrees = (owner != null) ? DragProgress01 * owner.VisualRotationDegrees : 0f;
        ApplyRotation(degrees);
    }

    /// <summary>ネジを回して見せる 数字は読めるように、逆向きに回して傾かないようにする</summary>
    private void ApplyRotation(float degrees)
    {
        Quaternion rot = Quaternion.Euler(0f, 0f, degrees);
        transform.localRotation = rot;
        if (orderLabel != null) orderLabel.rectTransform.localRotation = Quaternion.Inverse(rot);
    }

    private void Remove()
    {
        IsRemoved = true;
        isPressing = false;
        dragProgress = 0f;
        ApplyRotation(0f);
        ClearOrder(); // 外したら数字は消える
        if (targetGraphic != null) targetGraphic.enabled = false; // 非表示にして操作も受けなくする
        owner.OnScrewRemoved(this);
    }
}
