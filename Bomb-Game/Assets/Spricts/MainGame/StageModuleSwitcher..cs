using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ステージ開始時にそのステージで必要なモジュールだけを表示するスクリプト
/// StageDataのrequiredModulesに入ってるモジュールのGameObjectを有効にし、入っていないものは無効にする
/// MainGame.onStageStartedにOnStageStartedを登録して使う
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

    [Header("モジュールの登録")]
    [SerializeField] private ModuleEntry[] modules;

    [Header("開始前の表示")]
    [Tooltip("オンなら、ステージが始まるまでは全モジュールを隠す")]
    [SerializeField] private bool hideAllUntilStageStarts = true;

    private void Awake()
    {
        if (hideAllUntilStageStarts)
        {
            SetAllActive(false);
        }
    }

    /// <summary>
    /// MainGame.onStageStartedから呼ぶ
    /// </summary>
    public void OnStageStarted(StageData stage)
    {
        if (stage == null)
        {
            Debug.LogError("[StageModuleSwitcher] StageData が null です。");
            return;
        }

        var required = new HashSet<ModuleId>(stage.requiredModules);

        // 必要なモジュールだけ表示
        var registered = new HashSet<ModuleId>();
        if (modules != null)
        {
            foreach (var entry in modules)
            {
                if (entry == null || entry.root == null) continue;

                registered.Add(entry.moduleId);
                entry.root.SetActive(required.Contains(entry.moduleId));
            }
        }

        // 必要なのに登録されていないモジュールは警告
        foreach (var id in required)
        {
            if (id == ModuleId.None) continue;
            if (!registered.Contains(id))
            {
                Debug.LogWarning($"[StageModuleSwitcher] ステージ'{stage.stageId}'に必要な{id}がmodulesに登録されていません。");
            }
        }
    }

    private void SetAllActive(bool active)
    {
        if (modules == null) return;
        foreach (var entry in modules)
        {
            if (entry != null && entry.root != null)
            {
                entry.root.SetActive(active);
            }
        }
    }
}
