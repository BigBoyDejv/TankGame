using UnityEngine;

public class TankHealthBar : MonoBehaviour
{
    [Header("Nastavenia")]
    public TankHealth tankHealth;
    public bool isPlayer = false;
    public Color playerColor   = Color.green;
    public Color enemyColor    = Color.red;
    public Color lowHealthColor = Color.yellow;

    [Header("Vzhľad")]
    public float barWidth  = 80f;
    public float barHeight = 8f;
    public float yOffset   = 2.5f;

    [Header("Line of Sight")]
    public float visibilityRange = 80f; // max vzdialenosť na zobrazenie HP

    private Camera mainCamera;
    private Texture2D texture;
    private Transform playerTransform;

    void Start()
    {
        mainCamera = Camera.main;

        if (tankHealth == null)
            tankHealth = GetComponent<TankHealth>();

        texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    bool HasLineOfSight()
    {
        if (isPlayer) return true; // hráč vždy vidí svoje HP
        if (playerTransform == null) return false;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        if (dist > visibilityRange) return false;

        // Raycast medzi hráčom a enemy — skontroluj či nie je za prekážkou
        Vector3 direction = transform.position - playerTransform.position;
        RaycastHit hit;

        if (Physics.Raycast(playerTransform.position + Vector3.up * 1.5f,
            direction.normalized, out hit, dist))
        {
            // Ak raycast trafil tento objekt alebo jeho child — vidíme ho
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                return true;

            // Ak trafil niečo iné — enemy je za prekážkou
            return false;
        }

        return true; // nič neprekáža
    }

    void OnGUI()
    {
        if (tankHealth == null || mainCamera == null) return;
        if (!HasLineOfSight()) return; // skryj HP ak nie je vizuálny kontakt

        Vector3 worldPos = transform.position + Vector3.up * yOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0) return;

        float screenX = screenPos.x - barWidth / 2f;
        float screenY = Screen.height - screenPos.y - barHeight / 2f;

        float healthPercent = Mathf.Clamp01(tankHealth.currentHealth / tankHealth.maxHealth);

        // Pozadie
        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(screenX - 1, screenY - 1, barWidth + 2, barHeight + 2), texture);

        // HP bar farba
        if (isPlayer)
            GUI.color = healthPercent > 0.3f ? playerColor : lowHealthColor;
        else
            GUI.color = healthPercent > 0.3f ? enemyColor : lowHealthColor;

        GUI.DrawTexture(new Rect(screenX, screenY, barWidth * healthPercent, barHeight), texture);

        // Text
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