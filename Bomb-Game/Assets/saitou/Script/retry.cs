using UnityEngine;
using UnityEngine.SceneManagement;
public class retry: MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    public void retryButtonClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
