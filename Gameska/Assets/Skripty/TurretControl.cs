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

    public static Vector3 AimPoint;
    public static Vector3 DesiredAimPoint;
    public static float TurretYaw;

    private float nextFireTime = 0f;
    private float currentGunAngle = 0f;
    private float targetTurretYaw = 0f;

    private ReloadBar reloadBar;
    private TankCrosshair tankCrosshair;

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

        reloadBar = FindObjectOfType<ReloadBar>();
        tankCrosshair = FindObjectOfType<TankCrosshair>();
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
        float currentYaw = turret.eulerAngles.y;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, targetTurretYaw, turretRotationSpeed * Time.deltaTime);
        turret.rotation = Quaternion.Euler(0f, newYaw, 0f);
        TurretYaw = newYaw;
    }

    void ElevateGunTowardsMouse()
    {
        if (gun == null || turret == null) return;
        Vector3 toTarget = DesiredAimPoint - gun.position;
        float horizontalDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float verticalDiff = toTarget.y;
        float desiredAngle = Mathf.Atan2(verticalDiff, horizontalDist) * Mathf.Rad2Deg;
        desiredAngle = Mathf.Clamp(desiredAngle, minGunAngle, maxGunAngle);
        currentGunAngle = Mathf.MoveTowards(currentGunAngle, desiredAngle, gunElevationSpeed * Time.deltaTime);
        gun.localRotation = Quaternion.Euler(-currentGunAngle, 0f, 0f);
    }

    void UpdateActualAimPoint()
    {
        if (firePoint == null || gun == null) return;
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
        Vector3 baseDir = (sniper != null && sniper.IsSniping)
            ? sniper.AimDirection
            : gun.forward;

        AmmoManager ammo = AmmoManager.Instance;
        if (ammo != null)
        {
            ammo.UseAmmo();
            var shots = ammo.GetShots(firePoint.position, baseDir);
            foreach (var shot in shots)
                SpawnShell(shot.pos, shot.dir, shot.type);
        }
        else
        {
            SpawnShell(firePoint.position, baseDir, AmmoType.Standard);
        }

        if (reloadBar != null) reloadBar.OnFired();
        if (tankCrosshair != null) tankCrosshair.OnShot();
        if (muzzleFlash != null) muzzleFlash.Play();
        if (audioSource != null && shootSound != null) audioSource.PlayOneShot(shootSound);
        
        // Zatrepac kamerou pri vystrele (iba pre hraca)
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(0.1f, 0.15f);
    }

    void SpawnShell(Vector3 pos, Vector3 dir, AmmoType ammoType)
    {
        Quaternion shellRot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
        GameObject shell = Instantiate(shellPrefab, pos, shellRot);

        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = dir * shellSpeed;

        TankShell ts = shell.GetComponent<TankShell>();
        if (ts != null)
        {
            ts.damage = shellDamage;
            ts.ammoType = ammoType;
        }

        Destroy(shell, 5f);
    }
}