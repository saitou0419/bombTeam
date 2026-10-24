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
public class ModuleScrew : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("初期状態")]
    [SerializeField] private ScrewType initialType = ScrewType.Plus;

    [Header("見た目を変える対象")]
    [SerializeField] private Graphic targetGraphic;

    private ScrewModule owner;
    private bool isHolding;
    private float heldTime;

    public ScrewType Type { get; private set; }
    public bool IsRemoved { get; private set; }
    public ScrewType InitialType => initialType;

    ///<summary>長押しの進み具合 演出用</summary>
    public float HoldProgress01
    {
        get
        {
            if (owner == null || owner.HoldSeconds <= 0f) return 0f;
            return Mathf.Clamp01(heldTime / owner.HoldSeconds);
        }
    }

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
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
        isHolding = false;
        heldTime = 0f;
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

    private void Update()
    {
        if (!isHolding || IsRemoved || owner == null) return;

        //ドライバーが違う・Playing以外の時は進めない
        if (!owner.CanRemove(this))
        {
            heldTime = 0f;
            return;
        }

        heldTime += Time.deltaTime;
        if (heldTime >= owner.HoldSeconds)
        {
            Remove();
        }
    }

    private void Remove()
    {
        IsRemoved = true;
        isHolding = false;
        heldTime = 0f;
        if (targetGraphic != null) targetGraphic.enabled = false;
        owner.OnScrewRemoved(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //左クリックのみ
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsRemoved) return;

        isHolding = true;
        heldTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        CanselHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CanselHold();
    }

    private void CanselHold()
    {
        isHolding = false;
        heldTime = 0f;
    }
}
