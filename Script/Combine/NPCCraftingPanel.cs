using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class NPCCraftingPanel : MonoBehaviour, ICraftingPanel
{
    public GameObject[] slotPanelCombine;
    [SerializeField] private Button buatJamuButton;
    [SerializeField] private Button kasihNPCButton;
    [SerializeField] private Button closePanelButton;
    [SerializeField] private Image hasilJamuImage;
    [SerializeField] private Sprite jamuGagalSprite;
    [SerializeField] private Text namaJamuText;

    [SerializeField] private GameObject slotBahanPrefab;
    [SerializeField] private Transform slotBahanContainer;
    [SerializeField] private Transform slotCombineContainer;

    private ResepJamu currentCraftedJamu = null;
    private int selectedBahanIndex = -1;
    private List<ResepJamu> daftarResep;
    private List<int> bahanItemIndices = new List<int>();
    private List<GameObject> bahanSlots = new List<GameObject>();
    private JamuNPC currentNPC;

    public bool NeedsScaleAdjustment => false;

    void Start()
    {
        buatJamuButton.onClick.AddListener(BuatJamu);
        kasihNPCButton.onClick.AddListener(KasihJamuKeNPC);
        closePanelButton.onClick.AddListener(() => gameObject.SetActive(false));
        gameObject.SetActive(false); // Awalnya tidak aktif

        LoadRecipes();
    }

    void OnEnable()
    {
        TampilkanDariInventory();
        ResetSlotCombine();
        hasilJamuImage.gameObject.SetActive(false);
        namaJamuText.gameObject.SetActive(false);
        transform.localScale = Vector3.one; // Skala 1
    }

    public void Initialize(JamuNPC npc)
    {
        currentNPC = npc;

        Debug.Log("NPCCraftingPanel terhubung dengan NPC: " + npc.name);

        gameObject.SetActive(true);
        TampilkanDariInventory();
        ResetSlotCombine();
    }

    private void LoadRecipes()
    {
        daftarResep = JamuSystem.Instance?.jamuDatabase?.resepJamus ?? new List<ResepJamu>();
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
        foreach (GameObject slot in slotPanelCombine)
        {
            Image img = slot.transform.GetChild(0).GetComponent<Image>();
            img.sprite = null;
            img.color = new Color(1f, 1f, 1f, 0f);
        }

        hasilJamuImage.gameObject.SetActive(false);
        namaJamuText.gameObject.SetActive(false);
    }

    public void TampilkanDariInventory()
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (dtg == null) return;

        foreach (GameObject slot in bahanSlots)
            Destroy(slot);
        bahanSlots.Clear();
        bahanItemIndices.Clear();

        List<string> bahanTersedia = JamuIntegration.Instance.GetAvailableBahanNames();

        for (int i = 0; i < dtg.barang.Count; i++)
        {
            var item = dtg.barang[i];
            if (item != null && item.jumlah > 0 && bahanTersedia.Contains(item.nama))
            {
                GameObject slot = Instantiate(slotBahanPrefab, slotBahanContainer);
                bahanSlots.Add(slot);
                bahanItemIndices.Add(i);

                Image img = slot.transform.GetChild(0).GetComponent<Image>();
                Text txt = slot.transform.GetChild(1).GetComponent<Text>();

                img.sprite = item.gambar;
                img.color = Color.white;
                txt.text = item.jumlah.ToString();

                Button slotBtn = slot.GetComponent<Button>();
                int idx = i;
                slotBtn.onClick.AddListener(() => PilihBahan(item.nama));
            }
        }
    }

    public void PilihBahan(string namaBahan)
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (dtg == null || string.IsNullOrEmpty(namaBahan)) return;

        int index = dtg.barang.FindIndex(b => b != null && b.nama == namaBahan && b.jumlah > 0);
        if (index >= 0)
        {
            selectedBahanIndex = index;
            Debug.Log("Bahan dipilih: " + namaBahan);
        }
        else
        {
            Debug.LogWarning("Bahan tidak ditemukan atau habis: " + namaBahan);
        }
    }


    public void TempatkanBahanKeSlotCombine(int index)  
    {
        var dtg = ManagerPP<DataGame>.Get("datagame");
        if (selectedBahanIndex == -1 || dtg == null) return;

        if (index < 0 || index >= slotPanelCombine.Length) return;

        string bahanName = dtg.barang[selectedBahanIndex].nama;
        BahanItem bahan = JamuSystem.Instance.jamuDatabase.GetBahan(bahanName);

        GetBahanFromDatabase(bahanName);

        SlotCombine slotData = slotPanelCombine[index].GetComponent<SlotCombine>();
        if (slotData != null)
        {
            slotData.SetBahan(bahan); // Set the BahanItem for this slot
        }

        Image targetImage = slotPanelCombine[index].transform.GetChild(0).GetComponent<Image>();
        if (targetImage == null) return;

        targetImage.sprite = dtg.barang[selectedBahanIndex].gambar;
        targetImage.color = Color.white;

        float scaleRatio = PanelScalingUtils.CalculateScaleFactor(slotBahanContainer, slotCombineContainer);
        targetImage.transform.localScale = Vector3.one * scaleRatio;

        dtg.barang[selectedBahanIndex].jumlah--;
        ManagerPP<DataGame>.Set("datagame", dtg);

        selectedBahanIndex = -1;

    }

    public void BuatJamu()
    {
        if (JamuSystem.Instance == null || JamuSystem.Instance.jamuDatabase == null)
    {
        Debug.LogError("JamuSystem atau jamuDatabase tidak ditemukan!");
        return;
    }

        List<string> bahanDipakai = slotPanelCombine
        .Select(s => s.GetComponent<SlotCombine>().GetBahan()?.itemName)
        .Where(nama => !string.IsNullOrEmpty(nama))
        .ToList();


        currentCraftedJamu = daftarResep
            .FirstOrDefault(r => r.bahanResep.OrderBy(n => n).SequenceEqual(bahanDipakai.OrderBy(n => n)));

        hasilJamuImage.gameObject.SetActive(true);
        namaJamuText.gameObject.SetActive(true);

        if (currentCraftedJamu != null)
        {
            hasilJamuImage.sprite = currentCraftedJamu.jamuSprite;
            namaJamuText.text = currentCraftedJamu.jamuName;
        }
        else
        {
            hasilJamuImage.sprite = jamuGagalSprite;
            namaJamuText.text = "Jamu tidak diketahui!";
        }
    }

    public ResepJamu GetCurrentCraftedJamu()
    {
        return currentCraftedJamu;
    }

    public void KasihJamuKeNPC()
    {
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
}
