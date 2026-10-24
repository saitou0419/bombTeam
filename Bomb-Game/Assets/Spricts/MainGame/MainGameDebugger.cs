using UnityEngine;

/// <summary>
/// UI無しでMainGameを動作確認するためのデバックスクリプト
/// 本番ビルドでは外す
/// </summary>
public class MainGameDebugger : MonoBehaviour
{
    [SerializeField] private MainGame mainGame;
    [SerializeField] private int stageIndexToStart = 0;

    private void Reset()
    {
        mainGame = GetComponent<MainGame>();
    }

    private void OnGUI()
    {
        if (mainGame == null)
        {
            GUILayout.Label("MainGameが未設定です");
            return;
        }

        float scale = Screen.height / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        GUILayout.BeginArea(new Rect(10,10,300,250), GUI.skin.box);

        string stageId = mainGame.CurrentStage != null ? mainGame.CurrentStage.stageId : ("なし");
        GUILayout.Label($"Stage: {stageId}");
        GUILayout.Label($"Stage: {mainGame.State}");
        GUILayout.Label($"Time: {mainGame.RemainingTime:F1}");
        GUILayout.Label($"Cleard: {mainGame.ClearedModuleCount}/{mainGame.TotalModuleCount}");
        GUILayout.Label($"Score: {mainGame.CurrentStage}");

        if (mainGame.HasResult)
        {
            var r = mainGame.Result;
            GUILayout.Label($"Result: {(r.isCleared ? "CLEAR" : "FAILED")} / 残り{r.remainingTime:F1}s / Score {r.score}");
        }

        GUILayout.Space(8);

        if (GUILayout.Button("ステージ開始/再開始"))
        {
            mainGame.SelectStage(stageIndexToStart);
        }

        GUILayout.Space(8);
        GUILayout.Label("モジュール解除通知");
        foreach (var id in mainGame.RequiredModules)
        {
            if (GUILayout.Button($"{id}を解除"))
            {
                mainGame.NotifyModuleCleared(id);
            }
        }

        GUILayout.Space(8);
        if (GUILayout.Button("誤操作を通知"))
        {
            mainGame.NotifyModuleFailed();
        }
        
        if (GUILayout.Button("スコア+10(動作確認)"))
        {
            mainGame.AddScore(10);
        }

        GUILayout.EndArea();
    }
}
