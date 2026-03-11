using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Cieľ")]
    public Transform target;

    [Header("Vzdialenosť")]
    public float distance = 12f;
    public float smoothSpeed = 6f;

    [Header("Vertikálny uhol")]
    public float minVerticalAngle = 5f;
    public float maxVerticalAngle = 60f;
    [HideInInspector] public float verticalAngle = 25f;

    [Header("Stabilizácia")]
    public float positionDamping = 8f;
    public float rotationDamping = 8f;

    private float currentYaw = 0f;
    private Vector3 smoothVelocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }

        if (target != null)
            currentYaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Horizontálna rotácia sleduje vežu
        Vector3 aimDir = TurretControl.AimPoint - target.position;
        aimDir.y = 0f;

        if (aimDir.magnitude > 0.5f)
        {
            float targetYaw = Quaternion.LookRotation(aimDir).eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, smoothSpeed * Time.deltaTime);
        }

        // Clamp vertikálny uhol
        verticalAngle = Mathf.Clamp(verticalAngle, minVerticalAngle, maxVerticalAngle);

        // Pozícia kamery
        Quaternion rotation = Quaternion.Euler(verticalAngle, currentYaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPosition,
            ref smoothVelocity, 1f / positionDamping
        );

        // Pozerá na tank
        Vector3 lookTarget = target.position + Vector3.up * 1.5f;
        Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationDamping * Time.deltaTime);
    }
}