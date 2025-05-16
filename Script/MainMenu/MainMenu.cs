using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Elements")]
    public Button startButton;
    public Button continueButton;

    [Header("Scene Names")]
    public string cutsceneSceneName = "CutsceneScene";
    public string gameSceneName = "GameScene";

    [Header("UI Elements")]
    [SerializeField]
    private GameObject panel;

    [Header("Cutscene Settings")]
    [SerializeField]
    private bool skipCutsceneOnContinue = true;

    private void Start()
    {
        // Cek apakah ada save game
        CheckForSaveGame();

        // Tambahkan listener untuk tombol start
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartNewGame);
        }

        // Tambahkan listener untuk tombol continue
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueGame);
        }
    }

    private void CheckForSaveGame()
    {
        // Cek apakah ada data game sebelumnya
        if (PlayerPrefs.HasKey("datagame"))
        {
            // Aktifkan tombol continue jika ada save game
            if (continueButton != null)
                continueButton.interactable = true;
        }
        else
        {
            // Nonaktifkan tombol continue jika tidak ada save game
            if (continueButton != null)
                continueButton.interactable = false;
        }
    }

    public void StartNewGame()
    {
        // Reset status cutscene untuk memastikan cutscene diputar kembali
        ResetCutsceneStatus();

        // Load scene cutscene
        SceneManager.LoadScene(cutsceneSceneName);
    }

    // Melanjutkan game yang sudah ada
    public void ContinueGame()
    {
        // Set status cutscene sudah ditonton (untuk memastikan)
        SetCutsceneWatched();

        if (skipCutsceneOnContinue)
        {
            // Load langsung ke game scene
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // Load scene cutscene (akan otomatis di-skip oleh CutsceneManager)
            SceneManager.LoadScene(cutsceneSceneName);
        }
    }

    // Reset status cutscene
    private void ResetCutsceneStatus()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.ResetCutsceneStatus();
        }
        else
        {
            // Cara alternatif jika GameManager belum tersedia
            DataGame data = ManagerPP<DataGame>.Get("datagame");
            data.hasWatchedCutscene = false;
            ManagerPP<DataGame>.Set("datagame", data);

            // Log untuk debugging
            Debug.Log("Reset cutscene status: false");
        }
    }

    // Set status cutscene sudah ditonton
    private void SetCutsceneWatched()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.SetCutsceneWatched();
        }
        else
        {
            // Cara alternatif jika GameManager belum tersedia
            DataGame data = ManagerPP<DataGame>.Get("datagame");
            data.hasWatchedCutscene = true;
            ManagerPP<DataGame>.Set("datagame", data);

            // Log untuk debugging
            Debug.Log("Set cutscene watched: true");
        }
    }

    public void ShowPanel()
    {
        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
    }

    public void PindahScene(string nextScene)
    {
        // Cek jika ini adalah game scene dan mungkin perlu mengelola cutscene
        if (nextScene == gameSceneName)
        {
            // Cek status cutscene
            bool hasWatched = false;

            if (GameManager.instance != null)
            {
                hasWatched = GameManager.instance.HasWatchedCutscene();
            }
            else
            {
                DataGame data = ManagerPP<DataGame>.Get("datagame");
                hasWatched = data.hasWatchedCutscene;
            }

            // Jika belum pernah menonton cutscene, putar cutscene dulu
            if (!hasWatched)
            {
                SceneManager.LoadScene(cutsceneSceneName);
                return;
            }
        }

        // Pindah ke scene yang diminta
        SceneManager.LoadScene(nextScene);
    }

    // Untuk debugging
    public void LogCutsceneStatus()
    {
        if (GameManager.instance != null)
        {
            bool status = GameManager.instance.HasWatchedCutscene();
            Debug.Log("Cutscene status: " + status);
        }
        else
        {
            DataGame data = ManagerPP<DataGame>.Get("datagame");
            Debug.Log("Cutscene status: " + data.hasWatchedCutscene);
        }
    }
}