using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectButton : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "MainGame";

    public void SelectStage(int stageIndex)
    {
        MainGame.selectedStageIndex = stageIndex;

        SceneManager.LoadScene(gameSceneName);
    }
}
