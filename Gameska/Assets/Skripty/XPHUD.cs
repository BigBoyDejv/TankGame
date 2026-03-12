using UnityEngine;

public class XPHUD : MonoBehaviour
{
    private Texture2D tex;

    void Start()
    {
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    void OnGUI()
    {
        XPSystem xp = XPSystem.Instance;
        if (xp == null) return;

        float sw = Screen.width;
        float sh = Screen.height;

        float barW = 300f;
        float barH = 14f;
        float bx = (sw - barW) / 2f;
        float by = sh - 100f; // nad reload barom

        float progress = xp.xpToNextLevel > 0
            ? (float)xp.currentXP / xp.xpToNextLevel
            : 1f;

        // Pozadie
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
        GUI.DrawTexture(new Rect(bx - 2, by - 2, barW + 4, barH + 4), tex);

        // XP bar — zlatá farba
        GUI.color = new Color(1f, 0.78f, 0.1f, 1f);
        GUI.DrawTexture(new Rect(bx, by, barW * progress, barH), tex);

        // Text
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 11;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        GUI.Label(
            new Rect(bx, by - 18, barW, 16),
            $"Level {xp.currentLevel}  •  XP: {xp.currentXP} / {xp.xpToNextLevel}  •  Zabití: {xp.totalKills}",
            style
        );

        GUI.color = Color.white;
    }
}
