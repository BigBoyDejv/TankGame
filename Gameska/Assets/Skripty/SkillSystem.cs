using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SkillSystem : MonoBehaviour
{
    [Header("Smoke Screen")]
    public KeyCode smokeKey       = KeyCode.Q;
    public float   smokeCooldown  = 15f;
    public float   smokeDuration  = 6f;
    public float   smokeRadius    = 12f;
    public GameObject smokeEffectPrefab;

    [Header("Landmine")]
    public KeyCode mineKey        = KeyCode.E;
    public float   mineCooldown   = 10f;
    public int     maxMines       = 3;
    public float   mineDamage     = 120f;
    public float   mineRadius     = 5f;
    public GameObject mineVisualPrefab;
    public GameObject mineExplosionPrefab;

    [Header("Airstrike")]
    public KeyCode airstrikeKey       = KeyCode.F;
    public float   airstrikeCooldown  = 30f;
    public float   airstrikeDelay     = 3f;    // čas od výberu po dopad
    public float   airstrikeDamage    = 150f;
    public float   airstrikeRadius    = 12f;
    public float   airstrikeZoomHeight = 60f;  // výška kamery pri výbere
    public GameObject airstrikeExplosionPrefab;
    public GameObject airstrikeShellPrefab;

    // Cooldown timery
    private float smokeCDLeft     = 0f;
    private float mineCDLeft      = 0f;
    private float airstrikeCDLeft = 0f;

    // Stav
    private bool isSelectingAirstrike = false;
    private List<GameObject> placedMines = new List<GameObject>();
    private List<GameObject> smokeObjects = new List<GameObject>();

    // Referencie
    private TankMovement tankMovement;
    private TankHealth   tankHealth;
    private CameraFollow cameraFollow;
    private Camera       mainCamera;

    // Airstrike kamera
    private Vector3 savedCamPos;
    private Quaternion savedCamRot;
    private float savedCamFOV;
    private Vector3 airstrikeTarget;

    // GUI
    private Texture2D tex;

    void Start()
    {
        tankMovement  = GetComponent<TankMovement>();
        tankHealth    = GetComponent<TankHealth>();
        mainCamera    = Camera.main;
        if (mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    void Update()
    {
        smokeCDLeft     = Mathf.Max(0, smokeCDLeft     - Time.deltaTime);
        mineCDLeft      = Mathf.Max(0, mineCDLeft      - Time.deltaTime);
        airstrikeCDLeft = Mathf.Max(0, airstrikeCDLeft - Time.deltaTime);

        if (!isSelectingAirstrike)
        {
            if (Input.GetKeyDown(smokeKey)     && smokeCDLeft     <= 0f) ActivateSmoke();
            if (Input.GetKeyDown(mineKey)      && mineCDLeft      <= 0f) PlaceMine();
            if (Input.GetKeyDown(airstrikeKey) && airstrikeCDLeft <= 0f) StartAirstrikeSelect();
        }
        else
        {
            HandleAirstrikeSelection();
        }
    }

    // ══════════════════════════════════════════
    // SMOKE SCREEN
    // ══════════════════════════════════════════
    void ActivateSmoke()
    {
        smokeCDLeft = smokeCooldown;
        StartCoroutine(SmokeCoroutine());
    }

    IEnumerator SmokeCoroutine()
    {
        // Spawn smoke efekt
        GameObject smoke = null;
        if (smokeEffectPrefab != null)
        {
            smoke = Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
            smokeObjects.Add(smoke);
        }

        // Oslepi všetkých enemy v dosahu
        List<EnemyTank> blindedEnemies = new List<EnemyTank>();
        Collider[] cols = Physics.OverlapSphere(transform.position, smokeRadius);
        foreach (Collider c in cols)
        {
            EnemyTank et = c.GetComponentInParent<EnemyTank>();
            if (et != null && !blindedEnemies.Contains(et))
            {
                blindedEnemies.Add(et);
                et.SetBlinded(true);
            }
        }

        yield return new WaitForSeconds(smokeDuration);

        // Obnov AI
        foreach (EnemyTank et in blindedEnemies)
            if (et != null) et.SetBlinded(false);

        if (smoke != null)
        {
            smokeObjects.Remove(smoke);
            Destroy(smoke);
        }
    }

    // ══════════════════════════════════════════
    // LANDMINE
    // ══════════════════════════════════════════
    void PlaceMine()
    {
        if (placedMines.Count >= maxMines)
        {
            // Odober najstaršiu mínu
            if (placedMines[0] != null) Destroy(placedMines[0]);
            placedMines.RemoveAt(0);
        }

        mineCDLeft = mineCooldown;

        Vector3 minePos = transform.position - transform.forward * 2f;
        minePos.y = transform.position.y;

        GameObject mine;
        if (mineVisualPrefab != null)
            mine = Instantiate(mineVisualPrefab, minePos, Quaternion.identity);
        else
        {
            // Fallback — jednoduchý plochý valec
            mine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mine.transform.position    = minePos;
            mine.transform.localScale  = new Vector3(1f, 0.1f, 1f);
            mine.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.1f);
        }

        mine.layer = LayerMask.NameToLayer("Default");
        mine.AddComponent<Landmine>().Setup(mineDamage, mineRadius, mineExplosionPrefab, this);
        placedMines.Add(mine);
    }

    public void MineTriggered(GameObject mine)
    {
        placedMines.Remove(mine);
    }

    // ══════════════════════════════════════════
    // AIRSTRIKE
    // ══════════════════════════════════════════
    void StartAirstrikeSelect()
    {
        isSelectingAirstrike = true;

        // Ulož stav kamery
        if (mainCamera != null)
        {
            savedCamPos = mainCamera.transform.position;
            savedCamRot = mainCamera.transform.rotation;
            savedCamFOV = mainCamera.fieldOfView;
        }

        // Vypni CameraFollow — kamera pôjde priamo hore
        if (cameraFollow != null) cameraFollow.enabled = false;

        // Posuň kameru rovno hore
        if (mainCamera != null)
        {
            Vector3 topPos = transform.position + Vector3.up * airstrikeZoomHeight;
            mainCamera.transform.position = topPos;
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mainCamera.fieldOfView = 50f;
        }

        // Zobraz kurzor
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void HandleAirstrikeSelection()
    {
        // Pohyb kamery pri výbere
        if (mainCamera != null)
        {
            float camSpeed = 20f;
            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) move += Vector3.back;
            if (Input.GetKey(KeyCode.A)) move += Vector3.left;
            if (Input.GetKey(KeyCode.D)) move += Vector3.right;
            mainCamera.transform.position += move * camSpeed * Time.deltaTime;
        }

        // Ľavý klik = potvrď lokáciu
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 500f))
                airstrikeTarget = hit.point;
            else
                airstrikeTarget = ray.GetPoint(200f);

            ConfirmAirstrike();
        }

        // Escape = zruš
        if (Input.GetKeyDown(KeyCode.Escape))
            CancelAirstrike();
    }

    void ConfirmAirstrike()
    {
        isSelectingAirstrike = false;
        airstrikeCDLeft = airstrikeCooldown;

        // Obnov kameru
        RestoreCamera();

        StartCoroutine(AirstrikeCoroutine(airstrikeTarget));
    }

    void CancelAirstrike()
    {
        isSelectingAirstrike = false;
        RestoreCamera();
    }

    void RestoreCamera()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = savedCamPos;
            mainCamera.transform.rotation = savedCamRot;
            mainCamera.fieldOfView        = savedCamFOV;
        }
        if (cameraFollow != null) cameraFollow.enabled = true;

        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    IEnumerator AirstrikeCoroutine(Vector3 target)
    {
        // Použi Artillery warning systém
        ArtilleryTank.SpawnGroundWarning(target, airstrikeRadius, airstrikeDelay);

        // Vizuálny projektil zhora
        if (airstrikeShellPrefab != null)
        {
            GameObject shell = Instantiate(airstrikeShellPrefab,
                target + Vector3.up * 80f, Quaternion.Euler(90f, 0f, 0f));
            StartCoroutine(DropShell(shell, target, airstrikeDelay));
        }

        yield return new WaitForSeconds(airstrikeDelay);

        // Explózia
        if (airstrikeExplosionPrefab != null)
            Instantiate(airstrikeExplosionPrefab, target + Vector3.up * 0.5f,
                        Quaternion.identity);

        Collider[] cols = Physics.OverlapSphere(target, airstrikeRadius);
        foreach (Collider c in cols)
        {
            TankHealth th = c.GetComponentInParent<TankHealth>();
            if (th == null || th.isPlayer) continue;
            float dist    = Vector3.Distance(c.transform.position, target);
            float falloff = 1f - Mathf.Clamp01(dist / airstrikeRadius);
            th.TakeDamage(airstrikeDamage * falloff);
        }
    }

    IEnumerator DropShell(GameObject shell, Vector3 target, float duration)
    {
        Vector3 start = shell.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            shell.transform.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        Destroy(shell);
    }

    // ══════════════════════════════════════════
    // GUI — skill bary
    // ══════════════════════════════════════════
    void OnGUI()
    {
        if (tex == null) return;

        float sw = Screen.width;
        float sh = Screen.height;

        // Airstrike selection overlay
        if (isSelectingAirstrike)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.3f);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), tex);

            GUIStyle style = new GUIStyle();
            style.fontSize  = 18;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = new Color(1f, 0.8f, 0f);
            style.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, sh * 0.1f, sw, 30),
                      "🎯  Klikni na cieľ  |  ESC = zruš", style);
            GUI.color = Color.white;
        }

        // Skill ikony — vľavo dole
        float slotW = 64f;
        float slotH = 64f;
        float gap   = 8f;
        float startX = 20f;
        float startY = sh - slotH - 20f;

        DrawSkillSlot(startX,                 startY, slotW, slotH,
            "Q", "Dym",      smokeCooldown,     smokeCDLeft,
            new Color(0.5f, 0.8f, 0.5f));

        DrawSkillSlot(startX + slotW + gap,   startY, slotW, slotH,
            "E", "Míny",     mineCooldown,      mineCDLeft,
            new Color(0.8f, 0.6f, 0.2f));

        DrawSkillSlot(startX + (slotW+gap)*2, startY, slotW, slotH,
            "F", "Airstrike", airstrikeCooldown, airstrikeCDLeft,
            new Color(0.9f, 0.3f, 0.2f));
    }

    void DrawSkillSlot(float x, float y, float w, float h,
                       string key, string name,
                       float totalCD, float cdLeft, Color color)
    {
        bool ready = cdLeft <= 0f;
        float progress = ready ? 1f : 1f - (cdLeft / totalCD);

        // Pozadie
        GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, w, h), tex);

        // Cooldown fill
        if (!ready)
        {
            GUI.color = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.85f);
            GUI.DrawTexture(new Rect(x, y + h * (1f - progress), w, h * progress), tex);
        }
        else
        {
            GUI.color = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 0.85f);
            GUI.DrawTexture(new Rect(x, y, w, h), tex);
        }

        // Border
        GUI.color = ready ? color : new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f);
        GUI.DrawTexture(new Rect(x,         y,         w, 2),   tex);
        GUI.DrawTexture(new Rect(x,         y + h - 2, w, 2),   tex);
        GUI.DrawTexture(new Rect(x,         y,         2, h),   tex);
        GUI.DrawTexture(new Rect(x + w - 2, y,         2, h),   tex);

        GUI.color = Color.white;

        // Kláves
        GUIStyle keyStyle = new GUIStyle();
        keyStyle.fontSize  = 10;
        keyStyle.fontStyle = FontStyle.Bold;
        keyStyle.normal.textColor = new Color(1f, 1f, 0.5f, 0.9f);
        keyStyle.alignment = TextAnchor.UpperLeft;
        GUI.Label(new Rect(x + 4, y + 3, 20, 14), $"[{key}]", keyStyle);

        // Názov
        GUIStyle nameStyle = new GUIStyle();
        nameStyle.fontSize  = 10;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.normal.textColor = ready ? Color.white : new Color(0.6f, 0.6f, 0.6f);
        nameStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(x, y + 22, w, 18), name, nameStyle);

        // Cooldown text
        if (!ready)
        {
            GUIStyle cdStyle = new GUIStyle();
            cdStyle.fontSize  = 13;
            cdStyle.fontStyle = FontStyle.Bold;
            cdStyle.normal.textColor = Color.white;
            cdStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x, y + 38, w, 20), $"{cdLeft:0.0}s", cdStyle);
        }
        else
        {
            GUIStyle rdStyle = new GUIStyle();
            rdStyle.fontSize  = 10;
            rdStyle.fontStyle = FontStyle.Bold;
            rdStyle.normal.textColor = color;
            rdStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(x, y + 38, w, 20), "READY", rdStyle);
        }
    }
}