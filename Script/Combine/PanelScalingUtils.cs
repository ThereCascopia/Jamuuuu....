using UnityEngine;

public static class PanelScalingUtils
{
    // Calculate scale factor between two transforms
    public static float CalculateScaleFactor(Transform sourceTransform, Transform destinationTransform, float minScale = 0.2f, float maxScale = 2.5f)
    {
        if (sourceTransform == null || destinationTransform == null)
        {
            Debug.LogWarning("CalculateScaleFactor: Source or destination transform is null!");
            return 1f;
        }

        float sourceScale = GetGlobalScale(sourceTransform).x;
        float destScale = GetGlobalScale(destinationTransform).x;

        if (Mathf.Approximately(sourceScale, 0f))
        {
            Debug.LogWarning("CalculateScaleFactor: Source scale is zero!");
            return 1f;
        }

        float ratio = destScale / sourceScale;
        return Mathf.Clamp(ratio, minScale, maxScale);
    }

    public static Vector3 CalculateScaleFactorVec3(Transform sourceTransform, Transform destinationTransform)
    {
        if (sourceTransform == null || destinationTransform == null)
        {
            Debug.LogWarning("CalculateScaleFactorVec3: Source or destination transform is null!");
            return Vector3.one;
        }

        Vector3 sourceScale = GetGlobalScale(sourceTransform);
        Vector3 destScale = GetGlobalScale(destinationTransform);

        return new Vector3(
            Mathf.Clamp(destScale.x / Mathf.Max(sourceScale.x, 0.0001f), 0.2f, 2.5f),
            Mathf.Clamp(destScale.y / Mathf.Max(sourceScale.y, 0.0001f), 0.2f, 2.5f),
            Mathf.Clamp(destScale.z / Mathf.Max(sourceScale.z, 0.0001f), 0.2f, 2.5f)
        );
    }

    // Get the global scale of a transform by multiplying all parent scales
    public static Vector3 GetGlobalScale(Transform transform)
    {
        if (transform == null)
        {
            Debug.LogWarning("GetGlobalScale: Transform is null!");
            return Vector3.one;
        }

        Vector3 globalScale = transform.localScale;
        Transform parent = transform.parent;

        while (parent != null)
        {
            globalScale = Vector3.Scale(globalScale, parent.localScale);
            parent = parent.parent;
        }

        return globalScale;
    }

    // Adjust a game object's scale when moving between panels
    public static void AdjustScaleForPanel(GameObject item, Transform sourcePanel, Transform destinationPanel)
    {
        if (item == null)
        {
            Debug.LogWarning("AdjustScaleForPanel: Item is null!");
            return;
        }

        if (sourcePanel == null || destinationPanel == null)
        {
            Debug.LogWarning("AdjustScaleForPanel: Source or destination panel is null!");
            return;
        }

        if (!IsSameRootCanvas(sourcePanel, destinationPanel))
        {
            float scaleFactor = CalculateScaleFactor(sourcePanel, destinationPanel);
            item.transform.localScale = item.transform.localScale * scaleFactor;

            Debug.Log($"Adjusted scale for {item.name}: Scale factor = {scaleFactor}");
        }
        else
        {
            Debug.Log($"Same canvas detected for {item.name}, no scale adjustment needed.");
        }
    }


    // Find the root canvas for a transform
    public static Canvas FindRootCanvas(Transform transform)
    {
        if (transform == null)
        {
            Debug.LogWarning("FindRootCanvas: Transform is null!");
            return null;
        }

        Canvas canvas = transform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("FindRootCanvas: No Canvas found!");
            return null;
        }

        while (canvas.transform.parent != null)
        {
            Canvas parentCanvas = canvas.transform.parent.GetComponentInParent<Canvas>();
            if (parentCanvas == null)
                break;
            canvas = parentCanvas;
        }

        return canvas;
    }

    public static bool IsSameRootCanvas(Transform a, Transform b)
    {
        Canvas canvasA = FindRootCanvas(a);
        Canvas canvasB = FindRootCanvas(b);

        if (canvasA == null || canvasB == null) return false;

        return canvasA == canvasB;
    }

}