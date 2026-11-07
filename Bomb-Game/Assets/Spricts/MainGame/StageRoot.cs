using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 「このステージで表示するもの」をまとめる親オブジェクトに付けるスクリプト
/// 子にボタン・ネジ・配線モジュールを入れておくと、 StageModuleSwitcher が Stage Id の一致するときだけ表示し、モジュールを初期化する
/// </summary>
public class StageRoot : MonoBehaviour
{
    [Tooltip("このオブジェクトを表示するステージのStage Id(StageData と同じ文字列。例: 1-4)")]
    [SerializeField] private string stageId = "";

    [Tooltip("同じ配置を使い回す他のステージのStage Id(使い回さないなら空のままでよい")]
    [SerializeField] private string[] additionalStageIds;

    public string StageId => stageId;

    /// <summary>指定の Stage Id で、この親を表示してよいか</summary>
    public bool Matches(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (stageId == id) return true;

        if (additionalStageIds != null)
        {
            foreach (var s in additionalStageIds)
            {
                if (s == id) return true;
            }
        }
        return false;
    }

    /// <summary>表示された直後に呼ばれる。子のモジュールをすべて初期状態に戻す</summary>
    public void Setup(StageData stage)
    {
        foreach (var m in GetComponentsInChildren<ButtonModule>(true)) m.OnStageStarted(stage);
        foreach (var m in GetComponentsInChildren<ScrewModule>(true)) m.OnStageStarted(stage);
        foreach (var m in GetComponentsInChildren<WireModule>(true)) m.OnStageStarted(stage);
        foreach (var m in GetComponentsInChildren<TimingModule>(true)) m.OnStageStarted(stage);
    }

    /// <summary>この親の中にあるモジュールの種類(確認用)</summary>
    public HashSet<ModuleId> GetProvidedModules()
    {
        var set = new HashSet<ModuleId>();
        if (GetComponentsInChildren<ButtonModule>(true).Length > 0) set.Add(ModuleId.Button);
        if (GetComponentsInChildren<ScrewModule>(true).Length > 0) set.Add(ModuleId.Screw);
        if (GetComponentsInChildren<WireModule>(true).Length > 0) set.Add(ModuleId.Wire);
        if (GetComponentsInChildren<TimingModule>(true).Length > 0) set.Add(ModuleId.Timing);
        return set;
    }
}
