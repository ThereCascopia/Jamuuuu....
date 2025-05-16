using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// Adapter that connects the JamuSystem with the existing game systems
/// </summary>
public class JamuIntegration : MonoBehaviour
{
    public static JamuIntegration Instance { get; private set; }

    [SerializeField] public JamuSystem jamuSystem;
    [SerializeField] public Inventory inventory;

    // Cache for converted items
    private Dictionary<string, Item> convertedBahanItems = new Dictionary<string, Item>();
    private Dictionary<string, Item> convertedJamuItems = new Dictionary<string, Item>();

    private List<ICraftingPanel> registeredPanels = new List<ICraftingPanel>();

    public void RegisterCraftingPanel(ICraftingPanel panel)
    {
        if (panel != null && !registeredPanels.Contains(panel))
        {
            registeredPanels.Add(panel);
            Debug.Log("Crafting panel registered: " + panel);
        }
    }

    void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Find references if not set in inspector
        if (jamuSystem == null)
            jamuSystem = FindAnyObjectByType<JamuSystem>();

        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
    }

    void Start()
    {
        Debug.Log("JamuIntegration started. Found JamuSystem: " + (jamuSystem != null));
        Debug.Log("JamuIntegration found Inventory: " + (inventory != null));

        // Pre-cache jamu items for quicker reference
        if (jamuSystem != null && jamuSystem.jamuDatabase != null)
        {
            CacheJamuItems();
        }
    }

    /// <summary>
    /// Cache all jamu items for quicker conversion
    /// </summary>
    private void CacheJamuItems()
    {
        // Cache bahan items
        foreach (var bahan in jamuSystem.jamuDatabase.bahans)
        {
            Item item = new Item
            {
                nama = bahan.itemName,
                gambar = bahan.itemSprite,
                harga = bahan.itemValue,
                jumlah = 0
            };
            convertedBahanItems[bahan.itemName] = item;
        }

        // Cache jamu recipes
        foreach (var jamu in jamuSystem.jamuDatabase.resepJamus)
        {
            Item item = new Item
            {
                nama = jamu.jamuName,
                gambar = jamu.jamuSprite,
                harga = jamu.jamuValue,
                jumlah = 0
            };
            convertedJamuItems[jamu.jamuName] = item;
        }
    }

    /// <summary>
    /// Convert a Bahan Item from JamuSystem to the game's Item system
    /// </summary>
    public Item ConvertBahanToItem(BahanItem bahan)
    {
        if (bahan == null) return null;

        if (convertedBahanItems.TryGetValue(bahan.itemName, out Item cachedItem))
        {
            // Return a new instance with the same properties
            return new Item
            {
                nama = cachedItem.nama,
                gambar = cachedItem.gambar,
                harga = cachedItem.harga,
                jumlah = 1
            };
        }

        // Create new item if not cached
        Item item = new Item
        {
            nama = bahan.itemName,
            gambar = bahan.itemSprite,
            harga = bahan.itemValue,
            jumlah = 1
        };

        // Cache it for later use
        convertedBahanItems[bahan.itemName] = item;
        return item;
    }

    /// <summary>
    /// Convert a Jamu item to the game's Item system
    /// </summary>
    public Item ConvertJamuToItem(ResepJamu jamu)
    {
        if (jamu == null) return null;

        if (convertedJamuItems.TryGetValue(jamu.jamuName, out Item cachedItem))
        {
            // Return a new instance with the same properties
            return new Item
            {
                nama = cachedItem.nama,
                gambar = cachedItem.gambar,
                harga = cachedItem.harga,
                jumlah = 1
            };
        }

        // Create new item if not cached
        Item item = new Item
        {
            nama = jamu.jamuName,
            gambar = jamu.jamuSprite,
            harga = jamu.jamuValue,
            jumlah = 1
        };

        // Cache it for later use
        convertedJamuItems[jamu.jamuName] = item;
        return item;
    }

    /// <summary>
    /// Get a list of bahan names that match the current inventory items
    /// </summary>
    public List<string> GetAvailableBahanNames()
    {
        if (jamuSystem == null || jamuSystem.jamuDatabase == null)
            return new List<string>();

        List<string> result = new List<string>();
        var dtg = ManagerPP<DataGame>.Get("datagame");

        if (dtg == null) return result;

        foreach (Item item in dtg.barang)
        {
            if (item != null && item.gambar != null && item.jumlah > 0)
            {
                // Check if this item is a recognized bahan
                BahanItem bahan = jamuSystem.jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    result.Add(item.nama);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Check if a jamu can be crafted with the given ingredients
    /// </summary>
    public bool CanCraftJamu(List<string> ingredientNames)
    {
        if (jamuSystem == null || jamuSystem.jamuDatabase == null)
            return false;

        // Check each recipe in the database
        foreach (ResepJamu recipe in jamuSystem.jamuDatabase.resepJamus)
        {
            // Skip if ingredient count doesn't match
            if (recipe.bahanResep.Length != ingredientNames.Count)
                continue;

            // Check if all ingredients are used in this recipe
            bool allIngredientsMatch = true;
            foreach (string recipeIngredient in recipe.bahanResep)
            {
                if (!ingredientNames.Contains(recipeIngredient))
                {
                    allIngredientsMatch = false;
                    break;
                }
            }

            if (allIngredientsMatch)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Try to craft a jamu with the given ingredients
    /// </summary>
    public ResepJamu TryCraftJamu(List<string> ingredientNames)
    {
        if (jamuSystem == null || jamuSystem.jamuDatabase == null)
            return null;

        // Find a matching recipe
        foreach (ResepJamu recipe in jamuSystem.jamuDatabase.resepJamus)
        {
            // Skip if ingredient count doesn't match
            if (recipe.bahanResep.Length != ingredientNames.Count)
                continue;

            // Sort both lists to ensure comparison works regardless of order
            List<string> sortedRecipeIngredients = recipe.bahanResep.ToList();
            sortedRecipeIngredients.Sort();

            List<string> sortedIngredients = new List<string>(ingredientNames);
            sortedIngredients.Sort();

            // Check if ingredient lists match
            bool allIngredientsMatch = true;
            for (int i = 0; i < sortedRecipeIngredients.Count; i++)
            {
                if (!sortedIngredients[i].Equals(sortedRecipeIngredients[i]))
                {
                    allIngredientsMatch = false;
                    break;
                }
            }

            if (allIngredientsMatch)
                return recipe;
        }

        return null;
    }

    /// <summary>
    /// Add a jamu to the player's inventory
    /// </summary>
    public bool AddJamuToInventory(ResepJamu jamu)
    {
        if (jamu == null || inventory == null) return false;

        Item jamuItem = ConvertJamuToItem(jamu);
        if (jamuItem == null) return false;

        inventory.TambahItemHasilPanen(jamuItem.gambar);
        return true;
    }
}

/// <summary>
/// Factory for setting up the JamuIntegration system
/// </summary>
#if UNITY_EDITOR
public class JamuIntegrationSetup
{
    [UnityEditor.MenuItem("Tools/Jamu System/Setup Jamu Integration")]
    static void SetupJamuIntegration()
    {
        // Check if instance already exists
        JamuIntegration existing = Object.FindAnyObjectByType<JamuIntegration>();
        if (existing != null)
        {
            UnityEditor.Selection.activeGameObject = existing.gameObject;
            Debug.Log("JamuIntegration already exists");
            return;
        }

        // Create new integration object
        GameObject integrationObj = new GameObject("JamuIntegration");
        JamuIntegration integration = integrationObj.AddComponent<JamuIntegration>();

        // Find references to existing systems
        integration.jamuSystem = Object.FindAnyObjectByType<JamuSystem>();
        integration.inventory = Object.FindAnyObjectByType<Inventory>();

        UnityEditor.Selection.activeGameObject = integrationObj;
        Debug.Log("JamuIntegration created");
    }
}
#endif