using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class AlmanacSystem : MonoBehaviour
{
    private static AlmanacSystem _instance;
    public static AlmanacSystem Instance => _instance;

    public JamuSystem jamuSystem;

    [System.Serializable]
    public class AlmanacItemData
    {
        public string nama;
        [TextArea] public string manfaat;
        public string tipe; // "Rempah" atau "Jamu"
        public Sprite gambar;
        public bool ditemukan = false;
    }

    // No separate data almanac - all items dynamically created from JamuSystem
    private Dictionary<string, AlmanacItemData> discoveredItems = new Dictionary<string, AlmanacItemData>();

    [Header("UI Almanac")]
    public GameObject almanacPanel;
    public Button btnTutup;

    [Header("Tabs")]
    public Button btnRempahTab;
    public Button btnJamuTab;

    [Header("Content")]
    public Transform entriesContainerRempah;
    public Transform entriesContainerJamu;

    // Akan di-set sesuai tipe "Rempah" atau "Jamu"
    private Transform entriesContainer; // Container for item buttons (Grid Layout Group)
    public GameObject itemButtonPrefab; // Prefab for each item button

    [Header("Navigation")]
    public Button btnNext;
    public Button btnPrev;
    public int itemsPerPage = 6;

    [Header("Detail Panel")]
    public GameObject detailPanel;
    public Image detailImage;
    public Text detailNamaText;
    public Text detailManfaatText;

    // Runtime variables
    private int currentPage = 0;
    private string currentFilter = "Rempah"; // "Rempah" atau "Jamu"
    private List<AlmanacItemData> filteredItems = new List<AlmanacItemData>();
    private List<GameObject> instantiatedButtons = new List<GameObject>();
    private bool isAlmanacDataRefreshed = false;

    // Animation variables
    private Coroutine detailImageAnimationCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Get JamuSystem reference if not assigned
        if (jamuSystem == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null)
            {
                Debug.LogWarning("JamuSystem reference not found! Will try again later.");
                // We'll try to get it again when needed
            }
            else
            {
                Debug.Log("JamuSystem reference found");
            }
        }

        InitUI();
        LoadDiscoveredItemsData();

        if (discoveredItems.Count == 0)
        {
            Debug.Log("No items found in almanac, refreshing from JamuSystem");
            RefreshAlmanacDataFromJamuSystem();
        }

        // Default: hide panels at start
        almanacPanel.SetActive(false);
        detailPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Klik terjadi di luar area UI almanak.");
        }
    }

    private void InitUI()
    {
        // Setup event listeners
        btnTutup.onClick.AddListener(CloseAlmanac);
        btnRempahTab.onClick.AddListener(() => SetFilter("Rempah"));
        btnJamuTab.onClick.AddListener(() => SetFilter("Jamu"));

        btnNext.onClick.AddListener(NextPage);
        btnPrev.onClick.AddListener(PrevPage);

        // Set default selected tab
        UpdateTabVisuals("Rempah");
    }

    // Public methods for opening the almanac
    public void OpenAlmanac()
    {
        // Refresh data from JamuSystem in case there are new items
        RefreshAlmanacDataFromJamuSystem();

        almanacPanel.SetActive(true);
        
        currentPage = 0;
        SetFilter("Rempah"); // Default to showing Rempah items
    }

    public void CloseAlmanac()
    {
        almanacPanel.SetActive(false);
        CloseDetailPanel();
    }

    private void CloseDetailPanel()
    {
        // Stop any running animations
        if (detailImageAnimationCoroutine != null)
        {
            StopCoroutine(detailImageAnimationCoroutine);
            detailImageAnimationCoroutine = null;
        }

        // Reset any animated properties
        if (detailImage != null)
        {
            detailImage.transform.localScale = Vector3.one;
        }

        detailPanel.SetActive(false);
    }

    // Filter items based on type
    public void SetFilter(string tipe)
    {
        currentFilter = tipe;
        currentPage = 0; // Reset page to first
        UpdateTabVisuals(tipe);
        RefreshItemDisplay();
        SetEntriesContainerByType(tipe);

        // Only close detail panel if it's active
        if (detailPanel.activeSelf)
        {
            CloseDetailPanelAnimation();
        }
    }

    private void SetEntriesContainerByType(string tipe)
    {
        if (tipe == "Rempah")
        {
            entriesContainerRempah.gameObject.SetActive(true);
            entriesContainerJamu.gameObject.SetActive(false);
            entriesContainer = entriesContainerRempah;
        }
        else if (tipe == "Jamu")
        {
            entriesContainerRempah.gameObject.SetActive(false);
            entriesContainerJamu.gameObject.SetActive(true);
            entriesContainer = entriesContainerJamu;
        }
    }

    void UpdateTabVisuals(string selectedTab)
    {
        Color activeColor = new Color(1f, 0.8f, 0.4f); // Warm yellow for active tab
        Color inactiveColor = new Color(0.8f, 0.6f, 0.4f); // Desaturated for inactive tab

        // Set tab button colors
        if (btnRempahTab.GetComponent<Image>() != null)
        {
            btnRempahTab.GetComponent<Image>().color = selectedTab == "Rempah" ? activeColor : inactiveColor;
        }

        if (btnJamuTab.GetComponent<Image>() != null)
        {
            btnJamuTab.GetComponent<Image>().color = selectedTab == "Jamu" ? activeColor : inactiveColor;
        }
    }

    void RefreshItemDisplay()
    {
        // Clear existing buttons
        foreach (var button in instantiatedButtons)
        {
            Destroy(button);
        }
        instantiatedButtons.Clear();

        // Filter items based on current filter
        filteredItems.Clear();
        foreach (var item in discoveredItems.Values)
        {
            if (item.tipe == currentFilter && item.ditemukan)
            {
                filteredItems.Add(item);
            }
        }

        // Calculate pagination
        int totalPages = Mathf.CeilToInt((float)filteredItems.Count / itemsPerPage);
        if (currentPage >= totalPages && totalPages > 0)
        {
            currentPage = totalPages - 1;
        }

        // Update navigation buttons
        btnNext.interactable = (currentPage < totalPages - 1);
        btnPrev.interactable = (currentPage > 0);

        // Display items for current page
        int startIndex = currentPage * itemsPerPage;
        int itemsToShow = Mathf.Min(itemsPerPage, filteredItems.Count - startIndex);

        for (int i = 0; i < itemsToShow; i++)
        {
            int itemIndex = startIndex + i;
            if (itemIndex < filteredItems.Count)
            {
                CreateItemButton(filteredItems[itemIndex]);
            }
        }
    }

    void CreateItemButton(AlmanacItemData item)
    {
        GameObject buttonObj = Instantiate(itemButtonPrefab, entriesContainer);
        instantiatedButtons.Add(buttonObj);

        // Add the AlmanacItemButton component for animations
        AlmanacItemButton itemButton = buttonObj.GetComponent<AlmanacItemButton>();
        if (itemButton == null)
        {
            itemButton = buttonObj.AddComponent<AlmanacItemButton>();
        }

        // Set item data to the button
        itemButton.SetItemData(item);

        // Make sure the button has a valid clickable configuration
        Button btn = buttonObj.GetComponent<Button>();
        if (btn == null)
        {
            btn = buttonObj.AddComponent<Button>();
        }

        // Ensure we have a target graphic for the button
        Image buttonImage = buttonObj.GetComponentInChildren<Image>();
        if (buttonImage != null && btn.targetGraphic == null)
        {
            btn.targetGraphic = buttonImage;
        }

        // Add explicit onClick handler as backup
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ShowItemDetail(item));
    }

    // Public method for showing item details (called from AlmanacItemButton)
    public void ShowItemDetail(AlmanacItemData item)
    {
        detailPanel.SetActive(true);

        // Set detail panel content
        detailImage.sprite = item.gambar;
        detailImage.preserveAspect = true;
        detailNamaText.text = item.nama;
        detailManfaatText.text = item.manfaat;

        // Animate panel opening using native Unity
        StartDetailPanelAnimation();
    }

    private void StartDetailPanelAnimation()
    {
        // Initial scale for opening animation
        detailPanel.transform.localScale = Vector3.zero;

        // Start panel opening animation
        StartCoroutine(ScaleAnimation(detailPanel.transform, Vector3.one, 0.3f));

        // Start image pulsating animation
        if (detailImageAnimationCoroutine != null)
        {
            StopCoroutine(detailImageAnimationCoroutine);
        }
        detailImageAnimationCoroutine = StartCoroutine(PulsateAnimation(detailImage.transform, 1.05f, 0.5f));
    }

    private System.Collections.IEnumerator ScaleAnimation(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // Use easeOutBack effect (overshoot and settle back)
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float progress = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);

            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private System.Collections.IEnumerator PulsateAnimation(Transform target, float maxScale, float duration)
    {
        Vector3 baseScale = Vector3.one;
        Vector3 targetScale = new Vector3(maxScale, maxScale, 1);

        while (true) // Loop until explicitly stopped
        {
            // Scale up
            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
                target.localScale = Vector3.Lerp(baseScale, targetScale, t);
                yield return null;
            }

            // Scale down
            time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / duration));
                target.localScale = Vector3.Lerp(targetScale, baseScale, t);
                yield return null;
            }
        }
    }

    private void CloseDetailPanelAnimation()
    {
        // Check if the detail panel is active before trying to animate it
        if (!detailPanel.activeSelf)
        {
            // If it's already inactive, no need to animate
            return;
        }

        // Start panel closing animation
        StartCoroutine(ScaleAnimation(detailPanel.transform, Vector3.zero, 0.3f, onComplete: () =>
        {
            // Setelah animasi selesai, nonaktifkan detail panel
            detailPanel.SetActive(false);
        }));

        // Hentikan animasi pulsating pada gambar detail
        if (detailImageAnimationCoroutine != null)
        {
            StopCoroutine(detailImageAnimationCoroutine);
            detailImageAnimationCoroutine = null;
        }

        // Reset skala gambar detail
        if (detailImage != null)
        {
            detailImage.transform.localScale = Vector3.one;
        }
    }

    // Overload ScaleAnimation untuk menambahkan onComplete callback
    private System.Collections.IEnumerator ScaleAnimation(Transform target, Vector3 targetScale, float duration, System.Action onComplete = null)
    {
        Vector3 startScale = target.localScale;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // Gunakan easeOutBack effect (overshoot dan settle back)
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            float progress = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);

            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        target.localScale = targetScale;

        // Callback setelah animasi selesai
        onComplete?.Invoke();
    }

    void NextPage()
    {
        currentPage++;
        RefreshItemDisplay();
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshItemDisplay();
        }
    }

    // Refresh all almanac data from JamuSystem
    private void RefreshAlmanacDataFromJamuSystem()
    {
        if (jamuSystem == null || jamuSystem.jamuDatabase == null)
        {
            if (isAlmanacDataRefreshed) return; // Hindari pemanggilan berulang
            isAlmanacDataRefreshed = true;
            Debug.LogWarning("Cannot refresh almanac - JamuSystem or database not found!");

            // Try to find JamuSystem if not already assigned
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null || jamuSystem.jamuDatabase == null)
            {
                Debug.LogError("JamuSystem could not be found in the scene.");
                return;
            }
        }

        // Process Bahans (Rempah)
        foreach (var bahan in jamuSystem.jamuDatabase.bahans)
        {
            // Only add if not already in dictionary
            if (!discoveredItems.ContainsKey(bahan.itemName))
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = bahan.itemName,
                    manfaat = bahan.description,
                    tipe = "Rempah",
                    gambar = bahan.itemSprite,
                    ditemukan = false // Default to not discovered
                };
                discoveredItems.Add(bahan.itemName, newItem);
                Debug.Log($"Added Rempah to almanac database: {bahan.itemName}");
            }
        }

        // Process Jamu recipes
        foreach (var resep in jamuSystem.jamuDatabase.resepJamus)
        {
            // Only add if not already in dictionary
            if (!discoveredItems.ContainsKey(resep.jamuName))
            {
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = resep.jamuName,
                    manfaat = resep.description,
                    tipe = "Jamu",
                    gambar = resep.jamuSprite,
                    ditemukan = false // Default to not discovered
                };
                discoveredItems.Add(resep.jamuName, newItem);
                Debug.Log($"Added Jamu recipe to almanac database: {resep.jamuName}");
            }
        }

        Debug.Log($"Almanac data refreshed from JamuSystem. Total items: {discoveredItems.Count}");
    }

    // Add a new rempah item to the almanac
    public void AddItemToAlmanac(string namaItem)
    {
        // Try to get item from JamuSystem if not already in almanac
        if (!discoveredItems.ContainsKey(namaItem) && jamuSystem != null)
        {
            BahanItem bahan = jamuSystem.GetBahan(namaItem);
            if (bahan != null)
            {
                // Create new almanac entry for this bahan
                AlmanacItemData newItem = new AlmanacItemData
                {
                    nama = bahan.itemName,
                    manfaat = bahan.description,
                    tipe = "Rempah",
                    gambar = bahan.itemSprite,
                    ditemukan = true
                };
                discoveredItems.Add(namaItem, newItem);
                SaveDiscoveredItemsData();

                // Show discovery popup
                ShowDiscoveryPopup(newItem);
                return;
            }
        }

        // Mark as discovered if it already exists
        if (discoveredItems.TryGetValue(namaItem, out AlmanacItemData item))
        {
            if (!item.ditemukan)
            {
                item.ditemukan = true;
                SaveDiscoveredItemsData();
                Debug.Log($"Item {namaItem} ditambahkan ke almanac!");

                // Show discovery popup
                ShowDiscoveryPopup(item);
            }
            return;
        }

        Debug.LogWarning($"Item {namaItem} tidak ditemukan di JamuSystem");
    }

    // Add jamu to almanac
    // Replace the existing AddJamuToAlmanac method with this improved version

    // Add a jamu to the almanac when discovered or created
    public bool DiscoverJamu(string jamuName)
    {
        Debug.Log($"Attempting to discover jamu: {jamuName}");

        // First ensure the JamuSystem is properly linked
        if (jamuSystem == null)
        {
            jamuSystem = FindAnyObjectByType<JamuSystem>();
            if (jamuSystem == null)
            {
                Debug.LogError("JamuSystem not found when trying to discover jamu.");
                return false;
            }
        }

        // Make sure we have loaded all possible items first
        if (discoveredItems.Count == 0)
        {
            RefreshAlmanacDataFromJamuSystem();
        }

        // Get the jamu recipe data
        ResepJamu resep = jamuSystem.GetResepJamu(jamuName);
        if (resep == null)
        {
            Debug.LogWarning($"Jamu {jamuName} not found in database.");
            return false;
        }

        // Check if we need to add this jamu to the dictionary first
        if (!discoveredItems.ContainsKey(jamuName))
        {
            AlmanacItemData newItem = new AlmanacItemData
            {
                nama = resep.jamuName,
                manfaat = resep.description,
                tipe = "Jamu",
                gambar = resep.jamuSprite,
                ditemukan = true
            };
            discoveredItems.Add(jamuName, newItem);

            // Show discovery popup and save
            ShowDiscoveryPopup(newItem);
            SaveDiscoveredItemsData();
            Debug.Log($"Jamu {jamuName} added to almanac as new item!");
            return true;
        }
        else if (!discoveredItems[jamuName].ditemukan)
        {
            // Mark as discovered if it exists but hasn't been discovered yet
            discoveredItems[jamuName].ditemukan = true;

            // Show discovery popup and save
            ShowDiscoveryPopup(discoveredItems[jamuName]);
            SaveDiscoveredItemsData();
            Debug.Log($"Jamu {jamuName} marked as discovered in almanac!");
            return true;
        }

        // Item was already discovered
        Debug.Log($"Jamu {jamuName} was already in almanac.");
        return false;
    }

    // Show discovery popup when a new item is found
    void ShowDiscoveryPopup(AlmanacItemData item)
    {
        // This could be expanded with a nice UI popup
        Debug.Log($"New item discovered: {item.nama}");

        // TODO: Implement popup panel animation showing the newly discovered item
        // Could show a small notification that slides in from the side
    }

    // Save discovered items to PlayerPrefs
    private void SaveDiscoveredItemsData()
    {
        string almanacSaveKey = "almanac_data";
        string saveData = "";

        // Format: name:1,name2:0,... (1=discovered, 0=not discovered)
        foreach (var item in discoveredItems)
        {
            saveData += item.Key + ":" + (item.Value.ditemukan ? "1" : "0") + ",";
        }

        // Remove last comma
        if (saveData.Length > 0)
        {
            saveData = saveData.Substring(0, saveData.Length - 1);
        }

        PlayerPrefs.SetString(almanacSaveKey, saveData);
        PlayerPrefs.Save();
    }

    // Load discovered items from PlayerPrefs
    private void LoadDiscoveredItemsData()
    {
        // First load all items from JamuSystem
        RefreshAlmanacDataFromJamuSystem();

        // Then load discovery status from PlayerPrefs
        string almanacSaveKey = "almanac_data";
        string saveData = PlayerPrefs.GetString(almanacSaveKey, "");

        if (!string.IsNullOrEmpty(saveData))
        {
            string[] items = saveData.Split(',');

            foreach (string item in items)
            {
                string[] parts = item.Split(':');
                if (parts.Length >= 2)
                {
                    string itemName = parts[0];
                    bool discovered = parts[1] == "1";

                    // Update discovery status
                    if (discoveredItems.TryGetValue(itemName, out AlmanacItemData almanacItem))
                    {
                        almanacItem.ditemukan = discovered;
                    }
                }
            }
        }
    }

    // Debug method to unlock all items (for testing)
    public void UnlockAllItems()
    {
        foreach (var item in discoveredItems.Values)
        {
            item.ditemukan = true;
        }
        SaveDiscoveredItemsData();

        if (almanacPanel.activeSelf)
        {
            RefreshItemDisplay();
        }

        Debug.Log("All almanac items unlocked!");
    }
}