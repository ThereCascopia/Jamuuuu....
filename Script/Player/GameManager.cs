using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Sistem penyimpanan data game yang disederhanakan
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Text moneyText;

    private DataGame gameData;
    private string saveName = "datagame";
    private JamuSystem jamuSystem; // Reference to JamuSystem

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

        // Load data game yang tersimpan
        LoadGameData();

        // Initialize JamuSystem
        jamuSystem = FindAnyObjectByType<JamuSystem>();
        if (jamuSystem != null)
        {
            jamuSystem.LoadDatabase(); // Load the jamu database
        }
    }

    void Start()
    {
        // Update tampilan uang di awal
        UpdateMoneyDisplay();
    }

    // Memuat data game dari PlayerPrefs
    void LoadGameData()
    {
        gameData = ManagerPP<DataGame>.Get(saveName);
        Debug.Log("Data loaded: " + gameData.koin + " koin");
    }

    // Menyimpan data game ke PlayerPrefs
    void SaveGameData()
    {
        ManagerPP<DataGame>.Set(saveName, gameData);
        Debug.Log("Data saved: " + gameData.koin + " koin");
    }

    // Menambahkan uang/koin ke dompet pemain
    public void AddMoney(int amount)
    {
        gameData.koin += amount;
        UpdateMoneyDisplay();
        SaveGameData();
    }

    // Mengurangi uang/koin dari dompet pemain
    public bool SpendMoney(int amount)
    {
        if (gameData.koin >= amount)
        {
            gameData.koin -= amount;
            UpdateMoneyDisplay();
            SaveGameData();
            return true;
        }
        return false;
    }

    // Mendapatkan jumlah uang/koin saat ini
    public int GetMoney()
    {
        return gameData.koin;
    }

    // Update tampilan uang di UI
    void UpdateMoneyDisplay()
    {
        if (moneyText != null)
            moneyText.text = gameData.koin.ToString();
    }

    // Mengelola barang/item dalam inventory
    public Item GetItem(int index)
    {
        if (index >= 0 && index < gameData.barang.Count)
            return gameData.barang[index];
        return null;
    }

    public void SetItem(int index, Item item)
    {
        if (index >= 0 && index < gameData.barang.Count)
        {
            gameData.barang[index] = item;
            SaveGameData();
        }
    }

    // Dipanggil ketika aplikasi ditutup
    void OnApplicationQuit()
    {
        SaveGameData();
        if (jamuSystem != null)
        {
            jamuSystem.SaveDatabase(); // Save the jamu database
        }
    }

    public bool HasWatchedCutscene()
    {
        return gameData.hasWatchedCutscene;
    }

    // Menetapkan status cutscene sebagai sudah ditonton
    public void SetCutsceneWatched()
    {
        gameData.hasWatchedCutscene = true;
        SaveGameData();
        Debug.Log("Cutscene status ditandai sebagai sudah ditonton");
    }

    // Untuk testing - reset status cutscene
    public void ResetCutsceneStatus()
    {
        gameData.hasWatchedCutscene = false;
        SaveGameData();
        Debug.Log("Cutscene status di-reset");
    }

}

// Helper class untuk menyimpan dan memuat data (dari ManagerPP.cs)
public static class ManagerPP<T>
{
    public static void Set(string namaPP, T dtg)
    {
        string json = JsonUtility.ToJson(dtg);
        PlayerPrefs.SetString(namaPP, json);
        PlayerPrefs.Save();
    }

    public static T Get(string namaPP)
    {
        string json = PlayerPrefs.GetString(namaPP, "{}");
        T dtg = JsonUtility.FromJson<T>(json);
        return dtg;
    }
}

// Data class untuk menyimpan informasi game (dari DataGame.cs)
[System.Serializable]
public class DataGame
{
    public int koin = 100;
    public List<Item> barang = new List<Item>();
    public bool hasWatchedCutscene = false;  // Status apakah cutscene sudah pernah ditonton
}