using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ開始時に、そのステージで必要なモジュールだけを表示するスクリプト
/// ・Stage Layouts に登録したステージ:そこに入れたオブジェクトだけを表示する(1-4用など)
/// ・登録がないステージ:StageData の requiredModules に合うモジュールだけを表示する
/// MainGame.onStageStarted に OnStageStarted を登録して使う
/// </summary>
public class StageModuleSwitcher : MonoBehaviour
{
    [Serializable]
    public class ModuleEntry
    {
        [Tooltip("このモジュールの種類")]
        public ModuleId moduleId = ModuleId.None;
        [Tooltip("そのモジュールの親 GameObject(ButtonModule などを付けたオブジェクト)")]
        public GameObject root;
    }

    [Serializable]
    public class StageLayout
    {
        [Tooltip("対象の StageData の Stage Id(例: 1-4)")]
        public string stageId = "";
        [Tooltip("このステージで表示するオブジェクト ここに入れたものだけが表示される")]
        public GameObject[] roots;
    }

    [Header("標準のモジュール(1-1〜1-3など、requiredModulesで出し分けるもの)")]
    [SerializeField] private ModuleEntry[] modules;

    [Header("ステージ専用の表示(1-4など、専用のオブジェクトを使うステージ)")]
    [SerializeField] private StageLayout[] stageLayouts;

    [Header("開始前の表示")]
    [Tooltip("オンなら、ステージが始まるまでは全モジュールを隠す")]
    [SerializeField] private bool hideAllUntilStageStarts = true;

    private void Awake()
    {
        if (hideAllUntilStageStarts)
        {
            ApplyVisible(new HashSet<GameObject>());
        }
    }

    /// <summary>
    /// MainGame.onStageStarted から呼ぶ(StageData引数はInspectorの動的バインド用)
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        if (stage == null)
        {
            Debug.LogError("[StageModuleSwitcher] StageData が null です。");
            return;
        }

        var toShow = new HashSet<GameObject>();
        StageLayout layout = FindLayout(stage.stageId);

        if (layout != null)
        {
            // ステージ専用の表示:登録されたオブジェクトだけを出す
            if (layout.roots == null || layout.roots.Length == 0)
            {
                Debug.LogWarning($"[StageModuleSwitcher] ステージ '{stage.stageId}' の Roots が空です。何も表示されません。");
            }
            else
            {
                foreach (var go in layout.roots)
                {
                    if (go != null) toShow.Add(go);
                }
            }
        }
        else
        {
            // 標準:requiredModules に入っているモジュールだけを出す
            var required = new HashSet<ModuleId>(stage.requiredModules);
            var registered = new HashSet<ModuleId>();

            if (modules != null)
            {
                foreach (var entry in modules)
                {
                    if (entry == null || entry.root == null) continue;

                    registered.Add(entry.moduleId);
                    if (required.Contains(entry.moduleId)) toShow.Add(entry.root);
                }
            }

            foreach (var id in required)
            {
                if (id == ModuleId.None) continue;
                if (!registered.Contains(id))
                {
                    Debug.LogWarning($"[StageModuleSwitcher] ステージ '{stage.stageId}' に必要な {id} が modules に登録されていません。");
                }
            }
        }

        ApplyVisible(toShow);
    }

    private StageLayout FindLayout(string stageId)
    {
        if (stageLayouts == null) return null;
        foreach (var layout in stageLayouts)
        {
            if (layout != null && layout.stageId == stageId) return layout;
        }
        return null;
    }

    /// <summary>管理対象のオブジェクトのうち、toShow に入っているものだけを表示する</summary>
    private void ApplyVisible(HashSet<GameObject> toShow)
    {
        var all = new HashSet<GameObject>();

        if (modules != null)
        {
            foreach (var entry in modules)
            {
                if (entry != null && entry.root != null) all.Add(entry.root);
            }
        }
        if (stageLayouts != null)
        {
            foreach (var layout in stageLayouts)
            {
                if (layout == null || layout.roots == null) continue;
                foreach (var go in layout.roots)
                {
                    if (go != null) all.Add(go);
                }
            }
        }

        foreach (var go in all)
        {
            bool want = toShow.Contains(go);
            if (go.activeSelf != want) go.SetActive(want);
        }
    }
}