using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // Player tag
    public Vector3 offset = new Vector3(0, 15, -10); // Univerzálne pre všetky tanky
    public float smoothSpeed = 0.125f;

    void Start()
    {
        // Automaticky nájde Player
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target);
    }
}
