using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("Nastavenia")]
    public float mapSize = 200f;        // veľkosť minimapy v pixeloch
    public float worldSize = 150f;      // koľko world units pokrýva mapa
    public Vector2 position = new Vector2(10f, 10f); // od pravého horného rohu

    [Header("Farby")]
    public Color backgroundColor = new Color(0.05f, 0.1f, 0.05f, 0.85f);
    public Color playerColor     = new Color(0.1f, 0.9f, 0.1f, 1f);
    public Color enemyColor      = new Color(0.9f, 0.15f, 0.15f, 1f);
    public Color borderColor     = new Color(0.4f, 0.8f, 0.4f, 0.9f);

    private Texture2D tex;
    private Transform playerTransform;

    void Start()
{
    tex = new Texture2D(1, 1);
    tex.SetPixel(0, 0, Color.white);
    tex.Apply();
    // Nezačíname hľadať player tu
}

void Update()
{
    // Aktualizuj playerTransform cez XPSystem
    if (XPSystem.PlayerTransform != null)
        playerTransform = XPSystem.PlayerTransform;
    else if (playerTransform == null)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }
}

    void OnGUI()
    {
        if (playerTransform == null) return;

        float sw = Screen.width;
        float mx = sw - position.x - mapSize;
        float my = position.y;

        // Pozadie
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(mx, my, mapSize, mapSize), tex);

        // Border
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(mx - 2,           my - 2,           mapSize + 4, 2),          tex);
        GUI.DrawTexture(new Rect(mx - 2,           my + mapSize,     mapSize + 4, 2),          tex);
        GUI.DrawTexture(new Rect(mx - 2,           my - 2,           2,           mapSize + 4), tex);
        GUI.DrawTexture(new Rect(mx + mapSize,     my - 2,           2,           mapSize + 4), tex);

        Vector3 playerPos = playerTransform.position;

        // Hráč — šípka ukazujúca smer
        DrawMapIcon(mx, my, playerPos, playerPos, playerTransform.eulerAngles.y, playerColor, 7f, true);

        // Nepriatelia
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            DrawMapIcon(mx, my, enemy.transform.position, playerPos,
                enemy.transform.eulerAngles.y, enemyColor, 5f, false);
        }

        // Sever indikátor
        GUI.color = new Color(1f, 1f, 1f, 0.4f);
        GUIStyle northStyle = new GUIStyle();
        northStyle.fontSize = 9;
        northStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        northStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(mx, my + 2, mapSize, 14), "N", northStyle);

        GUI.color = Color.white;
    }

    void DrawMapIcon(float mx, float my, Vector3 worldPos, Vector3 centerPos,
                     float yaw, Color color, float size, bool isPlayer)
    {
        float halfMap = worldSize / 2f;

        float relX = worldPos.x - centerPos.x;
        float relZ = worldPos.z - centerPos.z;

        // Clamp na hranice mapy
        relX = Mathf.Clamp(relX, -halfMap, halfMap);
        relZ = Mathf.Clamp(relZ, -halfMap, halfMap);

        float screenX = mx + mapSize / 2f + (relX / halfMap) * (mapSize / 2f);
        float screenY = my + mapSize / 2f - (relZ / halfMap) * (mapSize / 2f);

        GUI.color = color;

        if (isPlayer)
        {
            // Trojuholník = šípka smeru hráča
            DrawArrow(screenX, screenY, yaw, size);
        }
        else
        {
            // Štvorec pre enemy
            GUI.DrawTexture(new Rect(screenX - size/2f, screenY - size/2f, size, size), tex);
        }
    }

    void DrawArrow(float x, float y, float yaw, float size)
    {
        // Jednoduchý trojuholník cez 3 bodky
        float rad = yaw * Mathf.Deg2Rad;
        float s = size;

        // Špička
        float tx = x + Mathf.Sin(rad) * s;
        float ty = y - Mathf.Cos(rad) * s;

        // Základňa ľavá
        float lx = x + Mathf.Sin(rad - 2.4f) * s * 0.7f;
        float ly = y - Mathf.Cos(rad - 2.4f) * s * 0.7f;

        // Základňa pravá
        float rx = x + Mathf.Sin(rad + 2.4f) * s * 0.7f;
        float ry = y - Mathf.Cos(rad + 2.4f) * s * 0.7f;

        // Kresli čiary špička→ľavá, špička→pravá, ľavá→pravá
        DrawLine(tx, ty, lx, ly, 1.5f);
        DrawLine(tx, ty, rx, ry, 1.5f);
        DrawLine(lx, ly, rx, ry, 1.5f);
    }

    void DrawLine(float x1, float y1, float x2, float y2, float thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float len = Mathf.Sqrt(dx*dx + dy*dy);
        if (len < 0.01f) return;

        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
        GUI.DrawTexture(new Rect(x1, y1 - thickness/2f, len, thickness), tex);
        GUI.matrix = Matrix4x4.identity;
    }
}
