using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class SceneChenger : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("StageSelect");
        }
    }

    public void GoToStageSelect()
    {
        SceneManager.LoadScene("StageSelect");
    }
}
