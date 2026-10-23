using System.Collections.Generic;
using UnityEngine;

///<summary>
///ステージ1つ分の設定データ
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "BombGame/Stage Data")]
public class StageData : ScriptableObject
{
    [Tooltip("ステージ識別子 重複防止")]
    public string stageId = "";

    [Tooltip("制限時間(秒) 0以下だと開始不可能")]
    public float timeLimit = 0f;

    [Tooltip("このステージで解除が必要なモジュール 全解除でクリア")]
    public List<ModuleId> requiredModules = new List<ModuleId>();
}
