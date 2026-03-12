using UnityEngine;

// Pomocný skript pre Airstrike ground warning — samostatný objekt
public class AirstrikeWarningHelper : MonoBehaviour
{
    private Vector3 center;
    private float   radius;
    private float   timeRemaining;
    private float   totalTime;
    private LineRenderer ring;
    private LineRenderer innerRing;

    public void Init(Vector3 c, float r, float duration)
    {
        center        = c;
        radius        = r;
        timeRemaining = duration;
        totalTime     = duration;

        ring = gameObject.AddComponent<LineRenderer>();
        SetupLR(ring, radius, 0.3f);

        GameObject innerGO = new GameObject("Inner");
        innerGO.transform.SetParent(transform);
        innerRing = innerGO.AddComponent<LineRenderer>();
        SetupLR(innerRing, radius * 0.2f, 0.2f);
    }

    void SetupLR(LineRenderer lr, float r, float width)
    {
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = 64;
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 10;
        SetPoints(lr, r);
    }

    void SetPoints(LineRenderer lr, float r)
    {
        int count = lr.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * 2f * Mathf.PI;
            float x = center.x + Mathf.Cos(angle) * r;
            float z = center.z + Mathf.Sin(angle) * r;
            float y = center.y;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(x, center.y + 10f, z), Vector3.down, out hit, 20f))
                y = hit.point.y + 0.1f;
            lr.SetPosition(i, new Vector3(x, y, z));
        }
    }

    void Update()
    {
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) { Destroy(gameObject); return; }

        float progress   = 1f - (timeRemaining / totalTime);
        float pulseSpeed = Mathf.Lerp(2f, 12f, progress);
        float pulse      = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
        float alpha      = Mathf.Lerp(0.5f, 1f, pulse);

        Color c = Color.Lerp(
            new Color(1f, 0.85f, 0f, alpha),
            new Color(1f, 0.05f, 0f, alpha),
            progress
        );

        if (ring != null) { ring.startColor = c; ring.endColor = c; SetPoints(ring, radius); }

        float innerR = radius * Mathf.Lerp(0.1f, 0.8f, progress);
        Color ci = new Color(1f, 0.2f, 0f, alpha * 0.5f);
        if (innerRing != null) { innerRing.startColor = ci; innerRing.endColor = ci; SetPoints(innerRing, innerR); }
    }

    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(center + Vector3.up * 0.5f);
        if (sp.z < 0) return;

        float cx = sp.x;
        float cy = Screen.height - sp.y;
        float progress = 1f - (timeRemaining / totalTime);

        GUIStyle s = new GUIStyle();
        s.fontSize  = Mathf.RoundToInt(Mathf.Lerp(13f, 18f, progress));
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;
        s.normal.textColor = new Color(1f, Mathf.Lerp(0.9f, 0.1f, progress), 0f, 1f);
        GUI.Label(new Rect(cx - 60, cy - 24, 120, 20), $"🎯  AIRSTRIKE  {timeRemaining:0.0}s", s);
    }
}