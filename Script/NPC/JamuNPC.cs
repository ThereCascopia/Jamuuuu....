using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class JamuNPC : MonoBehaviour
{
    // Required components
    public Image requestIcon;
    private JamuDatabase requestedJamuObj;
    public Canvas requestCanvas;     // Canvas containing the request icon
    public NPCCraftingPanel craftingPanel; // UI for mixing jamu ingredients specifically for NPC

    // NPC stats
    public float interactionDistance = 2f;  // How close player needs to be to interact
    public int reward = 50;               // Money rewarded for correct jamu
    public Sprite[] jamuTypes;            // Array of possible jamu types
    private Color originalColor;

    // Private variables
    private ResepJamu requestedJamuResep; // The jamu type this NPC wants
    private Transform player;             // Reference to player
    private bool canInteract = false;     // Whether player is close enough to interact

    void Start()
    {
        // Get reference to player
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Hide UI elements initially
        requestCanvas.gameObject.SetActive(false);

        // Make sure crafting panel is assigned
        if (craftingPanel == null)
        {
            Debug.LogError("NPCCraftingPanel tidak ditemukan! Pastikan komponen ini terhubung di Inspector.");
        }

    }

    void Update()
    {
        // Check if player is close enough to interact
        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactionDistance)
        {
            if (!canInteract)
            {
                canInteract = true;
                ShowRequestIcon();
            }
        }
        else
        {
            if (canInteract)
            {
                canInteract = false;
                HideRequestIcon();
            }
        }
    }

    // Display the jamu request icon above NPC
    void ShowRequestIcon()
    {
        requestCanvas.gameObject.SetActive(true);
    }

    // Hide the request icon
    void HideRequestIcon()
    {
        requestCanvas.gameObject.SetActive(false);
    }

    public void CreateRandomJamuRequest()
    {
        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
        {
            Debug.LogError("JamuTypes array kosong! Tambahkan sprite jamu di Inspector.");
            return;
        }

        // Dapatkan jamu acak dari JamuSystem
        var jamuDb = JamuSystem.Instance.jamuDatabase;
        if (jamuDb.resepJamus.Count > 0)
        {
            int randomIndex = Random.Range(0, jamuDb.resepJamus.Count);
            requestedJamuResep = jamuDb.resepJamus[randomIndex];

            // Set icon request
            if (requestIcon != null)
            {
                requestIcon.sprite = requestedJamuResep.jamuSprite;
            }
        }
        else
        {
            Debug.LogError("Tidak ada resep jamu dalam database!");
        }
    }

    // Called when player clicks on NPC
    public void OnNPCClicked()
    {
        if (canInteract)
        {
            ShowCraftingPanel();
        }
    }

    // Show crafting UI to mix jamu
    void ShowCraftingPanel()
    {
        if (craftingPanel != null)
        {
            // Initialize the crafting panel with reference to this NPC
            craftingPanel.Initialize(this);
        }
        else
        {
            Debug.LogError("Tidak dapat menampilkan panel crafting: Panel tidak terhubung!");
        }
    }

    // Called when player gives jamu to NPC
    public void GiveJamuToNPC(ResepJamu givenJamuResep)
    {
        bool isCorrectJamu = false;

        if (givenJamuResep != null && requestedJamuResep != null &&
        givenJamuResep.jamuName == requestedJamuResep.jamuName)
        {
            isCorrectJamu = true;
        }

        if (isCorrectJamu)
        {
            // Correct jamu was given
            var gameManager = GameObject.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.AddMoney(reward);
            }
            else
            {
                // Try using DataGame if exists
                var dtg = ManagerPP<DataGame>.Get("datagame");
                if (dtg != null)
                {
                    dtg.koin += reward;
                    ManagerPP<DataGame>.Set("datagame", dtg);
                }

                Debug.Log("Uang bertambah: " + reward);
            }

            ShowSuccessEffect();
        }
        else
        {
            // Wrong jamu was given, just close the panel
            ShowFailureEffect();

            // Ubah warna menjadi lebih merah
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                spriteRenderer.color += new Color(10f, 0, 0);
            }
        }

        // NPC disappears after interaction
        StartCoroutine(RemoveNPC());
    }


    // Visual feedback for successful jamu
    void ShowSuccessEffect()
    {
        Debug.Log("Sukses! Kamu mendapatkan " + reward + " koin!");
        // Add visual effects here if needed
    }

    // Visual feedback for wrong jamu
    void ShowFailureEffect()
    {
        Debug.Log("Jamu salah! NPC tidak senang.");
        // Add visual effects here if needed
    }

    // Make NPC leave after interaction
    IEnumerator RemoveNPC()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
}