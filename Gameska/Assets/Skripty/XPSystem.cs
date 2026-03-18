using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance;
    public static Transform PlayerTransform; // 🔥 STATICKÁ REFERENCIA PRE NEPRIATEĽOV

    [Header("XP Nastavenia")]
    public int xpPerKill = 100;
    public int xpToNextLevel = 400;
    public float xpScaling = 1.2f;

    [Header("Aktuálny stav")]
    public int currentXP = 0;
    public int currentLevel = 1;
    public int totalKills = 0;

    [Header("Tank Model Prefaby")]
    public GameObject[] tankModelPrefabs;

    [Header("Spawn nastavenia")]
    public Transform playerSpawnPoint; // 🎯 SEM PRETIAHNI SVOJ SPAWN

    private GameObject currentPlayer;
    private Camera mainCamera;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        mainCamera = Camera.main;
        
        // Zmaž existujúceho hráča ak nejaký je
        GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
        if (oldPlayer != null)
            Destroy(oldPlayer);
        
        // Vytvor nového hráča na spawne
        Vector3 spawnPos = playerSpawnPoint != null ? 
            playerSpawnPoint.position : 
            new Vector3(0, 1, 0);
            
        currentPlayer = Instantiate(tankModelPrefabs[0], spawnPos, Quaternion.identity);
        currentPlayer.tag = "Player";
        
        // 🔥 ULOŽÍME REFERENCIU PRE NEPRIATEĽOV
        PlayerTransform = currentPlayer.transform;
        
        // Pripoj kameru
        AttachCameraToPlayer(currentPlayer);
        
        Debug.Log($"✅ Hra spustená s level 1 tankom na {spawnPos}");
    }

    public void AddKill()
    {
        totalKills++;
        currentXP += xpPerKill;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpScaling);
            SwitchPlayerModel();
            
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.ShowUpgradeChoice();
        }
    }

    void SwitchPlayerModel()
    {
        int modelIndex = currentLevel - 1;
        if (modelIndex < 0 || modelIndex >= tankModelPrefabs.Length) return;

        // ULOŽ KAMERU A SKILLS
        Transform cameraParent = mainCamera.transform.parent;
        CameraFollow camFollow = mainCamera.GetComponent<CameraFollow>();
        if (camFollow != null) camFollow.tankBody = null; // Odpoj

        // ULOŽ pozíciu
        Vector3 position = currentPlayer.transform.position;
        Quaternion rotation = currentPlayer.transform.rotation;

        // ZNIČ STARÝ
        Destroy(currentPlayer);

        // VYTVOŔ NOVÝ
        currentPlayer = Instantiate(tankModelPrefabs[modelIndex], position, rotation);
        currentPlayer.tag = "Player";

        // 🔥 AKTUALIZUJEME REFERENCIU PRE NEPRIATEĽOV
        PlayerTransform = currentPlayer.transform;

        // OBNOV KAMERU
        mainCamera.transform.SetParent(currentPlayer.transform);
        mainCamera.transform.localPosition = new Vector3(0, 5, -10);
        mainCamera.transform.localRotation = Quaternion.Euler(10, 0, 0);
        
        // OBNOV CameraFollow
        if (camFollow != null) 
        {
            camFollow.tankBody = currentPlayer.transform;
            camFollow.enabled = true;
        }

        Debug.Log($"✅ Tank {currentLevel} + kamera obnovená");
    }

    void AttachCameraToPlayer(GameObject player)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (mainCamera == null) return;

        mainCamera.transform.SetParent(player.transform);
        mainCamera.transform.localPosition = new Vector3(0, 5, -10);
        mainCamera.transform.localRotation = Quaternion.Euler(10, 0, 0);
    }
}