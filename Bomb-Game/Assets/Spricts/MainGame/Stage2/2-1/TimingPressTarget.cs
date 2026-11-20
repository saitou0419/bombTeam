using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 中央の緑の〇につけるスクリプト
/// 左クリックを押した瞬間にTimingModuleへ知らせる
/// </summary>
public class TimingPressTarget : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private TimingModule module;

    private void Reset()
    {
        module = GetComponentInParent<TimingModule>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (module == null)
        {
            Debug.LogError("[TimingPressTarget] TimingModuleが未設定です");
            return;
        }
        module.OnPressed();
    }
}
