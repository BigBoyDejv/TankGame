using UnityEngine;

public class TurretControl : MonoBehaviour
{
    [Header("Referencie")]
    public Transform turret;
    public Transform gun;
    public Transform firePoint;
    public Camera mainCamera;
    public CameraFollow cameraFollow;

    [Header("Veža")]
    public float turretRotationSpeed = 100f;

    [Header("Hlaveň")]
    public float minGunAngle = -5f;
    public float maxGunAngle = 20f;
    public float gunElevationSpeed = 40f;

    [Header("Strieľanie")]
    public GameObject shellPrefab;
    public float shellSpeed = 80f;
    public float shellDamage = 40f;
    public float fireRate = 1f;

    [Header("Efekty")]
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootSound;

    // Zdieľané statické hodnoty pre crosshair a kameru
    public static Vector3 AimPoint;          // kde guľka dopadne (z firePoint)
    public static Vector3 DesiredAimPoint;   // kde hráč chce mieriť (z myši)
    public static float TurretYaw;           // aktuálna horizontálna rotácia veže

    private float nextFireTime = 0f;
    private float currentGunAngle = 0f;
    private float targetGunAngle = 0f;
    private float targetTurretYaw = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (cameraFollow == null && mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

        if (turret != null)
        {
            TurretYaw = turret.eulerAngles.y;
            targetTurretYaw = TurretYaw;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void Update()
    {
        SniperMode sniper = mainCamera != null ? mainCamera.GetComponent<SniperMode>() : null;
        bool isSniping = sniper != null && sniper.IsSniping;

        if (!isSniping)
        {
            UpdateDesiredAimPoint();
            RotateTurretTowardsMouse();
            ElevateGunTowardsMouse();
        }

        UpdateActualAimPoint();

        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void UpdateDesiredAimPoint()
    {
        if (mainCamera == null) return;

        // Raycast z myši na zem — kde hráč chce mieriť
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
            DesiredAimPoint = hit.point;
        else
            DesiredAimPoint = ray.GetPoint(500f);
    }

    void RotateTurretTowardsMouse()
    {
        if (turret == null) return;

        Vector3 dir = DesiredAimPoint - turret.position;
        dir.y = 0f;

        if (dir.magnitude < 0.1f) return;

        float desiredYaw = Quaternion.LookRotation(dir).eulerAngles.y;
        targetTurretYaw = desiredYaw;

        // Plynulé otáčanie veže
        float currentYaw = turret.eulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetTurretYaw, turretRotationSpeed * Time.deltaTime);
        turret.rotation = Quaternion.Euler(0f, newYaw, 0f);
        TurretYaw = newYaw;
    }

    void ElevateGunTowardsMouse()
    {
        if (gun == null || turret == null) return;

        // Použi pozíciu veže (nie firePoint) pre výpočet uhla
        Vector3 toTarget = DesiredAimPoint - gun.position;
        float horizontalDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float verticalDiff = toTarget.y;

        // Uhol k cieľu
        float desiredAngle = Mathf.Atan2(verticalDiff, horizontalDist) * Mathf.Rad2Deg;
        desiredAngle = Mathf.Clamp(desiredAngle, minGunAngle, maxGunAngle);

        targetGunAngle = desiredAngle;
        currentGunAngle = Mathf.MoveTowards(currentGunAngle, targetGunAngle, gunElevationSpeed * Time.deltaTime);
        // Záporné = hlaveň hore (Unity local rotation je invertovaná na X osi)
        gun.localRotation = Quaternion.Euler(-currentGunAngle, 0f, 0f);
    }

    void UpdateActualAimPoint()
    {
        if (firePoint == null || gun == null) return;

        // Smer = vždy gun.forward (world forward hlavne), nie firePoint.forward
        Vector3 aimDir = gun.forward;
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, aimDir, out hit, 1000f))
            AimPoint = hit.point;
        else
            AimPoint = firePoint.position + aimDir * 500f;
    }

    void Shoot()
    {
        if (shellPrefab == null || firePoint == null) return;

        SniperMode sniper = mainCamera != null ? mainCamera.GetComponent<SniperMode>() : null;
        // V sniper mode použi AimDirection = world forward hlavne (nie kamery!)
        // gun.forward = správny world smer hlavne bez ohľadu na orientáciu firePoint prefabu
        Vector3 shootDir = (sniper != null && sniper.IsSniping)
            ? sniper.AimDirection
            : gun.forward;

        GameObject shell = Instantiate(shellPrefab, firePoint.position, Quaternion.LookRotation(shootDir));
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = shootDir * shellSpeed;

        TankShell ts = shell.GetComponent<TankShell>();
        if (ts != null) ts.damage = shellDamage;

        ReloadBar rb2 = FindObjectOfType<ReloadBar>();
        if (rb2 != null) rb2.OnFired();

        TankCrosshair crosshair = FindObjectOfType<TankCrosshair>();
        if (crosshair != null) crosshair.OnShot();

        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);

        Destroy(shell, 5f);
    }
}