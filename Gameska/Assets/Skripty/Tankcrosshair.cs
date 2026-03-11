using UnityEngine;

public class TankCrosshair : MonoBehaviour
{
    [Header("Reticle kde hráč mieri (myš)")]
    public Color aimColor = new Color(1f, 1f, 1f, 0.9f);
    public float aimSize = 20f;
    public float aimGap = 8f;
    public float aimThickness = 2f;

    [Header("Reticle kde guľka skutočne dopadne (hlaveň)")]
    public Color gunColor = new Color(0f, 1f, 0f, 0.9f);
    public float gunSize = 16f;
    public float gunGap = 6f;
    public float gunThickness = 2f;
    public float gunCircleRadius = 5f;

    [Header("Animácia reticlu (dispersia)")]
    public float maxDispersion = 40f;   // max rozptyl v pixeloch
    public float dispersionSpeed = 3f;  // ako rýchlo rastie
    public float recoverySpeed = 2f;    // ako rýchlo sa vracia

    private Texture2D tex;
    private float currentDispersion = 0f;
    private bool wasFired = false;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    public void OnShot()
    {
        currentDispersion = maxDispersion;
    }

    void Update()
    {
        // Reticle sa zmenšuje keď tank stojí, zväčšuje pri pohybe
        bool isMoving = Input.GetAxis("Vertical") != 0f || Input.GetAxis("Horizontal") != 0f;

        if (isMoving)
            currentDispersion = Mathf.MoveTowards(currentDispersion, maxDispersion * 0.5f, dispersionSpeed * Time.deltaTime * 20f);
        else
            currentDispersion = Mathf.MoveTowards(currentDispersion, 0f, recoverySpeed * Time.deltaTime * 10f);
    }

    void OnGUI()
    {
        SniperMode sniper = Camera.main != null ? Camera.main.GetComponent<SniperMode>() : null;
        if (sniper != null && sniper.IsSniping) return; // v sniper mode má vlastný crosshair

        DrawAimReticle();
        DrawGunReticle();
    }

    void DrawAimReticle()
    {
        // Biely reticle — sleduje myš, animovaný (dispersia)
        float cx = Input.mousePosition.x;
        float cy = Screen.height - Input.mousePosition.y;
        float gap = aimGap + currentDispersion;
        float size = aimSize;
        float t = aimThickness;

        GUI.color = aimColor;
        // 4 čiary s medzerou
        GUI.DrawTexture(new Rect(cx - size - gap, cy - t/2f, size, t), tex);
        GUI.DrawTexture(new Rect(cx + gap, cy - t/2f, size, t), tex);
        GUI.DrawTexture(new Rect(cx - t/2f, cy - size - gap, t, size), tex);
        GUI.DrawTexture(new Rect(cx - t/2f, cy + gap, t, size), tex);
        // Bodka
        GUI.DrawTexture(new Rect(cx - t, cy - t, t*2f, t*2f), tex);
        GUI.color = Color.white;
    }

    void DrawGunReticle()
    {
        // Zelený reticle — kde guľka skutočne dopadne
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(TurretControl.AimPoint);
        if (sp.z < 0) return;

        float cx = sp.x;
        float cy = Screen.height - sp.y;

        if (cx < 0 || cx > Screen.width || cy < 0 || cy > Screen.height) return;

        float gap = gunGap;
        float size = gunSize;
        float t = gunThickness;

        GUI.color = gunColor;
        // 4 čiary s medzerou
        GUI.DrawTexture(new Rect(cx - size - gap, cy - t/2f, size, t), tex);
        GUI.DrawTexture(new Rect(cx + gap, cy - t/2f, size, t), tex);
        GUI.DrawTexture(new Rect(cx - t/2f, cy - size - gap, t, size), tex);
        GUI.DrawTexture(new Rect(cx - t/2f, cy + gap, t, size), tex);

        // Kruh okolo
        DrawCircle(cx, cy, gunCircleRadius, t);
        GUI.color = Color.white;
    }

    void DrawCircle(float cx, float cy, float radius, float thickness)
    {
        int segments = 24;
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i * step * Mathf.Deg2Rad;
            float x = cx + Mathf.Cos(a) * radius - thickness/2f;
            float y = cy + Mathf.Sin(a) * radius - thickness/2f;
            GUI.DrawTexture(new Rect(x, y, thickness, thickness), tex);
        }
    }
}