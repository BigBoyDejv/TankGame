using UnityEngine;

public class TankExhaust : MonoBehaviour
{
    [Header("Referencia")]
    public ParticleSystem exhaustParticles;

    [Header("Množstvo dymu (Emission)")]
    public float idleEmission = 10f;   // Keď stojí
    public float maxEmission = 60f;    // Keď ide naplno (zvýšené pre hustotu)

    [Header("Veľkosť obláčikov (Size)")]
    public float idleSize = 0.4f;      // Malý dym na voľnobehu
    public float maxSize = 1.2f;       // Veľké kúdoly pri pohybe

    [Header("Nastavenia pohybu")]
    public float maxSpeedForEmission = 8f;
    public float lerpSpeed = 3f;       // Pomalší plynulý nábeh

    private Vector3 lastPosition;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.MainModule mainModule;
    private float currentSpeed;

    void Start()
    {
        if (exhaustParticles == null) exhaustParticles = GetComponent<ParticleSystem>();
        
        if (exhaustParticles != null)
        {
            emissionModule = exhaustParticles.emission;
            mainModule = exhaustParticles.main;
        }
        lastPosition = transform.position;
    }

    void Update()
    {
        if (exhaustParticles == null) return;

        // Výpočet rýchlosti
        float dist = Vector3.Distance(transform.position, lastPosition);
        currentSpeed = Mathf.Lerp(currentSpeed, dist / Time.deltaTime, Time.deltaTime * lerpSpeed);

        float t = Mathf.Clamp01(currentSpeed / maxSpeedForEmission);

        // Hustota (Množstvo)
        emissionModule.rateOverTime = Mathf.Lerp(idleEmission, maxEmission, t);

        // Veľkosť obláčikov
        mainModule.startSizeMultiplier = Mathf.Lerp(idleSize, maxSize, t);

        lastPosition = transform.position;
    }
}
