using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

/// <summary>
/// Comprehensive jamu system that manages bahans, benihs, and jamu recipes
/// </summary>
public class JamuSystem : MonoBehaviour
{
    public static JamuSystem Instance { get; private set; }

    // Reference to the scriptable object database
    public JamuDatabase jamuDatabase;

    // Reference to the AlmanacSystem for adding discovered items
    private AlmanacSystem almanacSystem;

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
    }

    public void LoadDatabase()
    {
        // Load the database from PlayerPrefs or directly from asset
        if (jamuDatabase == null)
        {
            // Attempt to load from resources or a specific path
            jamuDatabase = Resources.Load<JamuDatabase>("PathToYourDatabase/JamuDatabase");
            if (jamuDatabase == null)
            {
                Debug.LogError("Failed to load JamuDatabase.");
            }
        }
    }

    public void SaveDatabase()
    {
        // Save the database to PlayerPrefs
        string json = JsonUtility.ToJson(jamuDatabase);
        PlayerPrefs.SetString("JamuDatabase", json);
        PlayerPrefs.Save();
        Debug.Log("Jamu Database saved.");
    }

    void Start()
    {
        // Get reference to AlmanacSystem
        almanacSystem = AlmanacSystem.Instance;
        if (almanacSystem == null)
        {
            Debug.LogWarning("AlmanacSystem not found in the scene. Jamus won't be added to almanac automatically.");
        }
    }

    /// <summary>
    /// Get a bahan item by its name
    /// </summary>
    public BahanItem GetBahan(string bahanName)
    {
        return jamuDatabase.GetBahan(bahanName);
    }

    /// <summary>
    /// Get a benih item by its name
    /// </summary>
    public BenihItem GetBenih(string benihName)
    {
        return jamuDatabase.GetBenih(benihName);
    }

    /// <summary>
    /// Get a jamu recipe by its name
    /// </summary>
    public ResepJamu GetResepJamu(string jamuName)
    {
        return jamuDatabase.GetResepJamu(jamuName);
    }

    /// <summary>
    /// Check if the player has all ingredients for a jamu recipe
    /// </summary>
    public bool HasIngredientsForJamu(string jamuName)
    {
        ResepJamu recipe = GetResepJamu(jamuName);
        if (recipe == null) return false;

        // Check if player has all ingredients in the inventory
        // This requires integration with your inventory system
        GameManager gameManager = GameManager.instance;
        Dictionary<string, int> playerBahans = new Dictionary<string, int>();

        // Count all bahans in player inventory
        for (int i = 0; i < 15; i++)
        {
            Item item = gameManager.GetItem(i);
            if (item != null && item.jumlah > 0)
            {
                // Check if the item is a bahan
                BahanItem bahan = jamuDatabase.GetBahan(item.nama);
                if (bahan != null)
                {
                    if (playerBahans.ContainsKey(item.nama))
                        playerBahans[item.nama] += item.jumlah;
                    else
                        playerBahans[item.nama] = item.jumlah;
                }
            }
        }

        // Check if player has all needed ingredients
        foreach (string ingredient in recipe.bahanResep)
        {
            if (!playerBahans.ContainsKey(ingredient) || playerBahans[ingredient] < 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Create a jamu from a recipe (consume ingredients and add jamu to inventory)
    /// </summary>
    public bool CreateJamu(string jamuName)
    {
        if (!HasIngredientsForJamu(jamuName)) return false;

        ResepJamu recipe = GetResepJamu(jamuName);
        GameManager gameManager = GameManager.instance;

        // Consume each ingredient
        foreach (string ingredient in recipe.bahanResep)
        {
            bool foundIngredient = false;

            // Find and remove one of each ingredient
            for (int i = 0; i < 15; i++)
            {
                Item item = gameManager.GetItem(i);
                if (item != null && item.nama == ingredient && item.jumlah > 0)
                {
                    item.jumlah--;
                    gameManager.SetItem(i, item);
                    foundIngredient = true;
                    break;
                }
            }

            if (!foundIngredient) return false; // Safeguard
        }

        // Create jamu item
        Item jamuItem = new Item
        {
            nama = recipe.jamuName,
            gambar = recipe.jamuSprite,
            harga = recipe.jamuValue,
            jumlah = 1
        };

        // Find empty slot or matching jamu to add to
        bool added = false;
        for (int i = 0; i < 15; i++)
        {
            Item slot = gameManager.GetItem(i);

            // If slot is empty or contains same jamu
            if (slot == null || (slot.nama == jamuName && slot.jumlah > 0))
            {
                if (slot == null)
                {
                    gameManager.SetItem(i, jamuItem);
                }
                else
                {
                    slot.jumlah++;
                    gameManager.SetItem(i, slot);
                }
                added = true;
                break;
            }
        }

        // If jamu was successfully created and added to inventory, 
        // also add it to the almanac
        if (added)
        {
            // Add to almanac if almanacSystem exists
            if (almanacSystem != null)
            {
                almanacSystem.DiscoverJamu(jamuName);
            }
            else
            {
                // Try to find almanacSystem again if it wasn't found earlier
                almanacSystem = AlmanacSystem.Instance;
                if (almanacSystem != null)
                {
                    almanacSystem.DiscoverJamu(jamuName);
                }
                else
                {
                    Debug.LogWarning($"AlmanacSystem not found, couldn't add {jamuName} to almanac");
                }
            }
            return true;
        }
        return added;
    }
}

/// <summary>
/// Base class for all jamu items
/// </summary>
[Serializable]
public abstract class BaseJamuItem
{
    public string itemName;
    public Sprite itemSprite;
    public int itemValue;  // Market price/value

    [TextArea(2, 5)]
    public string description;
}

/// <summary>
/// Represents a harvested bahan item
/// </summary>
[Serializable]
public class BahanItem : BaseJamuItem
{
    // Additional bahan-specific properties can be added here
}

/// <summary>
/// Represents a benih that can be planted to grow into a bahan
/// </summary>
[Serializable]
public class BenihItem : BaseJamuItem
{
    public Sprite[] growthStages;  // Sprite for each growth stage
    public float growthTime = 1f;  // Time between stages (in seconds)
    public BahanItem producesBahan;  // Reference to the bahan this benih produces
}

/// <summary>
/// Represents a jamu recipe
/// </summary>
[Serializable]
public class ResepJamu : BaseJamuItem
{
    public string jamuName => itemName;     // Name of the jamu
    public Sprite jamuSprite => itemSprite; // Result jamu sprite
    public int jamuValue => itemValue;      // Value of the resulting jamu

    public string[] bahanResep;  // Array of bahan names needed for this recipe

}

/// <summary>
/// The main database that holds all bahans, benihs, and resep jamus
/// </summary>
[CreateAssetMenu(fileName = "JamuDatabase", menuName = "Jamu System/Jamu Database")]
public class JamuDatabase : ScriptableObject
{
    [Header("Bahans")]
    public List<BahanItem> bahans = new List<BahanItem>();

    [Header("Benihs")]
    public List<BenihItem> benihs = new List<BenihItem>();

    [Header("Resep Jamu")]
    public List<ResepJamu> resepJamus = new List<ResepJamu>();

    // Helper methods to get items by name
    public BahanItem GetBahan(string bahanName)
    {
        return bahans.Find(h => h.itemName == bahanName);
    }

    public BenihItem GetBenih(string benihName)
    {
        return benihs.Find(s => s.itemName == benihName);
    }

    public ResepJamu GetResepJamu(string jamuName)
    {
        return resepJamus.Find(j => j.jamuName == jamuName);
    }

    // Add this method to JamuDatabase class
    public BahanItem FindBahanBySprite(Sprite sprite)
    {
        if (sprite == null) return null;

        foreach (BahanItem bahan in bahans)
        {
            if (bahan != null && bahan.itemSprite != null &&
                bahan.itemSprite.name == sprite.name)
            {
                return bahan;
            }
        }

        Debug.LogWarning($"Could not find BahanItem with sprite: {sprite.name}");
        return null;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Custom editor for the JamuDatabase ScriptableObject
/// </summary>
[CustomEditor(typeof(JamuDatabase))]
public class JamuDatabaseEditor : Editor
{
    enum ItemType { Bahan, Benih, ResepJamu }
    private ItemType currentTab = ItemType.Bahan;

    public override void OnInspectorGUI()
    {
        JamuDatabase database = (JamuDatabase)target;

        // Tab selection
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = currentTab == ItemType.Bahan ? Color.green : Color.white;
        if (GUILayout.Button("Bahans", EditorStyles.toolbarButton))
            currentTab = ItemType.Bahan;

        GUI.backgroundColor = currentTab == ItemType.Benih ? Color.green : Color.white;
        if (GUILayout.Button("Benihs", EditorStyles.toolbarButton))
            currentTab = ItemType.Benih;

        GUI.backgroundColor = currentTab == ItemType.ResepJamu ? Color.green : Color.white;
        if (GUILayout.Button("Resep Jamu", EditorStyles.toolbarButton))
            currentTab = ItemType.ResepJamu;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Display appropriate content based on selected tab
        switch (currentTab)
        {
            case ItemType.Bahan:
                DrawBahansTab(database);
                break;
            case ItemType.Benih:
                DrawBenihsTab(database);
                break;
            case ItemType.ResepJamu:
                DrawResepJamuTab(database);
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawBahansTab(JamuDatabase database)
    {
        EditorGUILayout.LabelField("Bahan Items", EditorStyles.boldLabel);

        for (int i = 0; i < database.bahans.Count; i++)
        {
            BahanItem bahan = database.bahans[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Bahan #{i + 1}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                database.bahans.RemoveAt(i);
                EditorUtility.SetDirty(target);
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            bahan.itemName = EditorGUILayout.TextField("Name", bahan.itemName);
            bahan.itemSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", bahan.itemSprite, typeof(Sprite), false);
            bahan.itemValue = EditorGUILayout.IntField("Value", bahan.itemValue);
            bahan.description = EditorGUILayout.TextArea(bahan.description, GUILayout.Height(50));

            // Additional bahan-specific properties could be added here

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Bahan"))
        {
            database.bahans.Add(new BahanItem());
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawBenihsTab(JamuDatabase database)
    {
        EditorGUILayout.LabelField("Benih Items", EditorStyles.boldLabel);

        for (int i = 0; i < database.benihs.Count; i++)
        {
            BenihItem benih = database.benihs[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Benih #{i + 1}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                database.benihs.RemoveAt(i);
                EditorUtility.SetDirty(target);
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            benih.itemName = EditorGUILayout.TextField("Name", benih.itemName);
            benih.itemSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", benih.itemSprite, typeof(Sprite), false);
            benih.itemValue = EditorGUILayout.IntField("Value", benih.itemValue);
            benih.description = EditorGUILayout.TextArea(benih.description, GUILayout.Height(50));

            // Benih-specific properties
            benih.growthTime = EditorGUILayout.FloatField("Growth Time (seconds)", benih.growthTime);

            // Reference to produced bahan
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Produces Bahan:", EditorStyles.boldLabel);

            // Dropdown to select which bahan this benih produces
            List<string> bahanNames = new List<string>() { "(None)" };
            bahanNames.AddRange(database.bahans.Select(h => h.itemName));

            int currentIndex = 0;
            if (benih.producesBahan != null)
            {
                for (int j = 0; j < database.bahans.Count; j++)
                {
                    if (database.bahans[j].itemName == benih.producesBahan.itemName)
                    {
                        currentIndex = j + 1; // +1 because of the "(None)" option
                        break;
                    }
                }
            }

            int newIndex = EditorGUILayout.Popup("Produced Bahan", currentIndex, bahanNames.ToArray());
            if (newIndex == 0)
            {
                benih.producesBahan = null;
            }
            else
            {
                benih.producesBahan = database.bahans[newIndex - 1];
            }

            // Growth stages
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Growth Stages:", EditorStyles.boldLabel);

            if (benih.growthStages == null || benih.growthStages.Length == 0)
            {
                benih.growthStages = new Sprite[4]; // Default to 4 stages
            }

            int stageCount = EditorGUILayout.IntSlider("Number of Stages", benih.growthStages.Length, 1, 8);
            if (stageCount != benih.growthStages.Length)
            {
                // Resize the array, preserving existing values
                System.Array.Resize(ref benih.growthStages, stageCount);
                EditorUtility.SetDirty(target);
            }

            for (int j = 0; j < benih.growthStages.Length; j++)
            {
                benih.growthStages[j] = (Sprite)EditorGUILayout.ObjectField($"Stage {j}", benih.growthStages[j], typeof(Sprite), false);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Benih"))
        {
            database.benihs.Add(new BenihItem());
            EditorUtility.SetDirty(target);
        }
    }

    private void DrawResepJamuTab(JamuDatabase database)
    {
        EditorGUILayout.LabelField("Resep Jamu", EditorStyles.boldLabel);

        for (int i = 0; i < database.resepJamus.Count; i++)
        {
            ResepJamu recipe = database.resepJamus[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Jamu #{i + 1}", EditorStyles.boldLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                database.resepJamus.RemoveAt(i);
                EditorUtility.SetDirty(target);
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            recipe.itemName = EditorGUILayout.TextField("Jamu Name", recipe.itemName);
            recipe.itemSprite = (Sprite)EditorGUILayout.ObjectField("Result Sprite", recipe.itemSprite, typeof(Sprite), false);
            recipe.itemValue = EditorGUILayout.IntField("Value", recipe.itemValue);
            recipe.description = EditorGUILayout.TextArea(recipe.description, GUILayout.Height(50));

            // Ingredients
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ingredients:", EditorStyles.boldLabel);

            if (recipe.bahanResep == null || recipe.bahanResep.Length == 0)
            {
                recipe.bahanResep = new string[2]; // Default to 2 ingredients
            }

            int ingredientCount = EditorGUILayout.IntSlider("Number of Ingredients", recipe.bahanResep.Length, 1, 10);
            if (ingredientCount != recipe.bahanResep.Length)
            {
                // Resize the array, preserving existing values
                System.Array.Resize(ref recipe.bahanResep, ingredientCount);
                EditorUtility.SetDirty(target);
            }

            // Get all available bahan names for dropdown
            List<string> bahanNames = new List<string>() { "(None)" };
            bahanNames.AddRange(database.bahans.Select(h => h.itemName));

            for (int j = 0; j < recipe.bahanResep.Length; j++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Ingredient {j + 1}:", GUILayout.Width(100));

                // Find current index in the bahan list
                int currentIndex = 0;
                string currentIngredient = recipe.bahanResep[j];
                if (!string.IsNullOrEmpty(currentIngredient))
                {
                    for (int k = 0; k < database.bahans.Count; k++)
                    {
                        if (database.bahans[k].itemName == currentIngredient)
                        {
                            currentIndex = k + 1; // +1 because of the "(None)" option
                            break;
                        }
                    }
                }

                int newIndex = EditorGUILayout.Popup(currentIndex, bahanNames.ToArray());
                recipe.bahanResep[j] = newIndex == 0 ? "" : database.bahans[newIndex - 1].itemName;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Resep Jamu"))
        {
            database.resepJamus.Add(new ResepJamu());
            EditorUtility.SetDirty(target);
        }
    }
}

/// <summary>
/// Creator window to make it easier to create a new Jamu Database
/// </summary>
public class JamuDatabaseCreator : EditorWindow
{
    [MenuItem("Tools/Jamu System/Create New Database")]
    public static void ShowWindow()
    {
        GetWindow<JamuDatabaseCreator>("Jamu Database Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a new Jamu System Database", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        if (GUILayout.Button("Create New Database"))
        {
            // Create the scriptable object asset
            JamuDatabase asset = ScriptableObject.CreateInstance<JamuDatabase>();

            // Save the asset as a file
            string path = EditorUtility.SaveFilePanelInProject("Save Jamu Database", "JamuDatabase", "asset", "Save the jamu database");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                // Open the newly created asset
                Selection.activeObject = asset;
            }
        }
    }
}

/// <summary>
/// Editor script for creating the GameObjects needed for the Jamu System
/// </summary>
public class JamuSystemSetup
{
    [MenuItem("Tools/Jamu System/Setup Jamu System")]
    static void SetupJamuSystem()
    {
        // Create the JamuSystem object
        GameObject systemObj = new GameObject("JamuSystem");
        JamuSystem system = systemObj.AddComponent<JamuSystem>();

        // Find any existing JamuDatabase asset
        string[] guids = AssetDatabase.FindAssets("t:JamuDatabase");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            JamuDatabase database = AssetDatabase.LoadAssetAtPath<JamuDatabase>(path);
            system.jamuDatabase = database;
        }

        Selection.activeGameObject = systemObj;
    }
}
#endif