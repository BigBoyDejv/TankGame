using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    [Header("Zdravie")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isPlayer = false;

    [Header("Efekty")]
    public GameObject explosionEffect;
    public GameObject fireEffect;

    [Header("UI - len pre hráča")]
    public Slider healthSlider;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthSlider != null)
            healthSlider.maxValue = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + " dostal " + damage + " damage. HP: " + currentHealth);

        // Update HP bar
        if (healthSlider != null)
            healthSlider.value = currentHealth;

        // Oheň keď HP < 30%
        if (fireEffect != null && currentHealth < maxHealth * 0.3f)
            fireEffect.SetActive(true);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, transform.rotation);

        if (isPlayer)
        {
            Debug.Log("Hráč bol zničený! Game Over.");
            // Tu môžeš neskôr pridať Game Over UI
        }
        else
        {
            Debug.Log(gameObject.name + " bol zničený!");
            // Notifikuj GameManager
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
                gm.EnemyDestroyed();
        }

        Destroy(gameObject, 0.5f);
    }
}
