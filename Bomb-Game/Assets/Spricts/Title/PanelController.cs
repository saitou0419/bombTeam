using UnityEngine;
using UnityEngine.InputSystem;

public class PanelController : MonoBehaviour
{
    public GameObject panel;

    void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OpenPanel();
        }
    }

    public void HidePanel()
    {
        panel.SetActive(false);

    }


    public void OpenPanel()
    {
        panel.SetActive(true);

    }
}
