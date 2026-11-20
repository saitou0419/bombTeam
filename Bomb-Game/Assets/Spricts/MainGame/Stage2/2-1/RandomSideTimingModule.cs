using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 2-1 のタイミングモジュール全体(ModuleId.Timing)。
/// ステージ開始のたびに、「表(回る円と電球)」か「裏(H形・エ形などのギミック)」のどちらかを
/// ランダムに選んで表示する。
/// 裏が選ばれたときは、登録された裏面(Backs)をすべて同時に表示する
/// 選ばれた面をクリアしたら、MainGame に解除成功を1回通知する
/// 選ばれなかった面は非表示にして、動きも止める
/// </summary>
public class RandomSideTimingModule : MonoBehaviour
{
    /// <summary>どちらの面を使うか(テスト用に固定もできる)</summary>
    public enum SideChoice
    {
        Random,     // ステージ開始のたびにランダム(本番用)
        FrontOnly,  // 表面に固定(動作確認用)
        BackOnly    // 裏面に固定(動作確認用)
    }

    [Header("接続")]
    [SerializeField] private MainGame mainGame;

    [Header("表と裏")]
    [Tooltip("表面(TimingModule を付けたオブジェクト)。表面を使わないなら空でもよい")]
    [SerializeField] private TimingModule front;
    [Tooltip("裏面(SlideTimingModule を付けたオブジェクト)。H型・エ型など 裏が選ばれたときは全部が同時に表示される")]
    [SerializeField] private SlideTimingModule[] backs;

    [Header("どちらの面を使うか")]
    [Tooltip("本番は Random。動作確認のときだけFrontOnly/BackOnlyで固定できる")]
    [SerializeField] private SideChoice sideChoice = SideChoice.Random;
    [Tooltip("Random のとき、表面が選ばれる確率(0〜1) 残りが裏面(H型+エ型) 仮の値であり正式仕様ではない")]
    [Range(0f, 1f)]
    [SerializeField] private float frontProbability = 0.5f;

    [Header("裏面のクリア条件")]
    [Tooltip("オンなら、表示された裏面をすべてクリアしたときに解除 オフなら、どれか1つクリアで解除(未確定のため仮の値)")]
    [SerializeField] private bool requireAllBacks = true;

    [Header("演出用イベント(任意)")]
    public UnityEvent onCleared;

    private bool initialized;
    private bool isCleared;
    private bool frontSelected = true;

    /// <summary>今回、表面が選ばれたか(UIなどが参照する用)</summary>
    public bool IsFrontSelected => frontSelected;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnDestroy()
    {
        if (front != null && front.onCleared != null) front.onCleared.RemoveListener(OnFrontCleared);
        if (backs == null) return;
        foreach (var b in backs)
        {
            if (b != null && b.onCleared != null) b.onCleared.RemoveListener(OnBackCleared);
        }
    }

    /// <summary>表裏のクリア通知を受け取る準備。一度だけ行う</summary>
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        if (front != null)
        {
            // 表面は、クリアしても MainGame に直接通知しない(通知はここでまとめる)
            front.SetReportToMainGame(false);
            if (front.onCleared == null) front.onCleared = new UnityEvent();
            front.onCleared.AddListener(OnFrontCleared);
        }

        if (backs == null) return;
        foreach (var b in backs)
        {
            if (b == null) continue;
            if (b.onCleared == null) b.onCleared = new UnityEvent();
            b.onCleared.AddListener(OnBackCleared);
        }
    }

    /// <summary>
    /// ステージ開始時に StageRoot から呼ばれる(StageData引数はInspectorの動的バインド用)
    /// すべての面を初期状態に戻し、今回使う面(表か裏)を選ぶ
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        EnsureInitialized();
        isCleared = false;

        if (front == null && CountValidBacks() == 0)
        {
            Debug.LogError("[RandomSideTimingModule] front、backs両方未設定です");
            return;
        }

        if (front != null) front.ResetModule();
        if (backs != null)
        {
            foreach (var b in backs)
            {
                if (b != null) b.ResetModule();
            }
        }

        frontSelected = ChooseFront();
        ShowSelectedSide();
    }

    private int CountValidBacks()
    {
        int count = 0;
        if (backs == null) return 0;
        foreach (var b in backs)
        {
            if (b != null) count++;
        }
        return count;
    }

    /// <summary>今回、表面を使うか(true)、裏面を使うか(false)を決める</summary>
    private bool ChooseFront()
    {
        bool hasFront = front != null;
        bool hasBack = CountValidBacks() > 0;

        if (!hasFront) return false;
        if (!hasBack) return true;

        switch (sideChoice)
        {
            case SideChoice.FrontOnly: return true;
            case SideChoice.BackOnly: return false;
            default: return Random.value < frontProbability;
        }
    }

    /// <summary>選ばれた面だけを表示する 裏面が選ばれたときは、Backsをすべて表示する</summary>
    private void ShowSelectedSide()
    {
        if (front != null) front.gameObject.SetActive(frontSelected);

        if (backs == null) return;
        foreach (var b in backs)
        {
            if (b != null) b.gameObject.SetActive(!frontSelected);
        }
    }

    // クリア判定

    /// <summary>表面をクリアしたとき</summary>
    private void OnFrontCleared()
    {
        if (frontSelected) CompleteModule();
    }

    /// <summary>裏面のどれかをクリアしたとき。条件を満たしたら解除する</summary>
    private void OnBackCleared()
    {
        if (frontSelected || isCleared) return;

        if (requireAllBacks && !AllBacksCleared()) return;
        CompleteModule();
    }

    private bool AllBacksCleared()
    {
        foreach (var b in backs)
        {
            if (b != null && !b.IsCleared) return false;
        }
        return true;
    }

    /// <summary>解除成功を MainGame に1回だけ通知する。</summary>
    private void CompleteModule()
    {
        if (isCleared) return;
        isCleared = true;

        onCleared?.Invoke();

        if (mainGame == null)
        {
            Debug.LogError("[RandomSideTimingModule] MainGameが未設定です");
            return;
        }
        mainGame.NotifyModuleCleared(ModuleId.Timing);
    }
}