using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitle : MonoBehaviour
{

    public void GoToTitle()
    {
        SceneManager.LoadScene("Title");
    }
    
}
