using UnityEngine;

public class AutoAim : MonoBehaviour
{
    [Header("Nastavenia")]
    public float lockRange = 50f;
    public float lockSpeed = 5f;
    public KeyCode lockKey = KeyCode.Mouse1; // pravý klik

    private Transform lockedTarget = null;
    private TurretControl turretControl;
    private CameraFollow cameraFollow;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Ak sa player zmenil cez XPSystem
        if (turretControl == null && XPSystem.PlayerTransform != null)
            turretControl = XPSystem.PlayerTransform.GetComponentInChildren<TurretControl>();
        if (mainCamera == null)
            mainCamera = Camera.main;
        if (cameraFollow == null && mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

        // Pravý klik — lock/unlock
        if (Input.GetMouseButtonDown(1))
        {
            if (lockedTarget != null)
            {
                // Unlock
                lockedTarget = null;
                if (cameraFollow != null) cameraFollow.enabled = true;
            }
            else
            {
                // Skús locknúť enemy
                TryLockTarget();
            }
        }

        // Ak máme locked target
        if (lockedTarget != null)
        {
            // Skontroluj či target stále existuje a je v dosahu
            float dist = Vector3.Distance(transform.position, lockedTarget.position);
            if (dist > lockRange * 1.5f)
            {
                lockedTarget = null;
                if (cameraFollow != null) cameraFollow.enabled = true;
                return;
            }

            // Otáčaj vežu k targetu
            AimAtTarget();

            // Otáčaj kameru k targetu
            RotateCameraToTarget();
        }
    }

    void TryLockTarget()
    {
        // Raycast zo stredu obrazovky
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, lockRange))
        {
            TankHealth th = hit.collider.GetComponentInParent<TankHealth>();
            if (th != null && !th.isPlayer)
            {
                lockedTarget = th.transform;
                if (cameraFollow != null) cameraFollow.enabled = false;
                return;
            }
        }

        // Ak raycast netrafil, hľadaj najbližší enemy v dosahu
        Collider[] cols = Physics.OverlapSphere(transform.position, lockRange);
        Transform closest = null;
        float closestDist = lockRange;

        foreach (Collider c in cols)
        {
            TankHealth th = c.GetComponentInParent<TankHealth>();
            if (th == null || th.isPlayer) continue;

            float dist = Vector3.Distance(transform.position, th.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = th.transform;
            }
        }

        if (closest != null)
        {
            lockedTarget = closest;
            if (cameraFollow != null) cameraFollow.enabled = false;
        }
    }

    void AimAtTarget()
{
    if (turretControl == null) return;
    if (lockedTarget == null) return;

    // Vezmi presnu poziciu collidera targetu
    Collider targetCollider = lockedTarget.GetComponentInChildren<Collider>();
    Vector3 targetPos = targetCollider != null 
        ? targetCollider.bounds.center 
        : lockedTarget.position + Vector3.up * 0.5f;

    TurretControl.DesiredAimPoint = targetPos;

    // Otoc vezu
    if (turretControl.turret != null)
    {
        Vector3 dir = targetPos - turretControl.turret.position;
        dir.y = 0f;
        if (dir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            turretControl.turret.rotation = Quaternion.RotateTowards(
                turretControl.turret.rotation, targetRot, 200f * Time.deltaTime
            );
        }
    }

    // Otoc hlavne presne na collider
    if (turretControl.gun != null)
    {
        Vector3 toTarget = targetPos - turretControl.gun.position;
        float horizontalDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float verticalDiff = toTarget.y;
        float desiredAngle = Mathf.Atan2(verticalDiff, horizontalDist) * Mathf.Rad2Deg;
        desiredAngle = Mathf.Clamp(desiredAngle, 
            turretControl.minGunAngle, turretControl.maxGunAngle);
        
        turretControl.gun.localRotation = Quaternion.Slerp(
            turretControl.gun.localRotation,
            Quaternion.Euler(-desiredAngle, 0f, 0f),
            10f * Time.deltaTime
        );
    }
}

    void RotateCameraToTarget()
    {
        if (mainCamera == null || lockedTarget == null) return;

        Vector3 desiredPos = lockedTarget.position - 
            (lockedTarget.position - transform.position).normalized * 15f + Vector3.up * 5f;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, desiredPos, lockSpeed * Time.deltaTime
        );

        Quaternion desiredRot = Quaternion.LookRotation(lockedTarget.position - mainCamera.transform.position);
        mainCamera.transform.rotation = Quaternion.Lerp(
            mainCamera.transform.rotation, desiredRot, lockSpeed * Time.deltaTime
        );
    }

    void OnGUI()
    {
        if (lockedTarget == null) return;
        if (mainCamera == null) return;

        // Lock indikátor nad targetom
        Vector3 screenPos = mainCamera.WorldToScreenPoint(lockedTarget.position + Vector3.up * 2f);
        if (screenPos.z < 0) return;

        float x = screenPos.x;
        float y = Screen.height - screenPos.y;
        float size = 30f;

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = new Color(1f, 0.2f, 0.2f, 0.9f);
        style.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(x - size, y - size, size * 2, size * 2), "◎", style);

        // Text LOCKED
        GUIStyle lockStyle = new GUIStyle();
        lockStyle.fontSize = 11;
        lockStyle.fontStyle = FontStyle.Bold;
        lockStyle.normal.textColor = new Color(1f, 0.3f, 0.3f, 1f);
        lockStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(x - 40, y + 16, 80, 16), "LOCKED", lockStyle);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lockRange);
    }
}