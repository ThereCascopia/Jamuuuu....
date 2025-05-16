using UnityEngine;
using UnityEngine.UI;

public class PlantingSystem : MonoBehaviour
{
    public static PlantingSystem Instance;
    private SoilTile currentSoil;
    public GameObject panelPilihBibit; // UI panel to choose seed
    public Transform isiPanel;
    public GameObject tombolSeedPrefab; // button with image & text
    public Button tombolClose;

    private void Awake()
    {
        // Cari referensi di prefab jika belum diassign
        if (panelPilihBibit == null)
        {
            panelPilihBibit = transform.Find("panelPilihBibit").gameObject;
        }

        if (tombolClose == null)
        {
            tombolClose = transform.Find("tombolClose").GetComponent<Button>();
        }

        // Tambahkan listener untuk tombolClose
        if (tombolClose != null)
        {
            tombolClose.onClick.RemoveAllListeners(); // Pastikan listener lama dihapus
            tombolClose.onClick.AddListener(() =>
            {
                CloseSeedPanel();
            });
        }

        Instance = this;
    }

    // Fungsi untuk menutup panel pilih bibit
    public void CloseSeedPanel()
    {
        panelPilihBibit.SetActive(false);
        tombolClose.gameObject.SetActive(false); // Nonaktifkan tombol close juga
        Debug.Log("Panel pilih bibit dan tombol close telah ditutup.");
    }

    public void StartPlanting(SoilTile soil)
    {
        currentSoil = soil;

        // Pastikan panelPilihBibit dan tombolClose aktif
        panelPilihBibit.SetActive(true);
        tombolClose.gameObject.SetActive(true);

        ShowSeedChoice();
    }

    void ShowSeedChoice()
    {
        // Kosongkan dulu isi panel
        foreach (Transform child in isiPanel)
        {
            Destroy(child.gameObject);
        }

        // Ambil semua bibit dari inventory
        var benihList = JamuSystem.Instance.jamuDatabase.benihs;

        foreach (BenihItem benih in benihList)
        {
            var data = ManagerPP<DataGame>.Get("datagame");

            // Cek apakah benih ini ada di inventory
            foreach (Item item in data.barang)
            {
                if (item != null && item.jumlah > 0 && item.nama == benih.itemName)
                {
                    GameObject tombol = Instantiate(tombolSeedPrefab, isiPanel);
                    tombol.GetComponentInChildren<Image>().sprite = benih.itemSprite;
                    tombol.GetComponentInChildren<Text>().text = item.jumlah.ToString();

                    string namaSeed = benih.itemName;

                    tombol.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        TanamBibit(namaSeed);
                        tombolClose.gameObject.SetActive(false);
                        panelPilihBibit.SetActive(false);
                    });
                }
            }
        }


        panelPilihBibit.SetActive(true);
    }

    public void TogglePanelBibit()
    {
        // Ambil status aktif saat ini dari panelPilihBibit
        bool aktif = panelPilihBibit.activeSelf;

        // Ubah status aktif panelPilihBibit dan tombolClose agar sama
        panelPilihBibit.SetActive(!aktif);
        tombolClose.gameObject.SetActive(!aktif);
    }


    void TanamBibit(string namaSeed)
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.RefreshInventory();
        }
        else
        {
            Debug.LogError("Inventory.Instance is null. Make sure the Inventory GameObject is active and properly initialized.");
        }
        var data = ManagerPP<DataGame>.Get("datagame");
        BenihItem seed = JamuSystem.Instance.GetBenih(namaSeed);
        if (seed == null)
        {
            Debug.LogError($"Benih {namaSeed} tidak ditemukan di database.");
            return;
        }

        for (int i = 0; i < data.barang.Count; i++)
        {
            if (data.barang[i] != null && data.barang[i].nama == namaSeed)
            {
                if (data.barang[i].jumlah > 0)
                {
                    data.barang[i].jumlah--;

                    if (data.barang[i].jumlah <= 0)
                    {
                        data.barang[i] = new Item();
                    }

                    ManagerPP<DataGame>.Set("datagame", data);
                    currentSoil.Plant(seed); // Seed name dikirim ke SoilTile
                    Inventory.Instance.RefreshInventory();
                    return;
                }
            }
        }
        Debug.LogWarning("Benih tidak tersedia di inventory.");
    }

}
