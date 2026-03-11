using UnityEngine;

public class SniperMode : MonoBehaviour
{
    [Header("Referencie")]
    public Camera mainCamera;
    public CameraFollow cameraFollow;
    public Transform gunTransform;
    public Transform turretTransform;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sniperFOV = 15f;
    public float zoomSpeed = 10f;

    [Header("Sniper pozícia (offset od gun)")]
    public Vector3 sniperOffset = new Vector3(0f, 0.1f, 0.5f);

    [Header("Citlivosť myši")]
    public float mouseSensitivity = 1.5f;
    public float minPitch = -5f;
    public float maxPitch = 20f;

    public bool IsSniping { get; private set; } = false;
    public Vector3 AimDirection { get; private set; } = Vector3.forward;

    private float targetFOV;
    private Texture2D tex;
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (cameraFollow == null && mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow>();

        targetFOV = normalFOV;
        tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            EnterSniperMode();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ExitSniperMode();
        }

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, targetFOV, zoomSpeed * Time.deltaTime);

        if (IsSniping)
        {
            HandleSniperInput();
            ApplySniperCamera();
        }
    }

    void EnterSniperMode()
    {
        IsSniping = true;
        targetFOV = sniperFOV;
        if (cameraFollow != null) cameraFollow.enabled = false;

        // Načítaj aktuálny yaw z veže
        if (turretTransform != null)
            yaw = turretTransform.eulerAngles.y;

        // Načítaj aktuálny pitch z hlavne (local X)
        if (gunTransform != null)
        {
            float rawX = gunTransform.localEulerAngles.x;
            pitch = rawX > 180f ? rawX - 360f : rawX;
            // Invertuj lebo Unity má -X = hore na lokálnej osi
            pitch = -pitch;
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void ExitSniperMode()
    {
        IsSniping = false;
        targetFOV = normalFOV;
        if (cameraFollow != null) cameraFollow.enabled = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void HandleSniperInput()
    {
        // Iba myš ovláda kameru — WASD pohybuje tankom ale NEotáča kameru
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += mouseY; // mouseY hore = pitch hore = hlaveň hore
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Otočiť vežu podľa yaw (world space)
        if (turretTransform != null)
            turretTransform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Otočiť hlaveň podľa pitch (local space, -pitch lebo Unity X os)
        if (gunTransform != null)
            gunTransform.localRotation = Quaternion.Euler(-pitch, 0f, 0f);

        // AimDirection = presne world forward hlavne
        AimDirection = gunTransform != null ? gunTransform.forward : mainCamera.transform.forward;
    }

    void ApplySniperCamera()
    {
        if (gunTransform == null) return;

        // Kamera sedí za hlavňou
        mainCamera.transform.position = gunTransform.TransformPoint(sniperOffset);

        // Kamera pozerá presne smerom ako hlaveň — yaw (world) + pitch (local cez vežu)
        // Správny spôsob: veža dáva yaw, hlaveň dáva pitch → kamera kopíruje finálnu rotáciu hlavne
        mainCamera.transform.rotation = Quaternion.Euler(
            -pitch,  // pitch kamery = opak local pitch hlavne
            yaw,     // yaw = rovnaký ako veža
            0f
        );
    }

    void OnGUI()
    {
        if (!IsSniping) return;

        float screenW = Screen.width;
        float screenH = Screen.height;
        float cx = screenW / 2f;
        float cy = screenH / 2f;
        float maskSize = screenH * 0.35f;

        // Stmavenie okrajov
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(new Rect(0, 0, screenW, cy - maskSize), tex);
        GUI.DrawTexture(new Rect(0, cy + maskSize, screenW, screenH), tex);
        GUI.DrawTexture(new Rect(0, cy - maskSize, cx - maskSize, maskSize * 2), tex);
        GUI.DrawTexture(new Rect(cx + maskSize, cy - maskSize, screenW, maskSize * 2), tex);

        // Kruh
        GUI.color = new Color(0f, 0.9f, 0f, 0.9f);
        DrawCircle(cx, cy, maskSize, 2f);

        // Tenké čiary cez celú obrazovku
        GUI.color = new Color(0f, 1f, 0f, 0.2f);
        GUI.DrawTexture(new Rect(0, cy - 1, screenW, 2), tex);
        GUI.DrawTexture(new Rect(cx - 1, 0, 2, screenH), tex);

        // Crosshair v strede
        GUI.color = Color.green;
        float s = 14f; float g = 5f; float t = 2f;
        GUI.DrawTexture(new Rect(cx - s - g, cy - t / 2f, s, t), tex);
        GUI.DrawTexture(new Rect(cx + g,     cy - t / 2f, s, t), tex);
        GUI.DrawTexture(new Rect(cx - t / 2f, cy - s - g, t, s), tex);
        GUI.DrawTexture(new Rect(cx - t / 2f, cy + g,     t, s), tex);

        GUI.color = Color.white;
    }

    void DrawCircle(float cx, float cy, float radius, float thickness)
    {
        int segments = 64;
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = cx + Mathf.Cos(angle) * radius - thickness / 2f;
            float y = cy + Mathf.Sin(angle) * radius - thickness / 2f;
            GUI.DrawTexture(new Rect(x, y, thickness, thickness), tex);
        }
    }
}