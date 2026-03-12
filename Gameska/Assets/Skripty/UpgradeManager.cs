using UnityEngine;
using System.Collections.Generic;

public enum UpgradeType
{
    FireRate,
    Damage,
    MoveSpeed,
    RepairHP
}

[System.Serializable]
public class Upgrade
{
    public UpgradeType type;
    public string title;
    public string description;
    public float value;
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Referencie")]
    public TurretControl turretControl;
    public TankMovement tankMovement;
    public TankHealth playerHealth;

    // Všetky možné upgrady
    private List<Upgrade> allUpgrades = new List<Upgrade>
    {
        new Upgrade { type = UpgradeType.FireRate,  title = "Rýchle nabíjanie",  description = "+25% rýchlosť nabíjania",  value = 0.25f },
        new Upgrade { type = UpgradeType.FireRate,  title = "Auto-loader",        description = "+40% rýchlosť nabíjania",  value = 0.40f },
        new Upgrade { type = UpgradeType.Damage,    title = "AP Granát",          description = "+20 poškodenie",            value = 20f   },
        new Upgrade { type = UpgradeType.Damage,    title = "HEAT Granát",        description = "+35 poškodenie",            value = 35f   },
        new Upgrade { type = UpgradeType.MoveSpeed, title = "Vylepšený motor",    description = "+20% rýchlosť pohybu",     value = 0.20f },
        new Upgrade { type = UpgradeType.MoveSpeed, title = "Turbodúchadlo",      description = "+35% rýchlosť pohybu",     value = 0.35f },
        new Upgrade { type = UpgradeType.RepairHP,  title = "Opravná súprava",    description = "Obnov 30 HP",               value = 30f   },
        new Upgrade { type = UpgradeType.RepairHP,  title = "Pancierový upgrade", description = "+50 max HP",                value = 50f   },
    };

    // 3 aktuálne ponúkané karty
    private Upgrade[] currentChoices = new Upgrade[3];
    public bool IsShowing => isShowing;
    private bool isShowing = false;

    // GUI
    private Texture2D tex;
    private GUIStyle titleStyle;
    private GUIStyle descStyle;
    private GUIStyle btnStyle;
    private GUIStyle levelStyle;
    private bool stylesInit = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Auto-nájdi komponenty ak nie sú nastavené
        if (turretControl == null)
            turretControl = FindObjectOfType<TurretControl>();
        if (tankMovement == null)
            tankMovement = FindObjectOfType<TankMovement>();
        if (playerHealth == null)
        {
            TankHealth[] healths = FindObjectsOfType<TankHealth>();
            foreach (var h in healths)
                if (h.isPlayer) { playerHealth = h; break; }
        }
    }

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.wordWrap = true;

        descStyle = new GUIStyle();
        descStyle.fontSize = 13;
        descStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        descStyle.alignment = TextAnchor.MiddleCenter;
        descStyle.wordWrap = true;

        btnStyle = new GUIStyle();
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.normal.textColor = Color.white;
        btnStyle.alignment = TextAnchor.MiddleCenter;

        levelStyle = new GUIStyle();
        levelStyle.fontSize = 22;
        levelStyle.fontStyle = FontStyle.Bold;
        levelStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        levelStyle.alignment = TextAnchor.MiddleCenter;
    }

    public void ShowUpgradeChoice()
    {
        List<Upgrade> pool = new List<Upgrade>(allUpgrades);
        for (int i = 0; i < 3; i++)
        {
            int idx = Random.Range(0, pool.Count);
            currentChoices[i] = pool[idx];
            pool.RemoveAt(idx);
        }

        isShowing = true;
        Time.timeScale = 0f;

        // Ukáž kurzor pre klikanie na karty
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void ApplyUpgrade(Upgrade upgrade)
    {
        switch (upgrade.type)
        {
            case UpgradeType.FireRate:
                if (turretControl != null)
                    turretControl.fireRate *= (1f + upgrade.value);
                break;

            case UpgradeType.Damage:
                if (turretControl != null)
                    turretControl.shellDamage += upgrade.value;
                break;

            case UpgradeType.MoveSpeed:
                if (tankMovement != null)
                    tankMovement.speed *= (1f + upgrade.value);
                break;

            case UpgradeType.RepairHP:
                if (playerHealth != null)
                {
                    if (upgrade.value == 50f) // max HP upgrade
                    {
                        playerHealth.maxHealth += 50f;
                        playerHealth.currentHealth += 50f;
                    }
                    else // heal
                    {
                        playerHealth.currentHealth = Mathf.Min(
                            playerHealth.currentHealth + upgrade.value,
                            playerHealth.maxHealth
                        );
                    }
                }
                break;
        }

        isShowing = false;
        Time.timeScale = 1f;

        // Schovaj kurzor späť — vráť herný stav
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void OnGUI()
    {
        if (!isShowing) return;
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // Tmavé pozadie
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), tex);
        GUI.color = Color.white;

        // Nadpis
        XPSystem xp = XPSystem.Instance;
        string levelText = xp != null ? $"LEVEL {xp.currentLevel} — Vyber upgrade" : "Vyber upgrade";
        GUI.Label(new Rect(0, sh * 0.12f, sw, 40), levelText, levelStyle);

        // 3 karty
        float cardW = 220f;
        float cardH = 280f;
        float spacing = 30f;
        float totalW = cardW * 3 + spacing * 2;
        float startX = (sw - totalW) / 2f;
        float startY = sh * 0.25f;

        Color[] cardColors = {
            new Color(0.15f, 0.25f, 0.45f, 0.95f), // modrá
            new Color(0.35f, 0.15f, 0.45f, 0.95f), // fialová
            new Color(0.45f, 0.25f, 0.10f, 0.95f), // oranžová
        };

        Color[] borderColors = {
            new Color(0.3f, 0.6f, 1.0f, 1f),
            new Color(0.7f, 0.3f, 1.0f, 1f),
            new Color(1.0f, 0.6f, 0.1f, 1f),
        };

        string[] icons = { "⚡", "💥", "🔧" };

        for (int i = 0; i < 3; i++)
        {
            float cx = startX + i * (cardW + spacing);
            Upgrade up = currentChoices[i];

            // Border
            GUI.color = borderColors[i];
            GUI.DrawTexture(new Rect(cx - 2, startY - 2, cardW + 4, cardH + 4), tex);

            // Karta pozadie
            GUI.color = cardColors[i];
            GUI.DrawTexture(new Rect(cx, startY, cardW, cardH), tex);
            GUI.color = Color.white;

            // Ikona
            GUIStyle iconStyle = new GUIStyle();
            iconStyle.fontSize = 36;
            iconStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx, startY + 20, cardW, 50), icons[i], iconStyle);

            // Typ upgradu (badge)
            Color badgeColor = GetTypeColor(up.type);
            GUI.color = new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.8f);
            GUI.DrawTexture(new Rect(cx + 15, startY + 78, cardW - 30, 24), tex);
            GUI.color = Color.white;
            GUIStyle badgeStyle = new GUIStyle();
            badgeStyle.fontSize = 11;
            badgeStyle.fontStyle = FontStyle.Bold;
            badgeStyle.normal.textColor = Color.white;
            badgeStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx + 15, startY + 78, cardW - 30, 24), GetTypeName(up.type), badgeStyle);

            // Názov
            GUI.Label(new Rect(cx + 10, startY + 112, cardW - 20, 50), up.title, titleStyle);

            // Popis
            GUI.Label(new Rect(cx + 10, startY + 168, cardW - 20, 60), up.description, descStyle);

            // Tlačidlo
            Color btnColor = new Color(borderColors[i].r * 0.7f, borderColors[i].g * 0.7f, borderColors[i].b * 0.7f, 1f);
            GUI.color = btnColor;
            GUI.DrawTexture(new Rect(cx + 20, startY + cardH - 50, cardW - 40, 36), tex);
            GUI.color = Color.white;
            GUI.Label(new Rect(cx + 20, startY + cardH - 50, cardW - 40, 36), "VYBRAŤ", btnStyle);

            // Klik na kartu alebo tlačidlo
            if (GUI.Button(new Rect(cx, startY, cardW, cardH), GUIContent.none, GUIStyle.none))
                ApplyUpgrade(up);
        }

        // Tip
        GUIStyle tipStyle = new GUIStyle();
        tipStyle.fontSize = 11;
        tipStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        tipStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(0, startY + cardH + 20, sw, 20), "Klikni na kartu pre výber", tipStyle);
    }

    Color GetTypeColor(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate:  return new Color(0.2f, 0.6f, 1.0f);
            case UpgradeType.Damage:    return new Color(1.0f, 0.3f, 0.3f);
            case UpgradeType.MoveSpeed: return new Color(0.2f, 0.8f, 0.3f);
            case UpgradeType.RepairHP:  return new Color(1.0f, 0.7f, 0.1f);
            default: return Color.gray;
        }
    }

    string GetTypeName(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.FireRate:  return "NABÍJANIE";
            case UpgradeType.Damage:    return "POŠKODENIE";
            case UpgradeType.MoveSpeed: return "POHYB";
            case UpgradeType.RepairHP:  return "HP / BRNENIE";
            default: return "";
        }
    }
}