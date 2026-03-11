using UnityEngine;

public class EnemyTank : MonoBehaviour
{
    [Header("AI Nastavenia")]
    public float detectionRange = 40f;
    public float attackRange = 25f;
    public float moveSpeed = 5f;
    public float turnSpeed = 40f;
    public float patrolRadius = 20f;

    [Header("Strieľanie")]
    public GameObject shellPrefab;
    public Transform firePoint;
    public float shellSpeed = 45f;
    public float shellDamage = 30f;
    public float fireRate = 3f;

    [Header("Efekty")]
    public GameObject explosionEffect;

    private Transform player;
    private Transform turret;
    private Transform gun;
    private float nextFireTime = 0f;
    private Vector3 patrolTarget;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.EndsWith("_Turret")) turret = child;
            if (child.name.EndsWith("_Gun")) gun = child;
        }

        if (firePoint == null && gun != null)
        {
            GameObject fp = new GameObject("EnemyFirePoint");
            fp.transform.SetParent(gun);
            fp.transform.localPosition = new Vector3(0f, 0f, 2f);
            fp.transform.localRotation = Quaternion.identity;
            firePoint = fp.transform;
        }
        else if (firePoint == null && turret != null)
        {
            // Fallback ak nie je gun
            GameObject fp = new GameObject("EnemyFirePoint");
            fp.transform.SetParent(turret);
            fp.transform.localPosition = new Vector3(0f, 0f, 2f);
            fp.transform.localRotation = Quaternion.identity;
            firePoint = fp.transform;
        }

        SetNewPatrolTarget();
    }

    void Update()
    {
        if (player == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        if (distToPlayer <= attackRange)
            currentState = State.Attack;
        else if (distToPlayer <= detectionRange)
            currentState = State.Chase;
        else
            currentState = State.Patrol;

        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase: Chase(); break;
            case State.Attack: Attack(); break;
        }

        if (turret != null && distToPlayer <= detectionRange)
            AimTurretAtPlayer();
    }

    void Patrol()
    {
        MoveTowards(patrolTarget);
        if (Vector3.Distance(transform.position, patrolTarget) < 2f)
            SetNewPatrolTarget();
    }

    void Chase()
    {
        MoveTowards(player.position);
    }

    void Attack()
    {
        RotateBodyTowards(player.position);

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void MoveTowards(Vector3 target)
    {
        RotateBodyTowards(target);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    void RotateBodyTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    void AimTurretAtPlayer()
    {
        Vector3 dir = (player.position - turret.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, targetRot, turnSpeed * 2f * Time.deltaTime);
    }

    void Shoot()
    {
        if (shellPrefab == null || firePoint == null) return;

        // Mier priamo na hráča
        Vector3 dir = player != null
            ? (player.position - firePoint.position).normalized
            : firePoint.forward;

        Quaternion shellRot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
        GameObject shell = Instantiate(shellPrefab, firePoint.position, shellRot);
        Rigidbody rb = shell.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = dir * shellSpeed;

        TankShell tankShell = shell.GetComponent<TankShell>();
        if (tankShell != null)
            tankShell.damage = shellDamage;

        Destroy(shell, 5f);
    }

    void SetNewPatrolTarget()
    {
        patrolTarget = transform.position + new Vector3(
            Random.Range(-patrolRadius, patrolRadius),
            0f,
            Random.Range(-patrolRadius, patrolRadius)
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}