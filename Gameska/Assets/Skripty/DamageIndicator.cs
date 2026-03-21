using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DamageIndicator : MonoBehaviour
{
    [Header("Nastavenia")]
    public float indicatorDuration = 0.5f;
    public float indicatorRadius = 80f;
    public Color indicatorColor = new Color(1f, 0f, 0f, 0.8f);

    private List<DamageInfo> activeIndicators = new List<DamageInfo>();
    private Texture2D tex;
    private Transform playerTransform;

    private class DamageInfo
    {
        public Vector3 hitDirection;
        public float timeRemaining;
    }

   void Start()
{
    tex = new Texture2D(1, 1);
    tex.SetPixel(0, 0, Color.white);
    tex.Apply();
}

    public void ShowDamage(Vector3 hitFromPosition)
    {
        if (playerTransform == null) return;

        Vector3 dir = hitFromPosition - playerTransform.position;
        dir.y = 0f;

        activeIndicators.Add(new DamageInfo
        {
            hitDirection = dir.normalized,
            timeRemaining = indicatorDuration
        });
    }

    void Update()
{
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

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        foreach (var indicator in activeIndicators)
        {
            float alpha = indicator.timeRemaining / indicatorDuration;

            // Uhol medzi smerom hráča a smerom zásahu
            float angle = Vector3.SignedAngle(
                playerTransform.forward,
                indicator.hitDirection,
                Vector3.up
            );

            float rad = angle * Mathf.Deg2Rad;
            float arrowX = cx + Mathf.Sin(rad) * indicatorRadius;
            float arrowY = cy - Mathf.Cos(rad) * indicatorRadius;

            // Šípka
            GUI.color = new Color(indicatorColor.r, indicatorColor.g, indicatorColor.b, alpha);
            DrawArrow(arrowX, arrowY, angle);
        }

        GUI.color = Color.white;
    }

    void DrawArrow(float x, float y, float angle)
    {
        float size = 20f;
        float thickness = 4f;

        // Jednoduchý trojuholník ako šípka
        GUI.DrawTexture(new Rect(x - thickness/2f, y - size/2f, thickness, size), tex);

        // Hlavička šípky
        GUI.DrawTexture(new Rect(x - size/3f, y - size/2f, size/1.5f, thickness), tex);
        GUI.DrawTexture(new Rect(x - size/3f, y - size/2f, thickness, size/3f), tex);
        GUI.DrawTexture(new Rect(x + size/3f - thickness, y - size/2f, thickness, size/3f), tex);
    }
}
