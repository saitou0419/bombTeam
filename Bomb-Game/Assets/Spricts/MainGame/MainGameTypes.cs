using System;
using UnityEngine;

/// <summary>
/// ゲームの進行状態
/// </summary>
public enum GameState
{
    Ready,  //待機
    Playing,   //開始
    Cleared,    //クリア
    Failed, //失敗
    Result, //リザルト
}

/// <summary>
/// モジュールの識別用
/// </summary>
public enum ModuleId
{
    None = 0,
    Button,
    Screw,
    Wire,
}

///<summary>
///UI側に渡す
/// </summary>
[Serializable]
public struct ResultData
{
    public string stageId;  // ステージ識別子
    public bool isCleared;  // true: クリア / false: 失敗
    public float remainingTime; // 残り時間(秒)
    public int score;   // スコア(計算式は未確定)
}

/// <summary>
/// 最新のリザルトを保持する静的クラス
/// MainGame が書き込み、Result シーン側が読み取る
/// シーンを切り替えても値が残る
/// </summary>
public static class ResultHolder
{
    public static bool HasResult { get; private set; }
    public static ResultData Current { get; private set; }

    public static void Set(ResultData data)
    {
        Current = data;
        HasResult = true;
    }

    public static void Clear()
    {
        Current = default;
        HasResult = false;
    }
}
