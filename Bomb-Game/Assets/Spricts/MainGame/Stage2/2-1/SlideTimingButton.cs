using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 裏面の「動くボタン」に付けるスクリプト
/// 左クリックを押した瞬間に、SlideTimingModule へ知らせる
/// どのボタンかは、SlideTimingModule が自動で設定する
/// </summary>
public class SlideTimingButton : MonoBehaviour, IPointerDownHandler
{
    private SlideTimingModule module;
    private int index = -1;

    /// <summary>SlideTimingModule から呼ばれる初期化</summary>
    public void Setup(SlideTimingModule owner, int buttonIndex)
    {
        module = owner;
        index = buttonIndex;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 左クリックのみ
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (module == null || index < 0) return;

        module.OnButtonPressed(index);
    }
}