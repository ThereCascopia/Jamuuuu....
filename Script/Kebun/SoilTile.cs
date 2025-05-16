using UnityEngine;
using System.Collections;

public class SoilTile : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    private BenihItem benihItem;      // Data benih yang ditanam
    private bool isPlanted = false;
    private int currentStage = -1;
    private float timer = 0f;

    private void OnMouseDown()
    {
        if (!isPlanted)
        {
            // Tanam jika belum ditanam
            if (PlantingSystem.Instance != null)
            {
                PlantingSystem.Instance.StartPlanting(this);
            }
            else
            {
                Debug.LogError("PlantingSystem instance is null.");
            }
        }
        else if (benihItem != null && currentStage >= benihItem.growthStages.Length - 1)
        {
            Harvest();
        }
        else
        {
            Debug.Log("Tanaman belum siap dipanen.");
        }
    }

    public void Plant(BenihItem benih)
    {
        benihItem = JamuSystem.Instance.GetBenih(benih.itemName);
        if (benihItem == null)
        {
            Debug.LogError("BenihItem tidak ditemukan di JamuSystem: " + benih.itemName);
            return;
        }

        isPlanted = true;
        currentStage = 0;
        timer = 0f;
        spriteRenderer.sprite = benihItem.growthStages[currentStage];

        StartCoroutine(Grow());
    }

    IEnumerator Grow()
    {
        while (currentStage < benihItem.growthStages.Length - 1)
        {
            yield return new WaitForSeconds(benihItem.growthTime);
            currentStage++;
            spriteRenderer.sprite = benihItem.growthStages[currentStage];
        }
    }

    public void Harvest()
    {
        if (!isPlanted || benihItem == null || currentStage < benihItem.growthStages.Length - 1)
            return;

        BahanItem hasil = benihItem.producesBahan;

        if (hasil != null)
        {
            Item hasilPanen = new Item
            {
                nama = hasil.itemName,
                gambar = hasil.itemSprite,
                harga = hasil.itemValue,
                jumlah = 1
            };

            Inventory.Instance.AddItemToInventory(hasilPanen);

            Debug.Log($"Panen berhasil: {hasil.itemName} masuk ke inventory.");
        }
        else
        {
            Debug.LogWarning("Benih ini tidak menghasilkan bahan saat dipanen.");
        }

        ResetSoil();
    }

    public void ResetSoil()
    {
        isPlanted = false;
        benihItem = null;
        currentStage = -1;
        timer = 0f;
        spriteRenderer.sprite = null;
    }
}
