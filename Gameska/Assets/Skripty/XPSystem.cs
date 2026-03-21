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
    
    GameObject oldPlayer = GameObject.FindGameObjectWithTag("Player");
    if (oldPlayer != null)
        Destroy(oldPlayer);
    
    Vector3 spawnPos = playerSpawnPoint != null ? 
        playerSpawnPoint.position : 
        new Vector3(0, 1, 0);
        
    currentPlayer = Instantiate(tankModelPrefabs[0], spawnPos, Quaternion.identity);
    currentPlayer.tag = "Player";
    PlayerTransform = currentPlayer.transform;
    
    AttachCameraToPlayer(currentPlayer);
    
    // Počkaj jeden frame aby sa všetko inicializovalo
    Invoke(nameof(InitPlayerComponents), 0.1f);
    
    Debug.Log($"✅ Hra spustená s level 1 tankom na {spawnPos}");
}
void InitPlayerComponents()
{
    if (currentPlayer == null) return;

    // UpgradeManager
    if (UpgradeManager.Instance != null)
    {
        UpgradeManager.Instance.turretControl = currentPlayer.GetComponent<TurretControl>();
        UpgradeManager.Instance.tankMovement = currentPlayer.GetComponent<TankMovement>();
        UpgradeManager.Instance.playerHealth = currentPlayer.GetComponent<TankHealth>();
    }

    // TankHealthBar na playerovi
    TankHealthBar playerBar = currentPlayer.GetComponent<TankHealthBar>();
    if (playerBar != null)
    {
        playerBar.tankHealth = currentPlayer.GetComponent<TankHealth>();
        playerBar.isPlayer = true;
    }

    // SkillSystem
    SkillSystem skillSystem = currentPlayer.GetComponent<SkillSystem>();
    if (skillSystem != null)
        skillSystem.InitReferences(currentPlayer);

    Debug.Log("✅ Player komponenty inicializované");
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

        // Zmeň tank len na leveloch 3 a 5
        if (currentLevel == 3 || currentLevel == 5)
            SwitchPlayerModel();

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ShowUpgradeChoice();
    }
}

    void SwitchPlayerModel()
{
    // Tank sa mení len na konkrétnych leveloch: 1, 3, 5
    int modelIndex = -1;
    
    if (currentLevel >= 5 && tankModelPrefabs.Length > 2)
        modelIndex = 2;
    else if (currentLevel >= 3 && tankModelPrefabs.Length > 1)
        modelIndex = 1;
    else
        modelIndex = 0;

    // Ak je model rovnaký ako teraz, nemeníme
    // (aby sa neresetoval tank na každý level)
    
    CameraFollow camFollow = mainCamera.GetComponent<CameraFollow>();
    if (camFollow != null) camFollow.tankBody = null;

    Vector3 position = currentPlayer.transform.position;
    Quaternion rotation = currentPlayer.transform.rotation;

    Destroy(currentPlayer);

    currentPlayer = Instantiate(tankModelPrefabs[modelIndex], position, rotation);
    currentPlayer.tag = "Player";
    PlayerTransform = currentPlayer.transform;

    mainCamera.transform.SetParent(currentPlayer.transform);
    mainCamera.transform.localPosition = new Vector3(0, 5, -10);
    mainCamera.transform.localRotation = Quaternion.Euler(10, 0, 0);

    if (camFollow != null)
    {
        camFollow.tankBody = currentPlayer.transform;
        camFollow.enabled = true;
    }

    if (UpgradeManager.Instance != null)
    {
        UpgradeManager.Instance.turretControl = currentPlayer.GetComponent<TurretControl>();
        UpgradeManager.Instance.tankMovement = currentPlayer.GetComponent<TankMovement>();
        UpgradeManager.Instance.playerHealth = currentPlayer.GetComponent<TankHealth>();
    }

    Invoke(nameof(InitPlayerComponents), 0.1f);
    
    Debug.Log($"✅ Tank level {currentLevel} — model index {modelIndex}");
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