using UnityEngine;

public class TankMovement : MonoBehaviour
{
    [Header("Pohyb")]
    public float speed     = 10f;
    public float turnSpeed = 50f;

    [Header("Stabilizácia")]
    public float angularDamping  = 10f;   // tlmí rotačné vibrácie
    public float uprightStrength = 8f;    // drží tank rovno (bez naklápania)

    private Rigidbody rb;

    [Header("Zvuk motora")]
    public AudioSource engineAudio;
    public float idlePitch  = 0.6f;   // pitch v pokoji
    public float activePitch = 1.4f;  // pitch pri plnej rýchlosti
    public float pitchSmooth = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (engineAudio == null)
            engineAudio = GetComponent<AudioSource>();
        if (engineAudio != null && !engineAudio.isPlaying)
            engineAudio.Play();
        rb.angularDamping  = angularDamping;
        // Zmrazi rotáciu na X a Z — tank sa nebude naklápať do strán ani dopredu
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        // Pohyb dopredu/dozadu
        Vector3 move = transform.forward * moveInput * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        // Otáčanie len na Y osi
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float turn = turnInput * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }

        // Zvuk motora — pitch aj volume podľa pohybu
        if (engineAudio != null)
        {
            if (!engineAudio.isPlaying) engineAudio.Play();

            float input = Mathf.Clamp01(Mathf.Abs(moveInput) + Mathf.Abs(turnInput));
            float targetPitch  = Mathf.Lerp(idlePitch, activePitch, input);
            float targetVolume = Mathf.Lerp(0.35f, 1f, input);

            engineAudio.pitch  = Mathf.Lerp(engineAudio.pitch,  targetPitch,  pitchSmooth * Time.fixedDeltaTime);
            engineAudio.volume = Mathf.Lerp(engineAudio.volume, targetVolume, pitchSmooth * Time.fixedDeltaTime);
        }

        // Vynúť upright — potlač akékoľvek naklápanie
        Vector3 currentUp  = transform.up;
        Vector3 correction = Vector3.Cross(currentUp, Vector3.up);
        rb.AddTorque(correction * uprightStrength, ForceMode.VelocityChange);
    }
}