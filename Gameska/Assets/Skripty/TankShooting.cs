using System.Collections;
using UnityEngine;

public class TankShooting : MonoBehaviour
{
    [Header("Strieľanie")]
    public GameObject shellPrefab;
    public Transform firePoint;
    public float shellSpeed = 50f;
    public float fireRate = 1f;
    public float shellDamage = 40f;

    [Header("Otáčanie vežičky")]
    public Transform turret;
    public float turretSpeed = 80f;

    [Header("Zvuk")]
    public AudioSource audioSource;
    public AudioClip shootSound;

    [Header("Efekty")]
    public ParticleSystem muzzleFlash;

    private float nextFireTime = 0f;

    void Update()
    {
        // Otáčanie vežičky myšou
        if (turret != null)
        {
            float turretInput = Input.GetAxis("Mouse X");
            turret.Rotate(0f, turretInput * turretSpeed * Time.deltaTime, 0f);
        }

        // Strieľanie na ľavý klik
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        if (shellPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Nastav shellPrefab a firePoint v Inspectore!");
            return;
        }

        // Vytvor projektil
        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();

        if (shellRb != null)
        {
            shellRb.linearVelocity = firePoint.forward * shellSpeed;
        }

        // Nastav damage na projektile
        TankShell tankShell = shell.GetComponent<TankShell>();
        if (tankShell != null)
        {
            tankShell.damage = shellDamage;
        }

        // Efekty
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        // Zničí projektil po 5 sekundách
        Destroy(shell, 5f);
    }
}
