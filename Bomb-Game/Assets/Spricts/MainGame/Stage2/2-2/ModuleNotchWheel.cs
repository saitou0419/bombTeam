using UnityEngine;

/// <summary>
/// 円1つの回転と停止を管理する。設定された角度の組ごとに、
/// くぼみが固定の黄色い線に合っているかを判定する。
/// </summary>
public class ModuleNotchWheel : MonoBehaviour
{
    [SerializeField] private RectTransform rotatingVisual;

    [Tooltip("1秒あたりの回転角度。負の値で逆回転")]
    [SerializeField] private float degreesPerSecond = 90f;

    [Tooltip("Z回転が0度のときのくぼみの角度。右が0度、左が180度")]
    [SerializeField] private float[] notchAngles;

    [Tooltip("対応する黄色い線の角度。Notch Anglesと同じ順で設定する")]
    [SerializeField] private float[] targetAngles;

    [Tooltip("黄色い線との角度差を何度まで許容するか")]
    [Range(0f, 45f)]
    [SerializeField] private float toleranceDegrees = 8f;

    private NotchWheelModule owner;
    private float angle;

    /// <summary>この円が正しい位置で停止済みかどうか。</summary>
    public bool IsStopped { get; private set; }

    /// <summary>円を未停止に戻し、開始角度を抽選する。</summary>
    /// <param name="module">この円を管理する表面モジュール。</param>
    public void ResetWheel(NotchWheelModule module)
    {
        owner = module;
        IsStopped = false;
        angle = Random.Range(0f, 360f);
        ApplyRotation();
    }

    private void Update()
    {
        if (IsStopped || owner == null || !owner.CanOperate) return;

        angle = Mathf.Repeat(angle + degreesPerSecond * Time.deltaTime, 360f);
        ApplyRotation();
    }

    /// <summary>
    /// この円のButtonのOn Click()から呼ぶ。
    /// すべてのくぼみが許容範囲に入っていれば円を止める。
    /// </summary>
    public void Press()
    {
        if (!isActiveAndEnabled || IsStopped || owner == null ||
            !owner.CanOperate || rotatingVisual == null) return;

        if (notchAngles == null || targetAngles == null ||
            notchAngles.Length == 0 || notchAngles.Length != targetAngles.Length)
        {
            Debug.LogError("くぼみと黄色い線の角度を同じ数だけ設定してください", this);
            return;
        }

        for (int i = 0; i < notchAngles.Length; i++)
        {
            float difference = Mathf.DeltaAngle(angle + notchAngles[i], targetAngles[i]);
            if (Mathf.Abs(difference) > toleranceDegrees)
            {
                owner.ReportPress(false);
                return;
            }
        }

        IsStopped = true;
        owner.ReportPress(true);
    }

    /// <summary>計算した角度を回転画像のZ軸に反映する。</summary>
    private void ApplyRotation()
    {
        if (rotatingVisual != null)
        {
            rotatingVisual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
