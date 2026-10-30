using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 配線一本のみ
/// 色の保持・表示・左クリックの検出、切断を担当
/// 正解かどうかはWireModuleで
/// </summary>
public class ModuleWire : MonoBehaviour, IPointerClickHandler
{
    [Header("初期の色")]
    [Tooltip("WireModuleの色リストの番号(0スタート)")]
    [SerializeField] private int initialColorIndex = 0;

    [Header("見た目を変える対象")]
    [SerializeField] private Graphic targetGraphic;

    [Header("切断後の見た目")]
    [Tooltip("切断の透明度 0なら完全に消える 仮の見た目")]
    [Range(0f, 1f)]
    [SerializeField] private float cutAlpha = 0.25f;

    private WireModule owner;
    private int groupIndex;
    private Color baseColor = Color.white;

    public int ColorIndex { get; private set; }
    public bool IsCut { get; private set; }
    public int InitialColorIndex => initialColorIndex;
    public int GroupIndex => groupIndex;

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
    }

    ///<summary>WireModuleから呼ばれる初期化</summary>
    public void Initialize(WireModule module, int group)
    {
        owner = module;
        groupIndex = group;
    }

    ///<summary>繋ぎ直して、指定の色にする</summary>
    public void ResetWire(int colorIndex, Color color)
    {
        IsCut = false;
        ColorIndex = colorIndex;
        baseColor = color;
        ApplyVisual();
    }

    ///<summary>切断する</summary>
    public void Cut()
    {
        IsCut = true;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (targetGraphic == null) return;

        Color c  = baseColor;
        c.a = IsCut ? cutAlpha : 1f;
        targetGraphic.color = c;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 左クリックのみ
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (IsCut || owner == null) return;

        owner.OnWireClicked(this);
    }
}
