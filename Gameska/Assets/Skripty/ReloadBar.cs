using UnityEngine;

public class ReloadBar : MonoBehaviour
{
    [Header("Referencie")]
    public TurretControl turretControl;

    [Header("Pozícia a veľkosť")]
    public float barWidth = 200f;
    public float barHeight = 16f;
    public float bottomOffset = 60f;

    [Header("Farby")]
    public Color reloadingColor = new Color(1f, 0.6f, 0f, 1f);
    public Color readyColor = new Color(0f, 1f, 0f, 1f);
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    private Texture2D tex;
    private float reloadProgress = 1f;
    private float lastFireTime = -999f;
    private bool isReady = true;

    void Start()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        if (turretControl == null)
            turretControl = FindObjectOfType<TurretControl>();
    }

    public void OnFired()
    {
        lastFireTime = Time.time;
        isReady = false;
    }

    void Update()
    {
        if (turretControl == null) return;

        float timeSinceFire = Time.time - lastFireTime;
        float reloadTime = 1f / turretControl.fireRate;

        reloadProgress = Mathf.Clamp01(timeSinceFire / reloadTime);
        isReady = reloadProgress >= 1f;
    }

    void OnGUI()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height - bottomOffset;

        // Pozadie
        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(cx - barWidth/2f - 2, cy - 2, barWidth + 4, barHeight + 4), tex);

        // Progress bar
        GUI.color = isReady ? readyColor : reloadingColor;
        GUI.DrawTexture(new Rect(cx - barWidth/2f, cy, barWidth * reloadProgress, barHeight), tex);

        // Text
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        string text = isReady ? "PRIPRAVENÉ" : $"Nabíjanie... {(int)(reloadProgress * 100f)}%";
        GUI.Label(new Rect(cx - barWidth/2f, cy, barWidth, barHeight), text, style);

        GUI.color = Color.white;
    }
}
