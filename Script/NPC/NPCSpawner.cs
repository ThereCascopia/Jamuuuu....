using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [Header("NPC Setup")]
    public GameObject npcOrang;
    public NPCCraftingPanel craftingPanelReference;
    public Transform[] spawnPoints;
    public Sprite[] availableJamuTypes;
    public float antrianMoveDistance = 3f; // Jarak pergerakan ke kiri ketika NPC baru dibuat
    private List<GameObject> npcsInQueue = new List<GameObject>(); // Daftar NPC yang mengantri


    [Header("Spawning Settings")]
    public float initialSpawnDelay = 3f;
    public float minSpawnInterval = 20f;
    public float maxSpawnInterval = 40f;
    public int maxConcurrentNPCs = 3;

    private int currentNPCCount = 0;

    void Start()
    {
        if (npcOrang == null)
        {
            Debug.LogError("NPC Prefab is missing!");
            return;
        }

        if (craftingPanelReference == null)
        {
            Debug.LogError("Crafting Panel Reference is missing!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return;
        }

        if (availableJamuTypes == null || availableJamuTypes.Length == 0)
        {
            Debug.LogError("No jamu types assigned!");
            return;
        }

        // Start the spawning coroutine
        StartCoroutine(SpawnNPCRoutine());
        StartCoroutine(SpawnNPCEveryThreeSeconds());
    }

    IEnumerator SpawnNPCRoutine()
    {
        // Initial delay before first spawn
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            // Only spawn if we haven't reached the maximum number of NPCs
            if (currentNPCCount < maxConcurrentNPCs)
            {
                SpawnNPC();
                currentNPCCount++;
            }

            // Wait for random interval before next spawn attempt
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnNPC()
    {
        // Pick a random spawn point
        int spawnIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[spawnIndex];

        // Geser semua NPC dalam antrian ke kiri
        foreach (GameObject npcGameObject in npcsInQueue)
        {
            if (npcGameObject != null)
            {
                Vector3 newPosition = npcGameObject.transform.position;
                newPosition.x -= antrianMoveDistance;
                npcGameObject.transform.position = newPosition;
            }
        }

        // Instantiate the NPC
        GameObject npcObject = Instantiate(npcOrang, spawnPoint.position, Quaternion.identity);
        JamuNPC npc = npcObject.GetComponent<JamuNPC>();
        npcsInQueue.Add(npcObject);

        if (npc != null)
        {
            // Configure the NPC
            npc.craftingPanel = craftingPanelReference;
            npc.jamuTypes = availableJamuTypes;
            npc.reward = Random.Range(40, 100);
            npc.CreateRandomJamuRequest();

            // Subscribe to NPC destruction to update count
            StartCoroutine(WatchForNPCDestruction(npcObject));
        }
        else
        {
            Debug.LogError("Spawned object does not have JamuNPC component!");
            Destroy(npcObject);
            npcsInQueue.Remove(npcObject);
            currentNPCCount--;
        }
    }

    IEnumerator WatchForNPCDestruction(GameObject npc)
    {
        while (npc != null)
        {
            yield return new WaitForSeconds(1f);
        }

        // NPC was destroyed, update count
        npcsInQueue.Remove(npc);
        currentNPCCount--;
    }

    IEnumerator SpawnNPCEveryThreeSeconds()
    {
        while (true)
        {
            if (currentNPCCount < maxConcurrentNPCs)
            {
                SpawnNPC();
                currentNPCCount++;
            }
            yield return new WaitForSeconds(3f);
        }
    }
}