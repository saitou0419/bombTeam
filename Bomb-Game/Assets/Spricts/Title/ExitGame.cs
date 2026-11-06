using UnityEngine;

public class ExitGame : MonoBehaviour
{

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Unityエディタでのテスト再生を停止する
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルドした実際のゲーム（出力後）を終了する
        Application.Quit();
#endif
    }
}
