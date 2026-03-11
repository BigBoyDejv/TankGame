using UnityEngine;

public class TankShell : MonoBehaviour
{
    public float damage = 40f;
    public GameObject explosionEffect;

    void OnCollisionEnter(Collision collision)
    {
        // Explózia efekt
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        // Damage na tank
        TankHealth health = collision.gameObject.GetComponent<TankHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
