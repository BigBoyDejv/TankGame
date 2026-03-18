using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Cieľ")]
    public Transform tankBody;

    [Header("Vzdialenosť")]
    public float distance = 12f;
    public float height   = 4f;

    [Header("Plynulosť")]
    public float positionSmooth = 6f;    // nižšie = pomalšie/stabilnejšie
    public float rotationSmooth = 6f;
    public float yawSmooth      = 4f;

    [Header("Vertikálny uhol")]
    public float minPitch = 10f;
    public float maxPitch = 45f;

    [Header("Stabilizácia")]
    public float heightSmooth   = 4f;    // výška sa mení pomalšie — menej poskokov
    public float lookAheadSmooth = 3f;   // look target sa hýbe pomalšie

    [HideInInspector] public float verticalAngle = 20f;

    private float   currentYaw      = 0f;
    private float   smoothedHeight  = 0f;
    private Vector3 smoothVelocity  = Vector3.zero;
    private Vector3 smoothLookTarget;

    void Start()
    {
        if (tankBody == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) tankBody = p.transform;
        }

        if (tankBody != null)
        {
            currentYaw     = tankBody.eulerAngles.y;
            smoothedHeight = tankBody.position.y;
            smoothLookTarget = tankBody.position + Vector3.up * 1.5f;
        }
    }

    void LateUpdate()
    {
        if (tankBody == null) return;

        SniperMode sniper = GetComponent<SniperMode>();
        if (sniper != null && sniper.IsSniping) return;

        bool isMoving  = Mathf.Abs(Input.GetAxis("Vertical"))   > 0.1f;
        bool isTurning = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;

        // Yaw
        if (isMoving || isTurning)
        {
            float targetYaw = tankBody.eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, yawSmooth * Time.deltaTime);
        }
        else
        {
            float targetYaw = TurretControl.TurretYaw;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, (yawSmooth * 0.4f) * Time.deltaTime);
        }

        // Vertikálny uhol podľa myši
        float mouseYNorm = Input.mousePosition.y / Screen.height;
        float targetPitch = Mathf.Lerp(maxPitch, minPitch, mouseYNorm);
        verticalAngle = Mathf.Lerp(verticalAngle, targetPitch, 4f * Time.deltaTime);
        verticalAngle = Mathf.Clamp(verticalAngle, minPitch, maxPitch);

        // KĽÚČ: výška tanku sa smoothuje samostatne — eliminuje poskoky
        smoothedHeight = Mathf.Lerp(smoothedHeight, tankBody.position.y, heightSmooth * Time.deltaTime);
        Vector3 smoothedTankPos = new Vector3(tankBody.position.x, smoothedHeight, tankBody.position.z);

        // Pozícia kamery
        Quaternion rotation  = Quaternion.Euler(verticalAngle, currentYaw, 0f);
        Vector3   desiredPos = smoothedTankPos + rotation * new Vector3(0f, 0f, -distance) + Vector3.up * height;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos, ref smoothVelocity, 1f / positionSmooth
        );

        // Look target sa tiež smoothuje — žiadne trhnutia
        Vector3 desiredLook = smoothedTankPos + Vector3.up * 1.5f;
        smoothLookTarget = Vector3.Lerp(smoothLookTarget, desiredLook, lookAheadSmooth * Time.deltaTime);

        Quaternion desiredRot = Quaternion.LookRotation(smoothLookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmooth * Time.deltaTime);
    }
}