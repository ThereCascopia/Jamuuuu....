using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Improved SlotCombine with better scaling handling for receiving drag & drop items
public class SlotCombine : MonoBehaviour, IDropHandler
{
    public int slotIndex; // Index slot ini dalam array slotPanelCombine
    private ICraftingPanel craftingPanel;
    private Image currentImage;

    private BahanItem currentBahan;

    void Start()
    {
        // Temukan panel crafting yang sesuai
        craftingPanel = PanelDetector.FindCraftingPanel(gameObject);

        if (craftingPanel == null)
        {
            Debug.LogWarning("DroppableSlotCombine: Tidak dapat menemukan ICraftingPanel!");
        }

        Debug.Log("DroppableSlotCombine initialized on " + gameObject.name + " with index " + slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null)
        {
            Debug.LogWarning("OnDrop: No dragged object found!");
            return;
        }

        SlotBahan draggedBahan = draggedObject.GetComponent<SlotBahan>();
        if (draggedBahan == null)
        {
            Debug.LogWarning($"OnDrop: Dragged object {draggedObject.name} has no SlotBahan component!");
            return;
        }

        if (craftingPanel == null)
        {
            Debug.LogError("OnDrop: No craftingPanel found!");
            return;
        }

        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine == null)
        {
            Debug.LogError("Slot_For_Combine tidak ditemukan di " + gameObject.name);
            return;
        }

        // Bersihkan semua anak di Slot_For_Combine (misal tes lama)
        foreach (Transform child in slotForCombine)
        {
            Destroy(child.gameObject);
        }

        // Reset image di Slot_For_Combine agar visual bersih
        Image slotForCombineImage = slotForCombine.GetComponent<Image>();
        if (slotForCombineImage != null)
        {
            slotForCombineImage.sprite = null;
            slotForCombineImage.color = new Color(1f, 1f, 1f, 0f); // transparan
        }

        BahanItem bahan = draggedBahan.GetBahan();
        if (bahan == null)
        {
            Debug.LogError($"OnDrop: SlotBahan on {draggedObject.name} has no BahanItem assigned!");
            return;
        }

        Debug.Log($"OnDrop: Got valid BahanItem: {bahan.itemName} for slot {slotIndex}");

        SetBahan(bahan);

        // Tandai dropped valid
        draggedBahan.droppedOnValidSlot = true;

        // Kembalikan bahan lama jika ada dan beda objek
        if (currentImage != null && currentImage.gameObject != draggedBahan.gameObject)
        {
            SlotBahan existingDrag = currentImage.GetComponent<SlotBahan>();
            if (existingDrag != null)
            {
                existingDrag.ReturnToOriginalPosition();
            }
        }

        try
        {
            Transform originalParent = draggedBahan.transform.parent;
            Vector3 originalScale = draggedBahan.transform.localScale;

            // Hitung skala jika perlu
            float scaleRatio = craftingPanel.NeedsScaleAdjustment
                ? PanelScalingUtils.CalculateScaleFactor(originalParent, slotForCombine)
                : 1f;

            // Tambahkan clamp untuk jaga-jaga
            scaleRatio = Mathf.Clamp(scaleRatio, 0.2f, 2.5f); // sesuaikan rentangnya jika perlu

            // Terapkan skala baru
            draggedBahan.transform.localScale = originalScale * scaleRatio;

            // Set parent ke Slot_For_Combine
            draggedBahan.transform.SetParent(slotForCombine, false);
            draggedBahan.transform.localPosition = Vector3.zero;

            // Simpan data bahan
            SetBahan(bahan);

            Debug.Log($"Saved BahanItem reference {bahan.itemName} in slot {slotIndex}");

            // Update current image referensi
            currentImage = draggedBahan.GetComponent<Image>();

            // Notifikasi panel crafting
            craftingPanel.TempatkanBahanKeSlotCombine(slotIndex);

            draggedBahan.StoreCurrentAsLastValid();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error during drag-drop: " + e.Message + "\n" + e.StackTrace);
            draggedBahan.ReturnToOriginalPosition();
        }
    }


    // Method untuk menyimpan referensi ke bahan yang saat ini ada di slot
    public void SetCurrentImage(Image image)
    {
        currentImage = image;
    }

    // Method untuk mendapatkan bahan yang saat ini ada di slot
    public Image GetCurrentImage()
    {
        return currentImage;
    }

    // Method to set the ingredient name directly
    public void SetBahan(BahanItem bahan)
    {
        currentBahan = bahan;
    }

    public BahanItem GetBahan()
    {
        return currentBahan;
    }

    // Method untuk membersihkan slot
    public void ClearSlot()
    {
        currentBahan = null;
        currentImage = null;

        Transform slotForCombine = transform.Find("Slot_For_Combine");
        if (slotForCombine != null)
        {
            Image img = slotForCombine.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }

            // Hapus semua anak juga
            foreach (Transform child in slotForCombine)
            {
                Destroy(child.gameObject);
            }
        }
    }

}