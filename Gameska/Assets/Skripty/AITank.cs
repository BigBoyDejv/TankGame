using System.Collections;
using UnityEngine;

public class AITank : MonoBehaviour
{
    [Header("AI Nastavenia")]
    public Transform player;
    public float detectionRange = 30f;
    public float attackRange = 20f;
    public float moveSpeed = 5f;
    public float turnSpeed = 40f;
    public float fireRate = 2f;

    [Header("Strieľanie")]
    public GameObject shellPrefab;
    public Transform firePoint;
    public float shellSpeed = 40f;
    public float shellDamage = 25f;

    [Header("Vežička")]
    public Transform turret;

    private float nextFireTime = 0f;
    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;

    private Vector3 patrolTarget;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        SetNewPatrolTarget();
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // State machine
        if (distanceToPlayer <= attackRange)
            currentState = State.Attack;
        else if (distanceToPlayer <= detectionRange)
            currentState = State.Chase;
        else
            currentState = State.Patrol;

        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;
            case State.Chase:
                ChasePlayer();
                break;
            case State.Attack:
                AttackPlayer();
                break;
        }
    }

    void Patrol()
    {
        MoveTowards(patrolTarget);

        if (Vector3.Distance(transform.position, patrolTarget) < 2f)
            SetNewPatrolTarget();
    }

    void ChasePlayer()
    {
        MoveTowards(player.position);
    }

    void AttackPlayer()
    {
        // Otočiť sa k hráčovi
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

        // Otočiť vežičku k hráčovi
        if (turret != null)
            turret.rotation = Quaternion.Slerp(turret.rotation, lookRotation, turnSpeed * Time.deltaTime);

        // Strieľanie
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Shoot();
        }
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    void Shoot()
    {
        if (shellPrefab == null || firePoint == null) return;

        GameObject shell = Instantiate(shellPrefab, firePoint.position, firePoint.rotation);
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();

        if (shellRb != null)
            shellRb.linearVelocity = firePoint.forward * shellSpeed;

        TankShell tankShell = shell.GetComponent<TankShell>();
        if (tankShell != null)
            tankShell.damage = shellDamage;

        Destroy(shell, 5f);
    }

    void SetNewPatrolTarget()
    {
        patrolTarget = transform.position + new Vector3(
            Random.Range(-20f, 20f), 0f, Random.Range(-20f, 20f)
        );
    }

    // Zobrazenie detection range v Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
