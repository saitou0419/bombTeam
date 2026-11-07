using UnityEngine;

public class StagePanelController : MonoBehaviour
{
    public GameObject[] stagePanels;

    public void OpenStagePanel(int index)
    {
        foreach(GameObject panel in stagePanels)
        {
            panel.SetActive(false);
        }

        stagePanels[index].SetActive(true);
    }

    
}
