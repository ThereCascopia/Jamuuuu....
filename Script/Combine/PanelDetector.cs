// PanelDetector.cs - Helper class to find the appropriate panel
using UnityEngine;

public static class PanelDetector
{
    // Helper method to find either panel type in a GameObject's hierarchy
    public static ICraftingPanel FindCraftingPanel(GameObject gameObject)
    {
        // Try to get the CombinePanel first
        CombinePanel combinePanel = gameObject.GetComponentInParent<CombinePanel>();
        if (combinePanel != null)
        {
            return combinePanel as ICraftingPanel;
        }

        // If not found, try to get the NPCCraftingPanel
        NPCCraftingPanel npcPanel = gameObject.GetComponentInParent<NPCCraftingPanel>();
        if (npcPanel != null)
        {
            return npcPanel as ICraftingPanel;
        }

        // If neither is found in immediate parents, search up the hierarchy
        Transform current = gameObject.transform.parent;
        while (current != null)
        {
            combinePanel = current.GetComponent<CombinePanel>();
            if (combinePanel != null)
            {
                return combinePanel as ICraftingPanel;
            }

            npcPanel = current.GetComponent<NPCCraftingPanel>();
            if (npcPanel != null)
            {
                return npcPanel as ICraftingPanel;
            }

            current = current.parent;
        }

        // No panel found
        return null;
    }
}