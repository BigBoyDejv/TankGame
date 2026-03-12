using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TankHealth : MonoBehaviour
{
    [Header("Zdravie")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isPlayer = false;

    [Header("Efekty")]
    public GameObject explosionEffect;
    public GameObject fireEffect;
    public GameObject smokeEffect; // Novy efekt dymu

    [Header("UI")]
    public Slider healthSlider;

    void Awake()
    {
        // Awake namiesto Start — inicializuje sa aj na prefaboch
        currentHealth = maxHealth;
    }

    void Start()
    {
        if (healthSlider != null)
            healthSlider.maxValue = maxHealth;
        if (fireEffect != null)
            fireEffect.SetActive(false);
        if (smokeEffect != null)
            smokeEffect.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (healthSlider != null)
            healthSlider.value = currentHealth;

        // Oheň pod 30%, Dym pod 60%
        if (fireEffect != null) fireEffect.SetActive(currentHealth < maxHealth * 0.3f);
        if (smokeEffect != null) smokeEffect.SetActive(currentHealth < maxHealth * 0.6f);

        // Trasenie kamery pri zasahu hraca
        if (isPlayer && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.2f, 0.4f);
        }

        if (currentHealth <= 0f)
            Die();
    }

    void Die()
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        if (isPlayer)
        {
            Debug.Log("Game Over!");
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.GameOver();
        }
        else
        {
            // XP za zabití enemy
            if (XPSystem.Instance != null)
                XPSystem.Instance.AddKill();

            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null) gm.EnemyDestroyed();
        }

        // VRAK - neznicime hned objekt!
        // 1. Zmenime farbu na spalenu ciernu
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Ponechame farbu efektom ohen/dym, zmenime len normalne materialy
            if (!r.gameObject.name.Contains("Effect") && !r.gameObject.name.Contains("Particle"))
            {
                r.material.color = new Color(0.1f, 0.1f, 0.1f);
            }
        }

        // 2. Vypneme pohyb a UI (okrem kolajnic a tanku, aby vrak nezavadzal, ale skriptom povieme cau)
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour s in scripts)
        {
            // Nechame len TankHealth (tento, hned ho vypneme)
            if (s != this) Destroy(s);
        }

        // Ak mal NavMeshAgenta, vypneme ho
        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) Destroy(agent);

        // Ak mal Rigidbody pre vlastny pohyb, znicime ho alebo znizime vahu
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // 3. Po 15 sekundach zmizne
        Destroy(gameObject, 15f);
        
        // Nakoniec vypneme aj tento skript, aby sme uz nedostavali poskodenie
        this.enabled = false;
    }

    public void ApplyBurn(float totalDamage, float duration)
    {
        StartCoroutine(BurnRoutine(totalDamage, duration));
    }

    private IEnumerator BurnRoutine(float totalDamage, float duration)
    {
        float elapsed = 0f;
        float damagePerTick = (totalDamage / duration) * 0.5f;

        if (fireEffect != null)
            fireEffect.SetActive(true);

        while (elapsed < duration)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;

            currentHealth -= damagePerTick;
            currentHealth = Mathf.Max(currentHealth, 0f);

            if (healthSlider != null)
                healthSlider.value = currentHealth;

            if (currentHealth <= 0f)
            {
                Die();
                yield break;
            }
        }

        if (fireEffect != null && currentHealth >= maxHealth * 0.3f)
            fireEffect.SetActive(false);
    }
}