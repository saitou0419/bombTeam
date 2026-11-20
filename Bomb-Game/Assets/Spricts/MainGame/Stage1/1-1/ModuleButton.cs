using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>ボタンの色の状態</summary>
public enum ButtonColorState
{
    Red,
    Green
}

/// <summary>
/// ボタン一個のみ
/// 色の保持と表示、左クリックの検出担当
/// クリックされたらBottonModuleに知らせる
/// </summary>
public class ModuleButton : MonoBehaviour, IPointerClickHandler
{
    [Header("初期状態")]
    [SerializeField] private ButtonColorState initialState = ButtonColorState.Red;

    [Header("表示色")]
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color grennColor = Color.green;

    [Header("色を変える対象")]
    [Tooltip("UIのImage等")]
    [SerializeField] private Graphic targetGraphic;
    [Tooltip("ワールド空間のsprite")]
    [SerializeField] private SpriteRenderer targetSprite;

    [Header("押す順番の数字(任意 子のTextMeshProを自動で探す)")]
    [SerializeField] private TMP_Text orderLabel;

    private ButtonModule ownerModule;

    public ButtonColorState State { get; private set; }
    public bool IsGreen => State == ButtonColorState.Green;
    public ButtonColorState InitialState => initialState;

    /// <summary>押す順番(1から) 0は順番なし(緑のボタンなど)</summary>
    public int Order { get; private set; }

    private void Reset()
    {
        //コンポーネント追加時、同じオブジェクトを表示している物を自動で拾う
        targetGraphic = GetComponent<Graphic>();
        targetSprite = GetComponent<SpriteRenderer>();
        orderLabel = GetComponentInChildren<TMP_Text>(true);
    }

    ///<summary>ButtonModuleから呼ばれる初期化</summary>
    public void Initialize(ButtonModule owner)
    {
        ownerModule = owner;
        if (orderLabel == null) orderLabel = GetComponentInChildren<TMP_Text>(true);
        ResetButton();
    }

    ///<summary>初期状態に戻す</summary>
    public void ResetButton()
    {
        ClearOrder();
        SetState(InitialState);
    }

    public void SetState(ButtonColorState newState)
    {
        State = newState;
        Color c = (newState == ButtonColorState.Green) ? grennColor : redColor;

        if (targetGraphic != null) targetGraphic.color = c;
        if (targetSprite != null) targetSprite.color = c;

        RefreshOrderLabel();
    }

    /// <summary>押す順番を設定し、数字を表示する(緑のときは表示しない)</summary>
    public void SetOrder(int order)
    {
        Order = order;
        if (orderLabel != null) orderLabel.text = order.ToString();
        RefreshOrderLabel();
    }

    /// <summary>順番をなくし、数字を消す</summary>
    public void ClearOrder()
    {
        Order = 0;
        RefreshOrderLabel();
    }

    /// <summary>数字は「順番があり、かつ赤いとき」だけ表示する</summary>
    private void RefreshOrderLabel()
    {
        if (orderLabel == null) return;
        orderLabel.gameObject.SetActive(Order > 0 && State == ButtonColorState.Red);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (ownerModule == null) return;

        ownerModule.OnButtonClicked(this);
    }
}
