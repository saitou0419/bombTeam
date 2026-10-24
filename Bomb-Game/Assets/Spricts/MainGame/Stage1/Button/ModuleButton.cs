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

    private ButtonModule ownerModule;

    public ButtonColorState State { get; private set; }
    public bool IsGreen => State == ButtonColorState.Green;
    public ButtonColorState InitialState => initialState;

    private void Reset()
    {
        //コンポーネント追加時、同じオブジェクトを表示している物を自動で拾う
        targetGraphic = GetComponent<Graphic>();
        targetSprite = GetComponent<SpriteRenderer>();
    }

    ///<summary>ButtonModuleから呼ばれる初期化</summary>
    public void Initialize(ButtonModule owner)
    {
        ownerModule = owner;
        ResetButton();
    }

    ///<summary>初期状態に戻す</summary>
    public void ResetButton()
    {
        SetState(InitialState);
    }

    public void SetState(ButtonColorState newState)
    {
        State = newState;
        Color c = (newState == ButtonColorState.Green) ? grennColor : redColor;

        if (targetGraphic != null) targetGraphic.color = c;
        if (targetSprite != null) targetSprite.color = c;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (ownerModule == null) return;

        ownerModule.OnButtonClicked(this);
    }
}
