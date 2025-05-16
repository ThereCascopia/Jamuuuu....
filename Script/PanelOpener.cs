using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    // Make sure to use the correct type declaration for the GameObject
    public GameObject panel;

    // Method to open the panel
    public void OpenPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }
}

