using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ArtilleryTank : MonoBehaviour
{
    [Header("Referencie modelu")]
    public Transform turret;        // _Turret child
    public Transform gun;           // _Gun child

    [Header("Otáčanie")]
    public float turretRotSpeed = 40f;
    public float gunElevSpeed   = 30f;
    public float minGunAngle    = 20f;   // min elevacia (blizko)
    public float maxGunAngle    = 70f;   // max elevacia (daleko)

    [Header("Strieľanie")]
    public float fireRate        = 8f;
    public float warningDuration = 2.5f;
    public float shellDamage     = 80f;
    public float explosionRadius = 8f;

    [Header("Dosah")]
    public float minRange = 30f;
    public float maxRange = 120f;

    [Header("Prefaby")]
    public GameObject explosionEffect;
    public GameObject shellPrefab;

    private Transform player;
    private float nextFireTime = 0f;
    private float currentGunAngle = 45f;

    // Ground indicators — LineRenderer kruhy
    private static List<GroundWarning> activeWarnings = new List<GroundWarning>();

    private class GroundWarning
    {
        public Vector3    center;
        public float      radius;
        public float      timeRemaining;
        public float      totalTime;
        public GameObject ringObj;
        public GameObject fillObj;
        public LineRenderer ring;
        public LineRenderer fill;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Auto-nájdi turret a gun ak nie sú nastavené
        if (turret == null || gun == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name.EndsWith("_Turret")) turret = child;
                if (child.name.EndsWith("_Gun"))    gun    = child;
            }
        }

        nextFireTime = Time.time + Random.Range(2f, fireRate);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > maxRange) return;

        AimAtPlayer(dist);

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            StartCoroutine(FireSequence());
        }

        UpdateWarnings();
    }

    // ── Mierenie ─────────────────────────────────────────────────
    void AimAtPlayer(float dist)
    {
        // Veža sa otáča horizontálne k hráčovi
        if (turret != null)
        {
            Vector3 dir = (player.position - turret.position);
            dir.y = 0f;
            if (dir.magnitude > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                turret.rotation = Quaternion.RotateTowards(
                    turret.rotation, target, turretRotSpeed * Time.deltaTime
                );
            }
        }

        // Delo sa zdvíha podľa vzdialenosti — ďalej = vyššie
        if (gun != null)
        {
            float t = Mathf.InverseLerp(minRange, maxRange, dist);
            float targetAngle = Mathf.Lerp(minGunAngle, maxGunAngle, t);
            currentGunAngle = Mathf.MoveTowards(
                currentGunAngle, targetAngle, gunElevSpeed * Time.deltaTime
            );
            gun.localRotation = Quaternion.Euler(-currentGunAngle, 0f, 0f);
        }
    }

    // ── Streľba ───────────────────────────────────────────────────
    IEnumerator FireSequence()
    {
        Vector3 targetPos = player.position + new Vector3(
            Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)
        );

        // Vytvor ground warning
        CreateGroundWarning(targetPos, explosionRadius, warningDuration);

        // Vizuálny projektil po parabole
        if (shellPrefab != null)
            StartCoroutine(AnimateShell(targetPos));

        yield return new WaitForSeconds(warningDuration);

        Explode(targetPos);
    }

    IEnumerator AnimateShell(Vector3 target)
    {
        Vector3 startPos = gun != null
            ? gun.position + gun.forward * 1f
            : transform.position + Vector3.up * 3f;

        GameObject shell = Instantiate(shellPrefab, startPos, Quaternion.identity);
        float elapsed  = 0f;
        float duration = warningDuration;
        float arcHeight = Vector3.Distance(startPos, target) * 0.6f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 pos = Vector3.Lerp(startPos, target, t);
            pos.y += arcHeight * Mathf.Sin(t * Mathf.PI);
            shell.transform.position = pos;

            // Otočiť smerom pohybu
            float tPrev = Mathf.Max(0f, t - 0.01f);
            Vector3 prev = Vector3.Lerp(startPos, target, tPrev);
            prev.y += arcHeight * Mathf.Sin(tPrev * Mathf.PI);
            Vector3 dir = pos - prev;
            if (dir.magnitude > 0.001f)
                shell.transform.rotation = Quaternion.LookRotation(dir);

            yield return null;
        }
        Destroy(shell);
    }

    void Explode(Vector3 center)
    {
        // Odober warning kruh
        foreach (var w in activeWarnings)
        {
            if (Vector3.Distance(w.center, center) < 1f)
            {
                if (w.ringObj != null) Destroy(w.ringObj);
                if (w.fillObj != null) Destroy(w.fillObj);
            }
        }
        activeWarnings.RemoveAll(w => Vector3.Distance(w.center, center) < 1f);

        if (explosionEffect != null)
            Instantiate(explosionEffect, center + Vector3.up * 0.5f, Quaternion.identity);

        Collider[] cols = Physics.OverlapSphere(center, explosionRadius);
        foreach (Collider col in cols)
        {
            TankHealth health = col.GetComponentInParent<TankHealth>();
            if (health == null) continue;
            float dist    = Vector3.Distance(col.transform.position, center);
            float falloff = 1f - Mathf.Clamp01(dist / explosionRadius);
            health.TakeDamage(shellDamage * falloff);
            if (health.isPlayer)
            {
                DamageIndicator ind = FindObjectOfType<DamageIndicator>();
                if (ind != null) ind.ShowDamage(center);
            }
        }
    }

    // ── Ground Warning — LineRenderer kruhy priamo na zemi ────────
    // Statická metóda — môže ju volať SkillSystem pre Airstrike
    public static void SpawnGroundWarning(Vector3 center, float radius, float duration)
    {
        // Vytvor dočasný objekt ktorý bude kresliť warning
        GameObject go = new GameObject("AirstrikeWarning");
        AirstrikeWarningHelper helper = go.AddComponent<AirstrikeWarningHelper>();
        helper.Init(center, radius, duration);
    }

    void CreateGroundWarning(Vector3 center, float radius, float duration)
    {
        var w = new GroundWarning
        {
            center        = center,
            radius        = radius,
            timeRemaining = duration,
            totalTime     = duration
        };

        // Vonkajší kruh
        w.ringObj = new GameObject("ArtilleryRing");
        w.ring    = w.ringObj.AddComponent<LineRenderer>();
        SetupLineRenderer(w.ring, radius, 0.25f, true);

        // Výplň — viacero menších kruhov
        w.fillObj = new GameObject("ArtilleryFill");
        w.fill    = w.fillObj.AddComponent<LineRenderer>();
        SetupLineRenderer(w.fill, radius * 0.15f, 0.15f, false);

        activeWarnings.Add(w);
    }

    void SetupLineRenderer(LineRenderer lr, float radius, float width, bool isRing)
    {
        lr.useWorldSpace    = true;
        lr.loop             = true;
        lr.startWidth       = width;
        lr.endWidth         = width;
        lr.positionCount    = 64;
        lr.material         = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder     = 10;

        // Nastav body kruhu
        SetCirclePoints(lr, lr.transform.position, radius);
    }

    void SetCirclePoints(LineRenderer lr, Vector3 center, float radius)
    {
        int count = lr.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * 2f * Mathf.PI;
            float x = center.x + Mathf.Cos(angle) * radius;
            float z = center.z + Mathf.Sin(angle) * radius;

            // Raycast na zem
            float y = center.y;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(x, center.y + 10f, z), Vector3.down, out hit, 20f))
                y = hit.point.y + 0.1f;

            lr.SetPosition(i, new Vector3(x, y, z));
        }
    }

    void UpdateWarnings()
    {
        for (int i = activeWarnings.Count - 1; i >= 0; i--)
        {
            var w = activeWarnings[i];
            w.timeRemaining -= Time.deltaTime;

            if (w.timeRemaining <= 0f)
            {
                if (w.ringObj != null) Destroy(w.ringObj);
                if (w.fillObj != null) Destroy(w.fillObj);
                activeWarnings.RemoveAt(i);
                continue;
            }

            float progress = 1f - (w.timeRemaining / w.totalTime);

            // Pulzujúca farba — žltá → červená, rýchlejšie pulzovanie
            float pulseSpeed = Mathf.Lerp(2f, 10f, progress);
            float pulse      = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
            float alpha      = Mathf.Lerp(0.5f, 1f, pulse);

            Color ringColor = Color.Lerp(
                new Color(1f, 0.85f, 0f, alpha),
                new Color(1f, 0.05f, 0f, alpha),
                progress
            );
            Color fillColor = new Color(1f, 0.1f, 0f, alpha * 0.3f);

            if (w.ring != null)
            {
                w.ring.startColor = ringColor;
                w.ring.endColor   = ringColor;
                // Zväčšuj šírku čiary čím sa blíži
                float lw = Mathf.Lerp(0.2f, 0.5f, progress);
                w.ring.startWidth = lw;
                w.ring.endWidth   = lw;

                // Aktualizuj pozíciu kruhu každý frame
                SetCirclePoints(w.ring, w.center, w.radius);
            }

            if (w.fill != null)
            {
                w.fill.startColor = fillColor;
                w.fill.endColor   = fillColor;
                // Vnútorný kruh rastie smerom von
                float innerR = w.radius * Mathf.Lerp(0.1f, 0.7f, progress);
                SetCirclePoints(w.fill, w.center, innerR);
            }
        }
    }

    // ── OnGUI — len text odpočet ───────────────────────────────────
    void OnGUI()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        foreach (var w in activeWarnings)
        {
            Vector3 sp = cam.WorldToScreenPoint(w.center + Vector3.up * 0.2f);
            if (sp.z < 0) continue;

            float cx = sp.x;
            float cy = Screen.height - sp.y;

            float progress = 1f - (w.timeRemaining / w.totalTime);
            float pulseSpeed = Mathf.Lerp(2f, 10f, progress);
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));

            GUIStyle style = new GUIStyle();
            style.fontSize  = Mathf.RoundToInt(Mathf.Lerp(13f, 18f, progress));
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = new Color(1f, Mathf.Lerp(0.9f, 0.1f, progress),
                                               0f, Mathf.Lerp(0.7f, 1f, alpha));

            GUI.Label(new Rect(cx - 50, cy - 24, 100, 20),
                      $"⚠  {w.timeRemaining:0.0}s", style);

            GUIStyle sub = new GUIStyle();
            sub.fontSize  = 10;
            sub.fontStyle = FontStyle.Bold;
            sub.alignment = TextAnchor.MiddleCenter;
            sub.normal.textColor = new Color(1f, 1f, 0f, alpha * 0.9f);
            GUI.Label(new Rect(cx - 50, cy - 8, 100, 14), "INCOMING!", sub);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minRange);
    }
}