using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Improved SlotBahan with better scaling handling for drag & drop between different scale panels
public class SlotBahan : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Referensi ke komponen panel yang dibutuhkan
    private ICraftingPanel craftingPanel;

    // Variabel untuk drag & drop
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Camera canvasCamera;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Vector3 originalScale; // Track original scale
    public bool droppedOnValidSlot = false;
    private Transform lastValidParent;
    private Vector3 lastValidPosition;
    private Vector3 lastValidScale; // Track last valid scale
    private Vector3 dragStartPosition;
    private Vector3 pointerStartPosition;

    private BahanItem currentBahan;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Simpan posisi dan skala awal
        originalParent = transform.parent;
        originalPosition = rectTransform.localPosition;
        originalScale = transform.localScale;

        // Temukan root canvas
        rootCanvas = GetComponentInParent<Canvas>();
        while (rootCanvas != null && rootCanvas.transform.parent != null && rootCanvas.transform.parent.GetComponentInParent<Canvas>() != null)
            rootCanvas = rootCanvas.transform.parent.GetComponentInParent<Canvas>();
        canvasCamera = rootCanvas != null ? rootCanvas.worldCamera : null;

        if (originalParent == null) originalParent = transform.parent;
        if (originalPosition == Vector3.zero) originalPosition = rectTransform.localPosition;

        lastValidParent = originalParent;
        lastValidPosition = originalPosition;
        lastValidScale = originalScale;
    }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError($"SlotBahan: RectTransform is missing on {gameObject.name}");
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError($"SlotBahan: CanvasGroup is missing or could not be added on {gameObject.name}");
            return;
        }

        // Attach to a BahanItem if there's one already in the Image
        Image slotImage = GetComponent<Image>();
        if (slotImage != null && slotImage.sprite != null)
        {
            // Try to find a matching BahanItem from your database based on the sprite
            if (JamuSystem.Instance != null && JamuSystem.Instance.jamuDatabase != null)
            {
                // Find BahanItem by sprite if possible
                BahanItem foundBahan = JamuSystem.Instance.jamuDatabase.FindBahanBySprite(slotImage.sprite);
                if (foundBahan != null)
                {
                    SetBahan(foundBahan);
                    Debug.Log($"SlotBahan initialized with BahanItem: {foundBahan.itemName} based on sprite");
                }
            }
        }

        // Check if BahanItem is still null and if we can find it by name
        if (currentBahan == null && gameObject.name.Contains("_"))
        {
            string potentialBahanName = gameObject.name.Split('_').Last();
            if (JamuSystem.Instance != null && JamuSystem.Instance.jamuDatabase != null)
            {
                BahanItem foundBahan = JamuSystem.Instance.jamuDatabase.GetBahan(potentialBahanName);
                if (foundBahan != null)
                {
                    SetBahan(foundBahan);
                    Debug.Log($"SlotBahan initialized with BahanItem: {foundBahan.itemName} based on name");
                }
            }
        }

        // At this point if currentBahan is still null, log a warning
        if (currentBahan == null)
        {
            Debug.LogWarning($"SlotBahan on {gameObject.name} initialized without a BahanItem!");
        }
        // Temukan panel crafting yang sesuai
        craftingPanel = PanelDetector.FindCraftingPanel(gameObject);

        if (craftingPanel == null)
        {
            Debug.LogWarning("DraggableSlotBahan: Tidak dapat menemukan ICraftingPanel!");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = rectTransform.position;

        if (canvasCamera != null)
        {
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                canvasCamera,
                out pointerStartPosition
            );
        }
        else
        {
            pointerStartPosition = rectTransform.position;
        }

        originalPosition = rectTransform.localPosition;
        originalScale = transform.localScale;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f;

        Debug.Log($"Begin drag: {gameObject.name}, Original scale: {originalScale}, Parent: {transform.parent.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 currentPointerWorldPosition;

        // Convert the current pointer position to world space
        if (canvasCamera != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform,
                eventData.position,
                canvasCamera, // Use the camera assigned to the Canvas
                out currentPointerWorldPosition))
        {
            // Calculate the drag offset in world coordinates
            Vector3 dragOffset = currentPointerWorldPosition - pointerStartPosition;

            // Apply the drag offset to the object's world position
            rectTransform.position = dragStartPosition + dragOffset;
        }
        else
        {
            // Fallback if no camera or conversion fails
            Vector3 dragVectorDelta = eventData.delta;

            // Convert Vector3 to Vector2
            Vector2 dragVectorDelta2D = new Vector2(dragVectorDelta.x, dragVectorDelta.y);
            rectTransform.anchoredPosition += dragVectorDelta2D;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (!droppedOnValidSlot)
        {
            ReturnToOriginalPosition();
            Debug.Log($"Item returned to original position with scale: {transform.localScale}");
        }
        else
        {
            Debug.Log($"Item dropped successfully with scale: {transform.localScale}");
        }

        droppedOnValidSlot = false;
    }

    public void ReturnToOriginalPosition()
    {
        if (originalParent != null)
        {
            foreach (Transform child in originalParent)
            {
                if (child == this.transform)
                {
                    Debug.LogWarning("Item already exists in the original parent.");
                    return;
                }
            }
            transform.SetParent(originalParent);
            rectTransform.localPosition = originalPosition;

            // Restore the original scale
            transform.localScale = originalScale;
            lastValidParent = originalParent;
            lastValidPosition = originalPosition;
            lastValidScale = originalScale;

            Debug.Log($"Returned to original parent: {originalParent.name} with scale: {originalScale}");
        }
        else
        {
            Debug.LogWarning("Original parent is null, cannot return to original position properly.");
        }
    }

    public void ReturnToLastValidPosition()
    {
        if (lastValidParent != null)
        {
            transform.SetParent(lastValidParent);
            rectTransform.localPosition = lastValidPosition;
            transform.localScale = lastValidScale;
            Debug.Log($"Returned to last valid position with scale: {lastValidScale}");
        }
        else
        {
            ReturnToOriginalPosition();
        }
    }

    public void StoreCurrentAsOriginal()
    {
        originalParent = transform.parent;
        originalPosition = rectTransform.localPosition;
        originalScale = transform.localScale;
        Debug.Log($"Stored current as original - Scale: {originalScale}, Parent: {originalParent?.name ?? "null"}");
    }

    public void StoreCurrentAsLastValid()
    {
        lastValidParent = transform.parent;
        lastValidPosition = rectTransform.localPosition;
        lastValidScale = transform.localScale;

        Debug.Log($"Stored as last valid - Scale: {lastValidScale}, Parent: {lastValidParent?.name ?? "null"}");
    }

    public void SetBahan(BahanItem bahan)
    {
        if (currentBahan == bahan) return;
        currentBahan = bahan;
        Debug.Log($"SlotBahan.SetBahan: Setting bahan to {(bahan != null ? bahan.itemName : "NULL")} on {gameObject.name}");
    }

    public BahanItem GetBahan()
    {
        if (currentBahan == null)
        {
            Debug.Log($"SlotBahan.GetBahan: WARNING - BahanItem is NULL on {gameObject.name}");
        }
        return currentBahan;
    }
}