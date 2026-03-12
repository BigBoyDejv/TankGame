using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AmmoType { Standard, Scatter, Ricochet, Napalm }

public class TankShell : MonoBehaviour
{
    [Header("Základné")]
    public float damage        = 40f;
    public AmmoType ammoType   = AmmoType.Standard;
    public GameObject explosionEffect;

    [Header("Scatter")]
    public int   scatterCount  = 5;
    public float scatterSpread = 15f;
    public float scatterSpeed  = 50f;

    [Header("Ricochet")]
    public int   maxBounces    = 1;
    public float ricoDamageMultiplier = 0.6f;

    [Header("Napalm")]
    public float napalmRadius  = 6f;
    public float burnDuration  = 3f;
    public float burnDPS       = 8f;
    public GameObject fireEffectPrefab;

    private bool  hasHit      = false;
    private int   bounceCount = 0;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        if (ammoType == AmmoType.Ricochet && bounceCount < maxBounces)
        {
            TankHealth th = collision.gameObject.GetComponentInParent<TankHealth>();
            if (th == null)
            {
                bounceCount++;
                damage *= ricoDamageMultiplier;
                Vector3 reflected = Vector3.Reflect(rb.linearVelocity.normalized,
                                                    collision.contacts[0].normal);
                rb.linearVelocity = reflected * rb.linearVelocity.magnitude;
                return;
            }
        }

        hasHit = true;
        HandleHit(collision.gameObject, collision.contacts[0].point);
    }

    void HandleHit(GameObject hitObj, Vector3 hitPoint)
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, hitPoint, Quaternion.identity);

        switch (ammoType)
        {
            case AmmoType.Standard:
            case AmmoType.Ricochet:
            case AmmoType.Scatter:
                DamageTarget(hitObj, damage, hitPoint);
                break;
            case AmmoType.Napalm:
                NapalmHit(hitPoint);
                break;
        }

        Destroy(gameObject);
    }

    void DamageTarget(GameObject target, float dmg, Vector3 hitPoint)
    {
        TankHealth health = target.GetComponentInParent<TankHealth>();
        if (health != null)
        {
            health.TakeDamage(dmg);
            if (health.isPlayer)
            {
                DamageIndicator ind = FindObjectOfType<DamageIndicator>();
                if (ind != null) ind.ShowDamage(hitPoint);
            }
        }
    }

    void NapalmHit(Vector3 center)
    {
        Collider[] cols = Physics.OverlapSphere(center, napalmRadius);
        HashSet<TankHealth> hit = new HashSet<TankHealth>();

        foreach (Collider c in cols)
        {
            TankHealth th = c.GetComponentInParent<TankHealth>();
            if (th != null && !hit.Contains(th))
            {
                hit.Add(th);
                th.TakeDamage(damage * 0.5f);
                th.StartCoroutine(BurnCoroutine(th));
            }
        }
    }

    IEnumerator BurnCoroutine(TankHealth target)
    {
        GameObject fireVFX = null;
        if (fireEffectPrefab != null && target != null)
            fireVFX = Instantiate(fireEffectPrefab, target.transform.position,
                                  Quaternion.identity, target.transform);

        float elapsed = 0f;
        while (elapsed < burnDuration && target != null)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            if (target != null)
                target.TakeDamage(burnDPS * 0.5f);
        }

        if (fireVFX != null) Destroy(fireVFX);
    }
}