using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private bool isPaused = false;
    private Texture2D tex;

    private GUIStyle titleStyle;
    private GUIStyle btnStyle;
    private GUIStyle overlayStyle;
    private bool stylesInit = false;

    void Start()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        titleStyle = new GUIStyle();
        titleStyle.fontSize = 28;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        btnStyle = new GUIStyle();
        btnStyle.fontSize = 16;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.normal.textColor = Color.white;
        btnStyle.alignment = TextAnchor.MiddleCenter;
    }

    void Update()
    {
        // Escape toggle pauza — ale nie keď je upgrade menu otvorené
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (UpgradeManager.Instance != null &&
                UpgradeManager.Instance.IsShowing) return;

            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            Cursor.visible = isPaused;
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Confined;
        }
    }

    void OnGUI()
    {
        if (!isPaused) return;
        InitStyles();

        float sw = Screen.width;
        float sh = Screen.height;

        // Tmavé pozadie
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), tex);

        // Panel
        float panelW = 320f;
        float panelH = 320f;
        float px = (sw - panelW) / 2f;
        float py = (sh - panelH) / 2f;

        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.97f);
        GUI.DrawTexture(new Rect(px, py, panelW, panelH), tex);

        // Border
        GUI.color = new Color(0.4f, 0.6f, 0.4f, 0.9f);
        GUI.DrawTexture(new Rect(px - 2,          py - 2,          panelW + 4, 2),          tex);
        GUI.DrawTexture(new Rect(px - 2,          py + panelH,     panelW + 4, 2),          tex);
        GUI.DrawTexture(new Rect(px - 2,          py - 2,          2,          panelH + 4), tex);
        GUI.DrawTexture(new Rect(px + panelW,     py - 2,          2,          panelH + 4), tex);

        GUI.color = Color.white;

        // Nadpis
        GUI.Label(new Rect(px, py + 30, panelW, 40), "⏸  PAUZA", titleStyle);

        // Čiara
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        GUI.DrawTexture(new Rect(px + 20, py + 80, panelW - 40, 1), tex);
        GUI.color = Color.white;

        // Tlačidlá
        float btnW = 220f;
        float btnH = 48f;
        float bx = px + (panelW - btnW) / 2f;

        if (DrawButton(bx, py + 100, btnW, btnH, "▶  Pokračovať",
            new Color(0.15f, 0.35f, 0.15f, 0.95f)))
        {
            Resume();
        }

        if (DrawButton(bx, py + 165, btnW, btnH, "↺  Reštartovať",
            new Color(0.25f, 0.25f, 0.1f, 0.95f)))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (DrawButton(bx, py + 230, btnW, btnH, "✕  Ukončiť hru",
            new Color(0.35f, 0.1f, 0.1f, 0.95f)))
        {
            Time.timeScale = 1f;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    bool DrawButton(float x, float y, float w, float h, string label, Color bgColor)
    {
        GUI.color = bgColor;
        GUI.DrawTexture(new Rect(x, y, w, h), tex);
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, h), label, btnStyle);
        return GUI.Button(new Rect(x, y, w, h), GUIContent.none, GUIStyle.none);
    }

    void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }
}
