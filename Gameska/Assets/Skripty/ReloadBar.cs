using UnityEngine;

public class ReloadBar : MonoBehaviour
{
    [Header("Referencie")]
    public TurretControl turretControl;

    [Header("Pozícia a veľkosť")]
    public float barWidth = 220f;
    public float barHeight = 18f;
    public float bottomOffset = 65f;

    [Header("Farby")]
    public Color reloadingColor = new Color(1f, 0.55f, 0f, 1f);
    public Color readyColor     = new Color(0.1f, 0.9f, 0.1f, 1f);
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.85f);

    private Texture2D tex;
    private float reloadProgress = 1f;
    private float lastFireTime   = -999f;
    private float reloadTime     = 1f;
    private bool  isReady        = true;

    void Awake()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    void Start()
    {
        if (turretControl == null)
            turretControl = FindObjectOfType<TurretControl>();

        if (turretControl != null)
            reloadTime = 1f / turretControl.fireRate;
    }

    public void OnFired()
    {
        lastFireTime = Time.time;
        isReady      = false;

        // Obnov reloadTime pri každom výstrele — fireRate sa môže zmeniť cez upgrade
        if (turretControl != null)
            reloadTime = 1f / turretControl.fireRate;
    }

    void Update()
    {
        // Skús nájsť TurretControl ak ešte nie je
        if (turretControl == null)
        {
            turretControl = FindObjectOfType<TurretControl>();
            if (turretControl != null)
                reloadTime = 1f / turretControl.fireRate;
            return;
        }

        float timeSinceFire = Time.time - lastFireTime;
        reloadProgress = Mathf.Clamp01(timeSinceFire / reloadTime);
        isReady        = reloadProgress >= 1f;
    }

    void OnGUI()
    {
        if (tex == null) return;

        float cx = Screen.width  / 2f;
        float cy = Screen.height - bottomOffset;

        // Pozadie
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(cx - barWidth/2f - 2, cy - 2, barWidth + 4, barHeight + 4), tex);

        // Progress bar
        GUI.color = isReady ? readyColor : reloadingColor;
        GUI.DrawTexture(new Rect(cx - barWidth/2f, cy, barWidth * reloadProgress, barHeight), tex);

        // Ohraničenie
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(cx - barWidth/2f - 1, cy - 1, barWidth + 2, 1),            tex);
        GUI.DrawTexture(new Rect(cx - barWidth/2f - 1, cy + barHeight, barWidth + 2, 1),    tex);
        GUI.DrawTexture(new Rect(cx - barWidth/2f - 1, cy - 1, 1, barHeight + 2),           tex);
        GUI.DrawTexture(new Rect(cx + barWidth/2f,     cy - 1, 1, barHeight + 2),           tex);

        // Text
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize        = 11;
        style.fontStyle       = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment       = TextAnchor.MiddleCenter;

        string text = isReady
            ? "▶  PRIPRAVENÉ"
            : $"↺  Nabíjanie...  {(int)(reloadProgress * 100f)}%";

        GUI.Label(new Rect(cx - barWidth/2f, cy, barWidth, barHeight), text, style);
        GUI.color = Color.white;
    }
}