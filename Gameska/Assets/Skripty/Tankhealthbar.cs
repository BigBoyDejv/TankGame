using UnityEngine;

public class TankHealthBar : MonoBehaviour
{
    [Header("Nastavenia")]
    public TankHealth tankHealth;
    public bool isPlayer = false;
    public Color playerColor = Color.green;
    public Color enemyColor = Color.red;
    public Color lowHealthColor = Color.yellow;

    [Header("Vzhľad")]
    public float barWidth = 80f;
    public float barHeight = 8f;
    public float yOffset = 2.5f;  // výška nad tankom

    private Camera mainCamera;
    private Texture2D texture;

    void Start()
    {
        mainCamera = Camera.main;

        if (tankHealth == null)
            tankHealth = GetComponent<TankHealth>();

        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
    }

    void OnGUI()
    {
        if (tankHealth == null || mainCamera == null) return;

        // Konvertuj 3D pozíciu nad tankom na 2D screen pozíciu
        Vector3 worldPos = transform.position + Vector3.up * yOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // Ak je tank za kamerou, neskresli
        if (screenPos.z < 0) return;

        float screenX = screenPos.x - barWidth / 2f;
        float screenY = Screen.height - screenPos.y - barHeight / 2f;

        float healthPercent = tankHealth.currentHealth / tankHealth.maxHealth;
        healthPercent = Mathf.Clamp01(healthPercent);

        // Pozadie (tmavé)
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(screenX - 1, screenY - 1, barWidth + 2, barHeight + 2), texture);

        // Farba podľa HP a typu tanku
        if (isPlayer)
        {
            GUI.color = healthPercent > 0.3f ? playerColor : lowHealthColor;
        }
        else
        {
            GUI.color = healthPercent > 0.3f ? enemyColor : lowHealthColor;
        }

        // HP bar
        GUI.DrawTexture(new Rect(screenX, screenY, barWidth * healthPercent, barHeight), texture);

        // Text s HP hodnotami
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 10;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        string hpText = isPlayer
            ? $"HP: {(int)tankHealth.currentHealth}/{(int)tankHealth.maxHealth}"
            : $"{(int)tankHealth.currentHealth}";

        GUI.Label(new Rect(screenX, screenY - 14, barWidth, 14), hpText, style);

        GUI.color = Color.white;
    }
}