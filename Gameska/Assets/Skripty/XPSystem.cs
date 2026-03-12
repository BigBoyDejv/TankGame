using UnityEngine;

public class XPSystem : MonoBehaviour
{
    public static XPSystem Instance;

    [Header("XP Nastavenia")]
    public int xpPerKill = 100;
    public int xpToNextLevel = 400; // každé 4 zabití = level up
    public float xpScaling = 1.2f;  // každý level treba viac XP

    [Header("Aktuálny stav")]
    public int currentXP = 0;
    public int currentLevel = 0;
    public int totalKills = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
            UpgradeManager.Instance.ShowUpgradeChoice();
        }
    }
}
