using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class AmmoSlot
{
    public AmmoType type;
    public string   name;
    public string   icon;
    public int      count;       // -1 = nekonečno (Standard)
    public Color    color;
}

public class AmmoManager : MonoBehaviour
{
    public static AmmoManager Instance;

    [Header("Dostupná munícia")]
    public List<AmmoSlot> ammoSlots = new List<AmmoSlot>
    {
        new AmmoSlot { type = AmmoType.Standard,  name = "Štandard",  icon = "●", count = -1, color = new Color(0.8f, 0.8f, 0.8f) },
        new AmmoSlot { type = AmmoType.Scatter,   name = "Scatter",   icon = "⊕", count =  8, color = new Color(0.9f, 0.7f, 0.1f) },
        new AmmoSlot { type = AmmoType.Ricochet,  name = "Ricochet",  icon = "↯", count =  6, color = new Color(0.2f, 0.7f, 1.0f) },
        new AmmoSlot { type = AmmoType.Napalm,    name = "Napalm",    icon = "🔥", count =  4, color = new Color(1.0f, 0.3f, 0.0f) },
    };

    [Header("Scatter nastavenia")]
    public int   scatterCount  = 5;
    public float scatterSpread = 15f;
    public float scatterSpeed  = 55f;

    private int currentIndex = 0;
    public AmmoType CurrentAmmo => ammoSlots[currentIndex].type;

    private Texture2D tex;
    private TurretControl turretControl;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        turretControl = FindObjectOfType<TurretControl>();
    }

    void Update()
    {
        // Q = predošlá munícia, E = ďalšia
        if (Input.GetKeyDown(KeyCode.Q)) CycleAmmo(-1);
        if (Input.GetKeyDown(KeyCode.E)) CycleAmmo(1);

        // Číselné klávesy 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectAmmo(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SelectAmmo(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SelectAmmo(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SelectAmmo(3);
    }

    void CycleAmmo(int dir)
    {
        currentIndex = (currentIndex + dir + ammoSlots.Count) % ammoSlots.Count;
    }

    void SelectAmmo(int idx)
    {
        if (idx < ammoSlots.Count) currentIndex = idx;
    }

    public bool UseAmmo()
    {
        AmmoSlot slot = ammoSlots[currentIndex];
        if (slot.count == -1) return true; // nekonečno
        if (slot.count <= 0)
        {
            // Nemá muníciu — prepni na Standard
            currentIndex = 0;
            return true;
        }
        slot.count--;
        return true;
    }

    // Volá TurretControl — vráti zoznam shell objektov (Scatter = viac)
    public List<(Vector3 pos, Vector3 dir, AmmoType type)> GetShots(
        Vector3 firePos, Vector3 baseDir)
    {
        var shots = new List<(Vector3, Vector3, AmmoType)>();
        AmmoType type = CurrentAmmo;

        if (type == AmmoType.Scatter)
        {
            for (int i = 0; i < scatterCount; i++)
            {
                Vector3 spread = baseDir + new Vector3(
                    Random.Range(-scatterSpread, scatterSpread) * 0.01f,
                    Random.Range(-scatterSpread * 0.3f, scatterSpread * 0.3f) * 0.01f,
                    Random.Range(-scatterSpread, scatterSpread) * 0.01f
                );
                shots.Add((firePos, spread.normalized, type));
            }
        }
        else
        {
            shots.Add((firePos, baseDir, type));
        }

        return shots;
    }

    void OnGUI()
    {
        if (tex == null) return;

        float sw = Screen.width;
        float sh = Screen.height;

        float slotW = 70f;
        float slotH = 65f;
        float spacing = 6f;
        float totalW = ammoSlots.Count * (slotW + spacing) - spacing;
        float startX = (sw - totalW) / 2f;
        float startY = sh - 240f; // nad reload barom

        for (int i = 0; i < ammoSlots.Count; i++)
        {
            AmmoSlot slot = ammoSlots[i];
            float x = startX + i * (slotW + spacing);
            bool selected = i == currentIndex;
            bool empty    = slot.count == 0;

            // Pozadie
            GUI.color = selected
                ? new Color(slot.color.r * 0.5f, slot.color.g * 0.5f, slot.color.b * 0.5f, 0.95f)
                : new Color(0.08f, 0.08f, 0.08f, 0.85f);
            GUI.DrawTexture(new Rect(x, startY, slotW, slotH), tex);

            // Border — hrubší keď selected
            GUI.color = selected ? slot.color : new Color(0.3f, 0.3f, 0.3f, 0.8f);
            float bw = selected ? 2f : 1f;
            GUI.DrawTexture(new Rect(x - bw,         startY - bw,         slotW + bw*2, bw),          tex);
            GUI.DrawTexture(new Rect(x - bw,         startY + slotH,      slotW + bw*2, bw),          tex);
            GUI.DrawTexture(new Rect(x - bw,         startY - bw,         bw,           slotH + bw*2), tex);
            GUI.DrawTexture(new Rect(x + slotW,      startY - bw,         bw,           slotH + bw*2), tex);

            GUI.color = empty ? new Color(0.5f, 0.5f, 0.5f) : Color.white;

            // Ikona
            GUIStyle iconStyle = new GUIStyle();
            iconStyle.fontSize  = 20;
            iconStyle.alignment = TextAnchor.MiddleCenter;
            iconStyle.normal.textColor = empty ? Color.gray : slot.color;
            GUI.Label(new Rect(x, startY + 6, slotW, 26), slot.icon, iconStyle);

            // Meno
            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize  = 9;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.normal.textColor = empty ? Color.gray : Color.white;
            GUI.Label(new Rect(x, startY + 32, slotW, 14), slot.name, nameStyle);

            // Počet
            GUIStyle cntStyle = new GUIStyle();
            cntStyle.fontSize  = 10;
            cntStyle.fontStyle = FontStyle.Bold;
            cntStyle.alignment = TextAnchor.MiddleCenter;
            cntStyle.normal.textColor = empty ? Color.red : new Color(1f, 0.85f, 0.2f);
            string countStr = slot.count == -1 ? "∞" : (empty ? "PRÁZDNE" : slot.count.ToString());
            GUI.Label(new Rect(x, startY + 46, slotW, 16), countStr, cntStyle);

            // Klávesa hint
            GUIStyle keyStyle = new GUIStyle();
            keyStyle.fontSize  = 8;
            keyStyle.alignment = TextAnchor.MiddleCenter;
            keyStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
            GUI.Label(new Rect(x, startY - 14, slotW, 12), $"[{i + 1}]", keyStyle);

            GUI.color = Color.white;
        }
    }
}