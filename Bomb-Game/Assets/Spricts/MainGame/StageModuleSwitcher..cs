using UnityEngine;

/// <summary>
/// ステージ開始時に、そのステージの StageRoot だけを表示するスクリプト
/// シーン内のStageRootを自動で探すため、ステージを増やしても登録は不要
/// MainGame.onStageStarted に OnStageStarted を登録して使う(この1行だけでよい)
/// </summary>
public class StageModuleSwitcher : MonoBehaviour
{
    [Header("開始前の表示")]
    [Tooltip("オンなら、ステージが始まるまでは全ステージの親を隠す")]
    [SerializeField] private bool hideAllUntilStageStarts = true;

    private StageRoot[] roots;

    private void Awake()
    {
        FindRoots();
        if (hideAllUntilStageStarts)
        {
            foreach (var r in roots)
            {
                if (r != null) r.gameObject.SetActive(false);
            }
        }
    }

    private void FindRoots()
    {
        // 非表示のものも含めて探す
        roots = FindObjectsByType<StageRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    /// <summary>
    /// MainGame.onStageStarted から呼ぶ(StageData引数はInspectorの動的バインド用)
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        if (stage == null)
        {
            Debug.LogError("[StageModuleSwitcher] StageDataがnullです");
            return;
        }
        if (roots == null) FindRoots();

        // Stage Id が一致する StageRoot を探す
        StageRoot target = null;
        foreach (var r in roots)
        {
            if (r == null || !r.Matches(stage.stageId)) continue;

            if (target == null)
            {
                target = r;
            }
            else
            {
                Debug.LogWarning($"[StageModuleSwitcher] Stage Id'{stage.stageId}'のStageRootが複数あります({r.name}) 最初の1つだけ使います");
            }
        }

        // 一致したものだけ表示し、それ以外は隠す
        foreach (var r in roots)
        {
            if (r != null) r.gameObject.SetActive(r == target);
        }

        if (target == null)
        {
            Debug.LogWarning($"[StageModuleSwitcher] Stage Id'{stage.stageId}'に合う StageRoot がありません 何も表示されません");
            return;
        }

        // 表示した後に、子のモジュールを初期化する
        target.Setup(stage);

        // StageData が要求するモジュールが、この親の中に揃っているか確認する
        var provided = target.GetProvidedModules();
        foreach (var id in stage.requiredModules)
        {
            if (id == ModuleId.None) continue;
            if (!provided.Contains(id))
            {
                Debug.LogWarning($"[StageModuleSwitcher] ステージ'{stage.stageId}'に必要な{id}が、StageRoot'{target.name}'の中にありません クリアできません");
            }
        }
    }
}