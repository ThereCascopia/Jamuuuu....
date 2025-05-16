using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;

// Updated to implement the ICraftingPanel interface
public class CombinePanel : MonoBehaviour, ICraftingPanel
{
    
    public GameObject[] slotPanelCombine;
    [SerializeField] private Button buatJamuButton;
    [SerializeField] private Image hasilJamuImage;
    [SerializeField] private Sprite jamuGagalSprite;
    [SerializeField] private Text namaJamuText;

    private JamuIntegration jamuIntegration;

    [SerializeField] private GameObject slotBahanPrefab;
    [SerializeField] private Transform slotBahanContainer;
    [SerializeField] private Transform slotCombineContainer;

    private int selectedBahanIndex = -1;
    private List<string> bahanDipakai = new List<string>();

    // List to hold the recipes from the JamuDatabase
    private List<ResepJamu> daftarResep;
    private List<int> bahanItemIndices = new List<int>();
    private List<GameObject> bahanSlots = new List<GameObject>();
    private GameObject selectedBahan = null;

    private ResepJamu currentCraftedJamu = null;

    public bool NeedsScaleAdjustment => true;

    void Start()
    {
        if (buatJamuButton != null)
        {
            buatJamuButton.onClick.AddListener(BuatJamu);
        }

        jamuIntegration = JamuIntegration.Instance;
        if (jamuIntegration != null)
        {
            jamuIntegration.RegisterCraftingPanel(this);
        }

        InitializeSlots();
        LoadRecipes();

        foreach (GameObject slot in slotPanelCombine)
        {
            Transform slotForCombine = slot.transform.Find("Slot_For_Combine");
            if (slotForCombine != null)
            {
                Image img = slotForCombine.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

    }

    void OnEnable()
    {
        TampilkanDariInventory();
        ResetSlotCombine();
        if (hasilJamuImage != null)
        {
            hasilJamuImage.gameObject.SetActive(false);
        }
        if (namaJamuText != null)
        {
            namaJamuText.gameObject.SetActive(false);
        }
    }

    private void LoadRecipes()
    {
        // Get the JamuDatabase instance
        JamuDatabase jamuDatabase = JamuSystem.Instance.jamuDatabase;
        if (jamuDatabase != null)
        {
            daftarResep = jamuDatabase.resepJamus;
        }
        else
        {
            Debug.LogWarning("JamuDatabase is not set in JamuSystem.");
            daftarResep = new List<ResepJamu>();
        }
    }

    public void ResetSlotCombine()
    {
        if (slotPanelCombine == null || slotPanelCombine.Length == 0)
        {
            Debug.LogWarning("No slot panels available to reset.");
            return;
        }

        foreach (GameObject slot in slotPanelCombine)
        {
            if (slot == null) continue;

            SlotCombine combineData = slot.GetComponent<SlotCombine>();
            if (combineData != null)
            {
                combineData.ClearSlot();
                combineData.SetBahan(null);
            }

            Transform slotForCombineTransform = slot.transform.Find("Slot_For_Combine");
            if (slotForCombineTransform == null)
            {
                Debug.LogWarning($"Slot {slot.name} has no Slot_For_Combine child.");
                continue;
            }

            foreach (Transform child in slotForCombineTransform)
            {
                Destroy(child.gameObject);
            }

            Image img = slotForCombineTransform.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = null;
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        bahanDipakai.Clear();

        if (hasilJamuImage != null)
        {
            hasilJamuImage.gameObject.SetActive(false);
        }
        if (namaJamuText != null)
        {
            namaJamuText.gameObject.SetActive(false);
        }
    }

    public void TampilkanDariInventory()
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (dtg == null)
        {
            Debug.LogError("TampilkanDariInventory: DataGame is null!");
            return;
        }

        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
        {
            Debug.LogError("TampilkanDariInventory: JamuSystem or jamuDatabase is null!");
            return;
        }

        if (JamuIntegration.Instance == null)
        {
            Debug.LogError("TampilkanDariInventory: JamuIntegration is null!");
            return;
        }

        // Clear existing slots
        foreach (GameObject slot in bahanSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        bahanSlots.Clear();
        bahanItemIndices.Clear();

        // Log available bahans for debugging
        List<string> bahanTersedia = JamuIntegration.Instance.GetAvailableBahanNames();
        Debug.Log($"Available bahans: {string.Join(", ", bahanTersedia)}");

        // Create new slots from inventory
        int createdSlots = 0;
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            var item = dtg.barang[i];
            if (item != null && item.jumlah > 0 && item.gambar != null)
            {
                Debug.Log($"Checking inventory item: {item.nama}");

                // Check if this is a jamu ingredient
                if (bahanTersedia.Contains(item.nama))
                {
                    BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(item.nama);
                    if (bahan != null)
                    {
                        Debug.Log($"Creating slot for bahan: {bahan.itemName}");

                        // Create the slot GameObject
                        GameObject slot = Instantiate(slotBahanPrefab, slotBahanContainer);
                        bahanSlots.Add(slot);
                        bahanItemIndices.Add(i);

                        // Set up the visual components
                        Image img = slot.transform.GetChild(0).GetComponent<Image>();
                        Text txt = slot.transform.GetChild(1).GetComponent<Text>();

                        // Verify we have a valid sprite
                        if (bahan.itemSprite == null)
                        {
                            Debug.LogError($"BahanItem {bahan.itemName} has null sprite!");
                            continue;
                        }

                        img.sprite = bahan.itemSprite;
                        img.color = Color.white;
                        txt.text = item.jumlah.ToString();

                        // Set up the SlotBahan component
                        // Set up the SlotBahan component
                        SlotBahan slotBahan = slot.GetComponentInChildren<SlotBahan>();
                        if (slotBahan != null)
                        {
                            slotBahan.SetBahan(bahan);

                            // Verify the BahanItem was assigned correctly
                            BahanItem verifyBahan = slotBahan.GetBahan();
                            Debug.Log($"Verified BahanItem: {(verifyBahan != null ? verifyBahan.itemName : "NULL")}");

                            createdSlots++;
                        }
                        else
                        {
                            Debug.LogError($"Newly created slot {slot.name} has no SlotBahan component!");
                        }

                        // Add click handler for direct selection
                        Button slotButton = slot.GetComponent<Button>();
                        if (slotButton != null)
                        {
                            string namaBahan = item.nama;
                            slotButton.onClick.RemoveAllListeners();
                            slotButton.onClick.AddListener(() => PilihBahan(namaBahan));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"JamuDatabase couldn't find BahanItem for: {item.nama}");
                    }
                }
            }
        }

        Debug.Log($"Created {createdSlots} bahan slots from inventory");
    }


    public void PilihBahan(string namaBahan)
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (dtg == null || string.IsNullOrEmpty(namaBahan)) return;

        int inventoryIndex = dtg.barang.FindIndex(b => b != null && b.nama == namaBahan && b.jumlah > 0);
        if (inventoryIndex < 0)
        {
            Debug.LogWarning("Bahan tidak ditemukan atau jumlah habis: " + namaBahan);
            return;
        }

        if (selectedBahanIndex != inventoryIndex)
        {
            selectedBahanIndex = inventoryIndex;
            Debug.Log("Bahan dipilih: " + namaBahan);
        }
        else
        {
            Debug.Log("Bahan yang sama dipilih kembali: " + namaBahan);
        }
    }





    public void TempatkanBahanKeSlotCombine(int index)
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (selectedBahanIndex == -1 || dtg == null) return;

        if (index < 0 || index >= slotPanelCombine.Length) return;

        Transform slotForCombineTransform = slotPanelCombine[index].transform.Find("Slot_For_Combine");
        if (slotForCombineTransform == null)
        {
            Debug.LogError($"Slot {index} does not have a Slot_For_Combine transform.");
            return;
        }

        // Hapus semua child di dalam Slot_For_Combine sebelum menambahkan yang baru
        foreach (Transform child in slotForCombineTransform)
        {
            Destroy(child.gameObject);
        }

        // Get the bahan name from inventory
        string bahanName = dtg.barang[selectedBahanIndex].nama;
        BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(bahanName);

        // Update SlotCombine component with the bahan name
        SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
        if (slotData != null)
        {
            slotData.SetBahan(bahan); // Use the BahanItem directly
            Debug.Log($"Setting slot {index} with Bahan: {bahan.itemName}");
        }

        // Update the visual representation
        Image targetImage = slotPanelCombine[index].transform.GetChild(0).GetComponent<Image>();
        if (targetImage == null) return;

        targetImage.sprite = dtg.barang[selectedBahanIndex].gambar;
        targetImage.color = Color.white;

        float scaleRatio = PanelScalingUtils.CalculateScaleFactor(slotBahanContainer, slotCombineContainer);
        targetImage.transform.localScale = Vector3.one * scaleRatio;

        // Consume the item from inventory
        dtg.barang[selectedBahanIndex].jumlah--;
        ManagerPP<DataGame>.Set("datagame", dtg);

        selectedBahanIndex = -1;
    }

    public void BuatJamu()
    {
        Debug.Log("==== Slot Status ====");
        for (int i = 0; i < slotPanelCombine.Length; i++)
        {
            SlotCombine combineData = slotPanelCombine[i].GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            Debug.Log($"Slot {i}: {(bahan != null ? bahan.itemName : "NULL")}");
        }
        Debug.Log("====================");

        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
        {
            Debug.LogError("JamuSystem atau jamuDatabase tidak tersedia!");
            return;
        }

        // Clear the list before populating it
        bahanDipakai.Clear();

        // Collect all ingredients from the slots
        foreach (GameObject slot in slotPanelCombine)
        {
            SlotCombine combineData = slot.GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            if (bahan != null)
            {
                bahanDipakai.Add(bahan.itemName);
                Debug.Log($"Bahan digunakan: {bahan.itemName}");
            }
            else
            {
                Debug.LogWarning($"Slot: {slot.name} is empty or has no valid BahanItem.");
            }
        }

        if (bahanDipakai.Count == 0)
        {
            Debug.LogWarning("Tidak ada bahan yang digunakan untuk membuat jamu.");
            return;
        }

        bool resepCocok = false;
        ResepJamu resepBerhasil = null;

        Debug.Log($"Checking {JamuSystem.Instance.jamuDatabase.resepJamus.Count} recipes with {bahanDipakai.Count} ingredients used");

        foreach (ResepJamu resep in JamuSystem.Instance.jamuDatabase.resepJamus)
        {
            if (resep.bahanResep.Length != bahanDipakai.Count)
            {
                Debug.Log($"Skipping recipe {resep.jamuName}: ingredient count mismatch ({resep.bahanResep.Length} vs {bahanDipakai.Count})");
                continue;
            }

            // Sort both lists before comparison
            List<string> sortedRecipeIngredients = new List<string>(resep.bahanResep);
            sortedRecipeIngredients.Sort();

            List<string> sortedBahanDipakai = new List<string>(bahanDipakai);
            sortedBahanDipakai.Sort();

            bool semuaBahanCocok = sortedRecipeIngredients.SequenceEqual(sortedBahanDipakai);

            Debug.Log($"Recipe {resep.jamuName} matches: {semuaBahanCocok}");
            Debug.Log($"Recipe ingredients: {string.Join(", ", sortedRecipeIngredients)}");
            Debug.Log($"Used ingredients: {string.Join(", ", sortedBahanDipakai)}");

            if (semuaBahanCocok)
            {
                resepCocok = true;
                resepBerhasil = resep;
                break;
            }
        }

        // Pastikan UI elemen untuk hasil jamu terlihat
        if (hasilJamuImage != null) hasilJamuImage.gameObject.SetActive(true);
        if (namaJamuText != null) namaJamuText.gameObject.SetActive(true);

        if (resepCocok && resepBerhasil != null)
        {
            // Save the current crafted jamu
            currentCraftedJamu = resepBerhasil;

            // Tampilkan hasil jamu yang berhasil dibuat
            hasilJamuImage.sprite = resepBerhasil.jamuSprite;
            namaJamuText.text = resepBerhasil.jamuName;
            Debug.Log($"Jamu berhasil dibuat: {resepBerhasil.jamuName}");

            // Tambahkan jamu ke inventory melalui JamuIntegration
            if (jamuIntegration != null)
            {
                jamuIntegration.AddJamuToInventory(resepBerhasil);
            }
            else
            {
                Debug.LogError("JamuIntegration tidak tersedia.");
                // Gunakan alternatif lain jika jamuIntegration tidak tersedia
                TambahJamuKeInventory(resepBerhasil);
            }

            // Tambahkan jamu ke almanac
            if (AlmanacSystem.Instance != null)
            {
                Debug.Log($"Adding jamu to almanac: {resepBerhasil.jamuName}");
                bool added = AlmanacSystem.Instance.DiscoverJamu(resepBerhasil.jamuName);
                if (added)
                {
                    Debug.Log($"Jamu {resepBerhasil.jamuName} successfully added to almanac");
                }
                else
                {
                    Debug.LogWarning($"Failed to add jamu {resepBerhasil.jamuName} to almanac - already discovered");
                }
            }
            else
            {
                Debug.LogError("AlmanacSystem.Instance is null! Cannot add jamu to almanac.");
                // Coba dapatkan instance AlmanacSystem lagi
                AlmanacSystem almanac = FindAnyObjectByType<AlmanacSystem>();
                if (almanac != null)
                {
                    almanac.DiscoverJamu(resepBerhasil.jamuName);
                }
            }
        }
        else
        {
            // Reset current crafted jamu if failed
            currentCraftedJamu = null;

            // Tampilkan hasil jamu yang gagal
            hasilJamuImage.sprite = jamuGagalSprite;
            namaJamuText.text = "Jamu tidak diketahui!";
            Debug.LogWarning("Jamu gagal dibuat karena tidak ditemukan resep yang cocok.");
        }

        // Tunggu beberapa detik sebelum reset slot combine
        // Jangan langsung reset slot, biarkan hasil terlihat
        Invoke("ResetSlotCombineDelayed", 5.0f);
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < slotPanelCombine.Length; i++)
        {
            SlotCombine slotComponent = slotPanelCombine[i].GetComponent<SlotCombine>();
            if (slotComponent == null)
            {
                slotComponent = slotPanelCombine[i].AddComponent<SlotCombine>();
            }
            slotComponent.slotIndex = i;
        }
        Debug.Log($"Initialized {slotPanelCombine.Length} combine slots");
    }

    // New method to return the current crafted Jamu
    public ResepJamu GetCurrentCraftedJamu()
    {
        return currentCraftedJamu;
    }

    // Method bantuan untuk reset slot dengan delay
    public void CompleteReset()
    {
        Debug.Log("Starting thorough reset of the combine panel...");

        foreach (GameObject slot in slotPanelCombine)
        {
            if (slot == null) continue;

            // Reset data di komponen SlotCombine
            SlotCombine slotData = slot.GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(null);
            }

            // Hapus semua isi dari Slot_For_Combine
            Transform slotForCombine = slot.transform.Find("Slot_For_Combine");
            if (slotForCombine != null)
            {
                foreach (Transform child in slotForCombine)
                {
                    Destroy(child.gameObject);
                }

                // Reset image background-nya
                Image slotImage = slotForCombine.GetComponent<Image>();
                if (slotImage != null)
                {
                    slotImage.sprite = null;
                    slotImage.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        // Reset data penggunaan bahan
        bahanDipakai.Clear();

        // Reset UI hasil jamu
        if (hasilJamuImage != null)
        {
            hasilJamuImage.gameObject.SetActive(false);
        }

        if (namaJamuText != null)
        {
            namaJamuText.text = string.Empty;
            namaJamuText.gameObject.SetActive(false);
        }

        Debug.Log("Thorough reset completed successfully.");
    }



    // Method bantuan untuk reset slot dengan delay
    private void ResetSlotCombineDelayed()
    {
        CompleteReset();

        // Wait a short time before refreshing the inventory
        StartCoroutine(UpdateInventoryAfterDelay(0.1f));
    }

    private IEnumerator UpdateInventoryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Update UI to reflect changes in inventory
        TampilkanDariInventory();

        Debug.Log("Inventory display updated after reset.");
    }

    private void TambahJamuKeInventory(ResepJamu resep)
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (dtg == null) return;

        bool jamuDitambahkan = false;

        for (int i = 0; i < dtg.barang.Count; i++)
        {
            if (dtg.barang[i] == null)
            {
                // Implementasi penambahan jamu baru sesuai sistem Anda
                jamuDitambahkan = true;
                break;
            }
            else if (dtg.barang[i].gambar != null && dtg.barang[i].gambar.name == resep.jamuSprite.name)
            {
                dtg.barang[i].jumlah++;
                jamuDitambahkan = true;
                break;
            }
        }

        if (!jamuDitambahkan)
        {
            Debug.LogWarning("Inventory penuh, tidak bisa menambah jamu!");
        }
    }
}
