using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>ネジの種類(ドライバーの種類も同じ) </summary>
public enum ScrewType
{
    Plus,
    Minus
}

/// <summary>
/// ネジモジュール内のネジ一個
/// 種類の保持・表示と左クリック長押しの検出、取り出しを担当
/// 外せるかどうかの判定はScrewModuleに問い合わせる
/// </summary>
public class ModuleScrew : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("初期状態")]
    [SerializeField] private ScrewType initialType = ScrewType.Plus;

    [Header("見た目を変える対象")]
    [SerializeField] private Graphic targetGraphic;

    private ScrewModule owner;
    private bool isPressing;
    private Vector2 dragOrigin;
    private float dragProgress;

    public ScrewType Type { get; private set; }
    public bool IsRemoved { get; private set; }
    public ScrewType InitialType => initialType;

    ///<summary>外す操作の進み具合 演出用</summary>
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
    }

    private void OnDisable()
    {
        CancelPress();
    }

    ///<summary>ScrewModuleから呼ばれる初期化</summary>
    public void Initialize(ScrewModule screwModule)
    {
        owner = screwModule;
    }

    ///<summary>取り付け直して初期状態に戻す 種類はinitialTypeになる</summary>
    public void ResetScrew()
    {
        IsRemoved = false;
        isPressing = false;
        dragProgress = 0f;
        transform.localRotation = Quaternion.identity;
        if (targetGraphic != null) targetGraphic.enabled = true;
        SetType(initialType);
    }

    public void SetType(ScrewType newType)
    {
        Type = newType;
        ApplyVisual();
    }

    ///<summary>見た目の反映(仮)</summary>
    private void ApplyVisual()
    {
        if (targetGraphic == null || owner == null) return;
        targetGraphic.color = owner.GetColor(Type);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //左クリックのみ
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsRemoved) return;

        isPressing = true;
        dragOrigin = eventData.position;
        SetProgress(0f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressing || IsRemoved || owner == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        //ドライバーが違う・Playing以外のときは進めず、その位置から数え直す
        if (!owner.CanRemove(this))
        {
            dragOrigin = eventData.position;
            SetProgress(0f);
            return;
        }

        //外す方向にどれだけ動かしたか
        float moved = Vector2.Dot(eventData.position - dragOrigin, owner.RemoveDirection);

        //逆方向に動かしたときは、その位置を新しい基準にする
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
        transform.localRotation = Quaternion.Euler(0f, 0f, degrees);
    }

    private void Remove()
    {
        IsRemoved = true;
        isPressing = false;
        dragProgress = 0f;
        transform.localRotation = Quaternion.identity;
        if (targetGraphic != null) targetGraphic.enabled = false; // 非表示にして操作も受けなくする
        owner.OnScrewRemoved(this);
    }
}
