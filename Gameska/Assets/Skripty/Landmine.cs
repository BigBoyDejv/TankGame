using UnityEngine;
using System.Collections;

public class Landmine : MonoBehaviour
{
    private float damage;
    private float radius;
    private GameObject explosionPrefab;
    private SkillSystem owner;
    private bool triggered = false;

    // Blikanie
    private Renderer rend;
    private float blinkTimer = 0f;

    public void Setup(float dmg, float rad, GameObject explosion, SkillSystem sk)
    {
        damage          = dmg;
        radius          = rad;
        explosionPrefab = explosion;
        owner           = sk;

        rend = GetComponent<Renderer>();

        // Ignoruj kolízie s hráčom
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider mineCol  = GetComponent<Collider>();
            Collider[] playerCols = player.GetComponentsInChildren<Collider>();
            if (mineCol != null)
                foreach (Collider pc in playerCols)
                    Physics.IgnoreCollision(mineCol, pc);
        }
    }

    void Update()
    {
        // Blikanie červenej LED
        if (rend != null)
        {
            blinkTimer += Time.deltaTime * 2f;
            float t = Mathf.Abs(Mathf.Sin(blinkTimer));
            rend.material.color = Color.Lerp(
                new Color(0.2f, 0.2f, 0.1f),
                new Color(1f, 0.1f, 0f),
                t
            );
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (triggered) return;

        // Spusti len keď naň narazí enemy
        TankHealth th = collision.gameObject.GetComponentInParent<TankHealth>();
        if (th == null || th.isPlayer) return;

        triggered = true;
        StartCoroutine(Explode());
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        TankHealth th = other.GetComponentInParent<TankHealth>();
        if (th == null || th.isPlayer) return;
        triggered = true;
        StartCoroutine(Explode());
    }

    IEnumerator Explode()
    {
        // Krátke oneskorenie pre dramatický efekt
        yield return new WaitForSeconds(0.1f);

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // AoE damage
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider c in cols)
        {
            TankHealth th = c.GetComponentInParent<TankHealth>();
            if (th == null) continue;
            float dist    = Vector3.Distance(c.transform.position, transform.position);
            float falloff = 1f - Mathf.Clamp01(dist / radius);
            th.TakeDamage(damage * falloff);
        }

        if (owner != null) owner.MineTriggered(gameObject);
        Destroy(gameObject);
    }
}