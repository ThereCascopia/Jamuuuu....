using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;

public enum CraftingPanelType
{
    CombinePanel,
    NPCCraftingPanel
}

// Unified panel that can function as either a CombinePanel or NPCCraftingPanel
public class CraftingManager : MonoBehaviour, ICraftingPanel
{
    [Header("Panel Configuration")]
    [SerializeField] private CraftingPanelType panelType = CraftingPanelType.CombinePanel;

    [Header("Common UI Elements")]
    public GameObject[] slotPanelCombine;
    [SerializeField] private Button buatJamuButton;
    [SerializeField] private Image hasilJamuImage;
    [SerializeField] private Sprite jamuGagalSprite;
    [SerializeField] private Text namaJamuText;

    [Header("Bahan and Slots")]
    [SerializeField] private GameObject slotBahanPrefab;
    [SerializeField] private Transform slotBahanContainer;
    [SerializeField] private Transform slotCombineContainer;

    [Header("NPC Mode Only")]
    [SerializeField] private Button kasihNPCButton;
    [SerializeField] private Button closePanelButton;

    [Header("Crafting Panels")]
    [SerializeField] private RectTransform combinePanel;
    [SerializeField] private RectTransform npcCraftingPanel;

    [Header("Slot References")]
    [SerializeField] private List<RectTransform> combineSlots = new List<RectTransform>();
    [SerializeField] private List<RectTransform> npcCraftingSlots = new List<RectTransform>();

    [Header("Debugging")]
    [SerializeField] private bool debugMode = false;

    private Canvas combinePanelCanvas;
    private Canvas npcCraftingPanelCanvas;

    private JamuIntegration jamuIntegration;
    private JamuNPC currentNPC;
    private int selectedBahanIndex = -1;
    private List<string> bahanDipakai = new List<string>();
    private List<ResepJamu> daftarResep;
    private List<int> bahanItemIndices = new List<int>();
    private List<GameObject> bahanSlots = new List<GameObject>();
    private GameObject selectedBahan = null;
    private ResepJamu currentCraftedJamu = null;

    // Property for the interface to determine if scaling is needed
    public bool NeedsScaleAdjustment => panelType == CraftingPanelType.CombinePanel;

    private void Awake()
    {
        // Find canvases on initialization
        FindCanvasReferences();
    }

    void Start()
    {
        // Common initialization
        if (buatJamuButton != null)
        {
            buatJamuButton.onClick.AddListener(BuatJamu);
        }

        // Panel-specific initialization
        if (panelType == CraftingPanelType.CombinePanel)
        {
            InitializeAsCombinePanel();
        }
        else
        {
            InitializeAsNPCCraftingPanel();
        }

        InitializeSlots();
        LoadRecipes();

        // Initialize slot images
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
            else
            {
                // Assume direct child image for NPCCraftingPanel
                Image img = slot.transform.GetChild(0).GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = new Color(1f, 1f, 1f, 0f);
                }
            }
        }

        if (debugMode)
        {
            LogPanelInformation();
        }
    }

    private void FindCanvasReferences()
    {
        if (combinePanel != null)
        {
            combinePanelCanvas = PanelScalingUtils.FindRootCanvas(combinePanel);
        }

        if (npcCraftingPanel != null)
        {
            npcCraftingPanelCanvas = PanelScalingUtils.FindRootCanvas(npcCraftingPanel);
        }
    }

    private void LogPanelInformation()
    {
        if (combinePanelCanvas != null)
        {
            Debug.Log($"CombinePanel Canvas: {combinePanelCanvas.name}, Render Mode: {combinePanelCanvas.renderMode}");
        }
        else
        {
            Debug.LogWarning("CombinePanel Canvas not found!");
        }

        if (npcCraftingPanelCanvas != null)
        {
            Debug.Log($"NPCCraftingPanel Canvas: {npcCraftingPanelCanvas.name}, Render Mode: {npcCraftingPanelCanvas.renderMode}");
        }
        else
        {
            Debug.LogWarning("NPCCraftingPanel Canvas not found!");
        }
    }

    public void MoveItemBetweenPanels(GameObject craftingItem, RectTransform sourceSlot, RectTransform destinationSlot)
    {
        if (craftingItem == null || sourceSlot == null || destinationSlot == null)
        {
            Debug.LogError("MoveItemBetweenPanels: Missing parameters!");
            return;
        }

        // Cache the original parent and world position
        Transform originalParent = craftingItem.transform.parent;
        Vector3 worldPos = craftingItem.transform.position;

        // Change parent and maintain world position
        craftingItem.transform.SetParent(destinationSlot);
        craftingItem.transform.position = worldPos;

        // Reset local position to center of the slot
        craftingItem.transform.localPosition = Vector3.zero;

        // Handle different canvas scaling
        AdjustItemScaling(craftingItem, originalParent, destinationSlot);

        if (debugMode)
        {
            Debug.Log($"Moved {craftingItem.name} from {sourceSlot.name} to {destinationSlot.name}");
        }
    }

    // Handle scaling differences between different canvas types
    private void AdjustItemScaling(GameObject item, Transform sourceParent, Transform destinationParent)
    {
        Canvas sourceCanvas = PanelScalingUtils.FindRootCanvas(sourceParent);
        Canvas destCanvas = PanelScalingUtils.FindRootCanvas(destinationParent);

        if (sourceCanvas == null || destCanvas == null)
        {
            Debug.LogWarning("AdjustItemScaling: Cannot find source or destination canvas!");
            return;
        }

        // Only adjust scale if source and destination canvases are different
        if (sourceCanvas != destCanvas)
        {
            // Different handling based on canvas render modes
            if (sourceCanvas.renderMode == RenderMode.WorldSpace && destCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // Moving from WorldSpace to ScreenSpaceCamera
                AdjustWorldToScreenSpaceScale(item, sourceParent, destinationParent);
            }
            else if (sourceCanvas.renderMode == RenderMode.ScreenSpaceCamera && destCanvas.renderMode == RenderMode.WorldSpace)
            {
                // Moving from ScreenSpaceCamera to WorldSpace
                AdjustScreenSpaceToWorldScale(item, sourceParent, destinationParent);
            }
            else
            {
                // Default handling for other canvas combinations
                PanelScalingUtils.AdjustScaleForPanel(item, sourceParent, destinationParent);
            }
        }
        else if (debugMode)
        {
            Debug.Log("Same canvas detected, no scaling adjustment needed.");
        }
    }

    // Special handling for WorldSpace to ScreenSpaceCamera transitions
    private void AdjustWorldToScreenSpaceScale(GameObject item, Transform sourceParent, Transform destinationParent)
    {
        // Use PanelScalingUtils but apply special considerations for WorldSpace to ScreenSpace
        Vector3 scaleFactorVec = PanelScalingUtils.CalculateScaleFactorVec3(sourceParent, destinationParent);

        // Get the Canvas Scaler if available for more accurate scaling
        CanvasScaler destCanvasScaler = PanelScalingUtils.FindRootCanvas(destinationParent)?.GetComponent<CanvasScaler>();
        if (destCanvasScaler != null && destCanvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // Apply additional scaling factor based on reference resolution and screen size
            float referenceAspect = destCanvasScaler.referenceResolution.x / destCanvasScaler.referenceResolution.y;
            float currentAspect = (float)Screen.width / Screen.height;
            float aspectRatio = currentAspect / referenceAspect;

            if (destCanvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                float matchFactor = destCanvasScaler.matchWidthOrHeight;
                float scaleFactor = Mathf.Lerp(aspectRatio, 1f, matchFactor);
                scaleFactorVec *= scaleFactor;
            }
        }

        // Apply the calculated scale
        item.transform.localScale = Vector3.Scale(item.transform.localScale, scaleFactorVec);

        if (debugMode)
        {
            Debug.Log($"Adjusted scale for WorldSpace to ScreenSpace: {scaleFactorVec}");
        }
    }

    // Special handling for ScreenSpaceCamera to WorldSpace transitions
    private void AdjustScreenSpaceToWorldScale(GameObject item, Transform sourceParent, Transform destinationParent)
    {
        // For ScreenSpace to WorldSpace, we often need to scale down significantly
        Vector3 scaleFactorVec = PanelScalingUtils.CalculateScaleFactorVec3(sourceParent, destinationParent);

        // Get the Canvas Scaler if available
        CanvasScaler sourceCanvasScaler = PanelScalingUtils.FindRootCanvas(sourceParent)?.GetComponent<CanvasScaler>();
        if (sourceCanvasScaler != null && sourceCanvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // Apply inverse of the scaling used in the opposite direction
            float referenceAspect = sourceCanvasScaler.referenceResolution.x / sourceCanvasScaler.referenceResolution.y;
            float currentAspect = (float)Screen.width / Screen.height;
            float aspectRatio = currentAspect / referenceAspect;

            if (sourceCanvasScaler.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight)
            {
                float matchFactor = sourceCanvasScaler.matchWidthOrHeight;
                float scaleFactor = Mathf.Lerp(aspectRatio, 1f, matchFactor);
                scaleFactorVec /= scaleFactor;
            }
        }

        // Apply the calculated scale
        item.transform.localScale = Vector3.Scale(item.transform.localScale, scaleFactorVec);

        if (debugMode)
        {
            Debug.Log($"Adjusted scale for ScreenSpace to WorldSpace: {scaleFactorVec}");
        }
    }

    // Helper method to find a slot in the combine panel by index
    public RectTransform GetCombineSlot(int index)
    {
        if (index >= 0 && index < combineSlots.Count)
        {
            return combineSlots[index];
        }
        return null;
    }

    // Helper method to find a slot in the NPC crafting panel by index
    public RectTransform GetNPCCraftingSlot(int index)
    {
        if (index >= 0 && index < npcCraftingSlots.Count)
        {
            return npcCraftingSlots[index];
        }
        return null;
    }

    public void TransferToCombinePanel(GameObject craftingItem, int sourceSlotIndex, int destSlotIndex)
    {
        RectTransform sourceSlot = GetNPCCraftingSlot(sourceSlotIndex);
        RectTransform destSlot = GetCombineSlot(destSlotIndex);

        if (sourceSlot != null && destSlot != null)
        {
            MoveItemBetweenPanels(craftingItem, sourceSlot, destSlot);
        }
        else
        {
            Debug.LogError($"Cannot find slots: sourceIndex={sourceSlotIndex}, destIndex={destSlotIndex}");
        }
    }

    // Example method to transfer an item from combine to NPC crafting panel
    public void TransferToNPCCraftingPanel(GameObject craftingItem, int sourceSlotIndex, int destSlotIndex)
    {
        RectTransform sourceSlot = GetCombineSlot(sourceSlotIndex);
        RectTransform destSlot = GetNPCCraftingSlot(destSlotIndex);

        if (sourceSlot != null && destSlot != null)
        {
            MoveItemBetweenPanels(craftingItem, sourceSlot, destSlot);
        }
        else
        {
            Debug.LogError($"Cannot find slots: sourceIndex={sourceSlotIndex}, destIndex={destSlotIndex}");
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

        if (panelType == CraftingPanelType.NPCCraftingPanel)
        {
            transform.localScale = Vector3.one; // Fixed scale for NPC panel
        }
    }

    private void InitializeAsCombinePanel()
    {
        jamuIntegration = JamuIntegration.Instance;
        if (jamuIntegration != null)
        {
            jamuIntegration.RegisterCraftingPanel(this);
        }

        // Hide NPC-specific buttons
        if (kasihNPCButton != null)
            kasihNPCButton.gameObject.SetActive(false);
        if (closePanelButton != null)
            closePanelButton.gameObject.SetActive(false);
    }

    private void InitializeAsNPCCraftingPanel()
    {
        // Initialize NPC-specific components
        if (kasihNPCButton != null)
            kasihNPCButton.onClick.AddListener(KasihJamuKeNPC);
        if (closePanelButton != null)
            closePanelButton.onClick.AddListener(() => gameObject.SetActive(false));

        gameObject.SetActive(false); // Initially not active
    }

    // Method to connect with NPC (only used in NPC mode)
    public void Initialize(JamuNPC npc)
    {
        if (panelType != CraftingPanelType.NPCCraftingPanel)
        {
            Debug.LogWarning("Attempted to initialize with NPC but panel is not in NPC mode!");
            return;
        }

        currentNPC = npc;
        Debug.Log("UnifiedCraftingPanel terhubung dengan NPC: " + npc.name);

        gameObject.SetActive(true);
        TampilkanDariInventory();
        ResetSlotCombine();
    }

    private void LoadRecipes()
    {
        // Get the JamuDatabase instance
        if (JamuSystem.Instance != null && JamuSystem.Instance.jamuDatabase != null)
        {
            daftarResep = JamuSystem.Instance.jamuDatabase.resepJamus;
        }
        else
        {
            Debug.LogWarning("JamuDatabase is not set in JamuSystem.");
            daftarResep = new List<ResepJamu>();
        }
    }

    private string GetBahanFromDatabase(string bahanName)
    {
        var jamuDb = JamuSystem.Instance?.jamuDatabase;
        if (jamuDb != null)
        {
            BahanItem bahanItem = jamuDb.GetBahan(bahanName);
            if (bahanItem != null)
            {
                return bahanItem.itemName;
            }
        }
        return null;
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

            // Handle different structure based on panel type
            if (panelType == CraftingPanelType.CombinePanel)
            {
                Transform slotForCombineTransform = slot.transform.Find("Slot_For_Combine");
                if (slotForCombineTransform != null)
                {
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
            }
            else
            {
                // NPCCraftingPanel structure
                Image img = slot.transform.GetChild(0).GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = new Color(1f, 1f, 1f, 0f);
                }
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

        // Get available bahans
        List<string> bahanTersedia = JamuIntegration.Instance.GetAvailableBahanNames();
        Debug.Log($"Available bahans: {string.Join(", ", bahanTersedia)}");

        // Create new slots from inventory
        int createdSlots = 0;
        for (int i = 0; i < dtg.barang.Count; i++)
        {
            var item = dtg.barang[i];
            if (item != null && item.jumlah > 0 && bahanTersedia.Contains(item.nama))
            {
                // Create the slot GameObject
                GameObject slot = Instantiate(slotBahanPrefab, slotBahanContainer);
                bahanSlots.Add(slot);
                bahanItemIndices.Add(i);

                // Set up the visual components
                Image img = slot.transform.GetChild(0).GetComponent<Image>();
                Text txt = slot.transform.GetChild(1).GetComponent<Text>();

                BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    // For CombinePanel, we use the BahanItem sprite
                    img.sprite = panelType == CraftingPanelType.CombinePanel ? bahan.itemSprite : item.gambar;
                    img.color = Color.white;
                    txt.text = item.jumlah.ToString();

                    // Set up the SlotBahan component
                    SlotBahan slotBahan = slot.GetComponentInChildren<SlotBahan>();
                    if (slotBahan != null)
                    {
                        slotBahan.SetBahan(bahan);
                        createdSlots++;
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

        // Get the bahan name from inventory
        string bahanName = dtg.barang[selectedBahanIndex].nama;
        BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(bahanName);

        if (bahan == null)
        {
            Debug.LogError($"BahanItem not found for: {bahanName}");
            return;
        }

        // Different handling based on panel type
        if (panelType == CraftingPanelType.CombinePanel)
        {
            Transform slotForCombineTransform = slotPanelCombine[index].transform.Find("Slot_For_Combine");
            if (slotForCombineTransform == null)
            {
                Debug.LogError($"Slot {index} does not have a Slot_For_Combine transform.");
                return;
            }

            // Clear existing content
            foreach (Transform child in slotForCombineTransform)
            {
                Destroy(child.gameObject);
            }

            // Update SlotCombine component
            SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(bahan);
                Debug.Log($"Setting slot {index} with Bahan: {bahan.itemName}");
            }

            // Update visual
            Image targetImage = slotForCombineTransform.GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = bahan.itemSprite;
                targetImage.color = Color.white;
            }
        }
        else
        {
            // NPCCraftingPanel style
            SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
            if (slotData != null)
            {
                slotData.SetBahan(bahan);
            }

            Image targetImage = slotPanelCombine[index].transform.GetChild(0).GetComponent<Image>();
            if (targetImage != null)
            {
                targetImage.sprite = dtg.barang[selectedBahanIndex].gambar;
                targetImage.color = Color.white;

                float scaleRatio = PanelScalingUtils.CalculateScaleFactor(slotBahanContainer, slotCombineContainer);
                targetImage.transform.localScale = Vector3.one * scaleRatio;
            }
        }

        // Consume the item from inventory
        dtg.barang[selectedBahanIndex].jumlah--;
        ManagerPP<DataGame>.Set("datagame", dtg);
        selectedBahanIndex = -1;
    }

    public void BuatJamu()
    {
        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
        {
            Debug.LogError("JamuSystem atau jamuDatabase tidak tersedia!");
            return;
        }

        // Log slot status
        Debug.Log("==== Slot Status ====");
        for (int i = 0; i < slotPanelCombine.Length; i++)
        {
            SlotCombine combineData = slotPanelCombine[i].GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            Debug.Log($"Slot {i}: {(bahan != null ? bahan.itemName : "NULL")}");
        }

        // Clear and collect ingredients from slots
        bahanDipakai.Clear();
        foreach (GameObject slot in slotPanelCombine)
        {
            SlotCombine combineData = slot.GetComponent<SlotCombine>();
            BahanItem bahan = combineData?.GetBahan();
            if (bahan != null)
            {
                bahanDipakai.Add(bahan.itemName);
                Debug.Log($"Bahan digunakan: {bahan.itemName}");
            }
        }

        if (bahanDipakai.Count == 0)
        {
            Debug.LogWarning("Tidak ada bahan yang digunakan untuk membuat jamu.");
            return;
        }

        // Find matching recipe
        ResepJamu resepBerhasil = null;
        Debug.Log($"Checking {daftarResep.Count} recipes with {bahanDipakai.Count} ingredients used");

        foreach (ResepJamu resep in daftarResep)
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
                resepBerhasil = resep;
                break;
            }
        }

        // Show result UI
        if (hasilJamuImage != null) hasilJamuImage.gameObject.SetActive(true);
        if (namaJamuText != null) namaJamuText.gameObject.SetActive(true);

        if (resepBerhasil != null)
        {
            // Save the current crafted jamu
            currentCraftedJamu = resepBerhasil;

            // Display successful jamu
            hasilJamuImage.sprite = resepBerhasil.jamuSprite;
            namaJamuText.text = resepBerhasil.jamuName;
            Debug.Log($"Jamu berhasil dibuat: {resepBerhasil.jamuName}");

            // For CombinePanel, add to inventory
            if (panelType == CraftingPanelType.CombinePanel)
            {
                if (jamuIntegration != null)
                {
                    jamuIntegration.AddJamuToInventory(resepBerhasil);
                }
                else
                {
                    Debug.LogError("JamuIntegration tidak tersedia.");
                    TambahJamuKeInventory(resepBerhasil);
                }

                // Add to almanac
                if (AlmanacSystem.Instance != null)
                {
                    Debug.Log($"Adding jamu to almanac: {resepBerhasil.jamuName}");
                    bool added = AlmanacSystem.Instance.DiscoverJamu(resepBerhasil.jamuName);
                    if (added)
                    {
                        Debug.Log($"Jamu {resepBerhasil.jamuName} successfully added to almanac");
                    }
                }
            }
        }
        else
        {
            // Reset current crafted jamu if failed
            currentCraftedJamu = null;

            // Display failed jamu
            hasilJamuImage.sprite = jamuGagalSprite;
            namaJamuText.text = "Jamu tidak diketahui!";
            Debug.LogWarning("Jamu gagal dibuat karena tidak ditemukan resep yang cocok.");
        }

        // For CombinePanel, automatically reset after delay
        if (panelType == CraftingPanelType.CombinePanel)
        {
            Invoke("ResetSlotCombineDelayed", 5.0f);
        }
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

    // Method to return the current crafted Jamu
    public ResepJamu GetCurrentCraftedJamu()
    {
        return currentCraftedJamu;
    }

    // NPC specific method
    public void KasihJamuKeNPC()
    {
        if (panelType != CraftingPanelType.NPCCraftingPanel)
        {
            Debug.LogWarning("KasihJamuKeNPC called but panel is not in NPC mode!");
            return;
        }

        if (currentCraftedJamu == null)
        {
            Debug.LogWarning("Belum membuat jamu yang valid untuk diberikan!");
            return;
        }

        if (currentNPC != null)
        {
            currentNPC.GiveJamuToNPC(currentCraftedJamu);
            currentCraftedJamu = null;
            ResetSlotCombine();
            gameObject.SetActive(false); // Tutup panel setelah kasih
        }
        else
        {
            Debug.LogError("Tidak ada NPC yang sedang terhubung!");
        }
    }

    // Method for resetting with delay (for CombinePanel)
    private void ResetSlotCombineDelayed()
    {
        CompleteReset();
        StartCoroutine(UpdateInventoryAfterDelay(0.1f));
    }

    // Method for thorough reset
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

            if (panelType == CraftingPanelType.CombinePanel)
            {
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
            else
            {
                // NPCCraftingPanel structure
                Image img = slot.transform.GetChild(0).GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.color = new Color(1f, 1f, 1f, 0f);
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

    private IEnumerator UpdateInventoryAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
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