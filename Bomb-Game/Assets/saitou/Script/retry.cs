using UnityEngine;
using UnityEngine.SceneManagement;

public class retry : MonoBehaviour
{
    [SerializeField] private string gameSceneName;

    public void retryButtonClicked()
    {
        MainGame.retryStageId = ResultHolder.Current.stageId;

        SceneManager.LoadScene(gameSceneName);
    }
}