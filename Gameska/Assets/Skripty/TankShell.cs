using UnityEngine;

// Tento skript NEIDEME dávať na Bullet FX prefab!
// Dáme ho na prázdny "ShellWrapper" GameObject ktorý:
// 1. Pohybuje sa cez vlastný Rigidbody
// 2. Nesie Bullet FX prefab ako čisto vizuálny child (bez jeho Rigidbody)
// 3. Detekuje kolíziu a robí damage

[RequireComponent(typeof(Rigidbody))]
public class TankShell : MonoBehaviour
{
    public float damage = 40f;
    public GameObject explosionEffect;

    [Header("Bullet FX Pack prefab")]
    public GameObject bulletFXPrefab; // Bullet_Fire1 / Fire2 / Fire3 prefab

    private bool hasHit = false;
    private GameObject spawnedFX;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Spawni FX prefab ako child — ale ODOBER mu Rigidbody a Collider
        if (bulletFXPrefab != null)
        {
            spawnedFX = Instantiate(bulletFXPrefab, transform.position, transform.rotation, transform);
            spawnedFX.transform.localPosition = Vector3.zero;
            spawnedFX.transform.localRotation = Quaternion.identity;

            // Odober Rigidbody z FX prefabu
            Rigidbody fxRb = spawnedFX.GetComponent<Rigidbody>();
            if (fxRb != null) Destroy(fxRb);

            // Odober všetky Collidery z FX prefabu
            foreach (Collider col in spawnedFX.GetComponentsInChildren<Collider>())
                Destroy(col);

            // Odober Bullet skript z FX prefabu
            foreach (MonoBehaviour script in spawnedFX.GetComponentsInChildren<MonoBehaviour>())
            {
                if (script.GetType().Name == "Bullet")
                    Destroy(script);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        // Oddeľ FX od wrappera
        if (spawnedFX != null)
        {
            spawnedFX.transform.SetParent(null);
            Destroy(spawnedFX, 2f);
        }

        // Explózia
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Damage
        TankHealth health = collision.gameObject.GetComponentInParent<TankHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);

            if (health.isPlayer)
            {
                DamageIndicator indicator = FindObjectOfType<DamageIndicator>();
                if (indicator != null)
                    indicator.ShowDamage(transform.position);
            }
        }

        Destroy(gameObject);
    }
}