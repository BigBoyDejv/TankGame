using UnityEngine;
using UnityEngine.AI;

public class EnemyTank : MonoBehaviour
{
    [Header("AI Nastavenia")]
    public float detectionRange = 40f;
    public float attackRange = 25f;
    public float moveSpeed = 5f;
    public float turnSpeed = 40f;
    public float patrolRadius = 20f;

    [Header("Strieľanie")]
    public GameObject shellPrefab; // ← toto je ShellEnemy
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

    private NavMeshAgent agent;
    private Rigidbody rb;

    private enum State { Patrol, Chase, Attack }
    private State currentState = State.Patrol;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private int stuckCounter = 0;

    void Start()
{
    // Počkáme kým XPSystem vytvorí hráča
    Invoke(nameof(DelayedInit), 0.2f);
}

void DelayedInit()
{
    FindPlayer();
    FindTankParts();
    CreateFirePoint();
    SetupNavMeshAgent();
    SetupRigidbody();
    Invoke(nameof(CheckNavMesh), 0.1f);
    lastPosition = transform.position;
}

    void FindTankParts()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.EndsWith("_Turret")) turret = child;
            if (child.name.EndsWith("_Gun")) gun = child;
        }
    }

    void CreateFirePoint()
    {
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
            GameObject fp = new GameObject("EnemyFirePoint");
            fp.transform.SetParent(turret);
            fp.transform.localPosition = new Vector3(0f, 0f, 2f);
            fp.transform.localRotation = Quaternion.identity;
            firePoint = fp.transform;
        }
    }

    void SetupNavMeshAgent()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Log("NavMeshAgent pridaný automaticky");
        }

        // DÔLEŽITÉ: Optimalizované nastavenia pre tank
        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed * 2f;
        agent.acceleration = 12f; // Zvýšené pre rýchlejšiu odozvu
        agent.stoppingDistance = attackRange * 0.5f; // Zmenšené
        agent.radius = 1.2f; // Zmenšené
        agent.height = 0.8f; // Zvýšené
        agent.baseOffset = 0.1f; // Mierne zdvihnuté
        
        // DÔLEŽITÉ: Tieto nastavenia pomáhajú pri pohybe
        agent.autoBraking = true;
        agent.autoRepath = true;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.GoodQualityObstacleAvoidance;
        
        // Povoliť pohyb po schodoch a rampách
        agent.autoTraverseOffMeshLink = true;
    }

    void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // DÔLEŽITÉ: Necháme NavMeshAgent riadiť pohyb
            rb.useGravity = false;
            Log("Rigidbody pridaný ako kinematic");
        }
    }

    void CheckNavMesh()
    {
        if (!agent.isOnNavMesh)
        {
            LogWarning("Tank nie je na NavMesh! Hľadám blízke miesto...");
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                Log("Tank presunutý na NavMesh");
            }
            else
            {
                LogError("Nenašlo sa miesto na NavMesh!");
            }
        }
        else
        {
            Log("Tank je na NavMesh");
        }

        SetNewPatrolTarget();
    }

   void Update()
{
     if (player == null)
        {
            FindPlayer();
            if (player == null) return;
        }

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Kontrola či nie je tank zaseknutý
        CheckIfStuck();

        // Zmena stavu podľa vzdialenosti
        UpdateState(distToPlayer);

        // Vykonanie aktuálneho stavu
        switch (currentState)
        {
            case State.Patrol: Patrol(); break;
            case State.Chase:  Chase();  break;
            case State.Attack: Attack(); break;
        }

        // Otočenie veže ak je hráč v dosahu
        if (turret != null && distToPlayer <= detectionRange)
            AimTurretAtPlayer();
    }

    void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;
        
        if (stuckTimer > 2f) // Každé 2 sekundy skontrolujeme
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            // Ak sa takmer nepohol a mal by sa hýbať
            if (distanceMoved < 0.5f && agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
            {
                stuckCounter++;
                LogWarning($"Tank sa zdá byť zaseknutý! Pokus o uvoľnenie #{stuckCounter}");
                
                if (stuckCounter >= 2)
                {
                    // Pokus o uvoľnenie
                UnstickTank();
                    stuckCounter = 0;
                }
            }
            else
            {
                stuckCounter = 0; // Reset ak sa hýbe
            }
            
            lastPosition = transform.position;
            stuckTimer = 0f;
        }
    }

    void UnstickTank()
    {
        // Metóda 1: Reštartovať agenta
        agent.isStopped = true;
        agent.ResetPath();
        agent.isStopped = false;
        
        // Metóda 2: Skúsiť nový cieľ
        if (currentState == State.Patrol)
        {
            SetNewPatrolTarget();
        }
        else if (currentState == State.Chase || currentState == State.Attack)
        {
            agent.SetDestination(player.position);
        }
        
        // Metóda 3: Malý posun
        Vector3 randomDir = Random.insideUnitSphere * 2f;
        randomDir.y = 0;
        agent.Move(randomDir);
        
        Log("Pokus o uvoľnenie tanku");
    }

    void UpdateState(float distToPlayer)
    {
        State previousState = currentState;
        
        if (distToPlayer <= attackRange)
            currentState = State.Attack;
        else if (distToPlayer <= detectionRange)
            currentState = State.Chase;
        else
            currentState = State.Patrol;

        if (previousState != currentState)
            Log($"Zmena stavu: {previousState} -> {currentState}");
    }

    bool AgentReady()
    {
        return agent != null && agent.isOnNavMesh && agent.enabled && !agent.isStopped;
    }

    void Patrol()
    {
        if (!AgentReady()) 
        {
            // Núdzový pohyb
            SimpleEmergencyMove();
            return;
        }

        agent.isStopped = false;

        // Ak nemá cestu alebo je blízko cieľa, hľadaj nový
        if (!agent.hasPath || agent.remainingDistance < 2f)
        {
            SetNewPatrolTarget();
        }
    }

    void SimpleEmergencyMove()
    {
        // Jednoduchý pohyb vpred ak NavMesh nefunguje
        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        
        // Náhodné otáčanie
        transform.Rotate(0f, Random.Range(-30f, 30f) * Time.deltaTime, 0f);
    }

    void Chase()
    {
        if (!AgentReady()) return;
        
        agent.isStopped = false;
        
        // Pravidelne aktualizujeme cieľ
        if (!agent.pathPending)
        {
            agent.SetDestination(player.position);
        }
        
        Log($"Prenasledovanie - Vzdialenosť k cieľu: {agent.remainingDistance}", false);
    }

    void Attack()
    {
        if (!AgentReady()) 
        {
            // Útok bez pohybu
            RotateTowardsPlayer();
        }
        else
        {
            // Zastavíme pohyb
            agent.isStopped = true;
            
            // Otáčame sa k hráčovi
            RotateTowardsPlayer();
        }

        // Strieľanie
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnSpeed * Time.deltaTime
            );
        }
    }

    void AimTurretAtPlayer()
    {
        if (turret == null) return;
        
        Vector3 dir = (player.position - turret.position).normalized;
        dir.y = 0f;
        if (dir == Vector3.zero) return;
        
        Quaternion targetRot = Quaternion.LookRotation(dir);
        turret.rotation = Quaternion.RotateTowards(
            turret.rotation, targetRot, turnSpeed * 2f * Time.deltaTime
        );
    }

    void Shoot()
    {
        if (shellPrefab == null || firePoint == null)
        {
            LogWarning("Nie je nastavený shellPrefab alebo firePoint");
            return;
        }

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
        
        Log("Výstrel!");
    }

    void SetNewPatrolTarget()
    {
        if (!AgentReady()) 
        {
            LogWarning("Nemôžem nastaviť patrol target");
            return;
        }

        // Skúsime nájsť bod na NavMesh
        for (int i = 0; i < 5; i++) // Skúsime 5 krát
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
            randomDir += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
                patrolTarget = hit.position;
                agent.SetDestination(patrolTarget);
                Log($"Nový patrol target: {patrolTarget}");
                return;
            }
        }
        
        // Ak sa nepodarí nájsť, skúsime centrum mapy
        if (NavMesh.SamplePosition(Vector3.zero, out NavMeshHit centerHit, 100f, NavMesh.AllAreas))
        {
            agent.SetDestination(centerHit.position);
            Log("Používam centrum mapy ako patrol target");
        }
    }

    void Log(string message, bool alwaysShow = true)
    {
        if (showDebugLogs || alwaysShow)
            Debug.Log($"[EnemyTank] {message}");
    }

    void LogWarning(string message)
    {
        Debug.LogWarning($"[EnemyTank] {message}");
    }

    void LogError(string message)
    {
        Debug.LogError($"[EnemyTank] {message}");
    }

    private bool isBlinded = false;

    public void SetBlinded(bool blind)
    {
        isBlinded = blind;
        if (blind)
        {
            currentState = State.Patrol;
            if (AgentReady()) agent.isStopped = false;
        }
        Log($"Tank blinded: {blind}");
    }


 void FindPlayer()
    {
        if (XPSystem.PlayerTransform != null)
        {
            player = XPSystem.PlayerTransform;
            Debug.Log($"Enemy {name} našiel hráča: {player.name}");
        }
        else
        {
            Debug.LogWarning($"Enemy {name} nenašiel hráča - XPSystem.PlayerTransform je null");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (Application.isPlaying && patrolTarget != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(patrolTarget, 1f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }
        
        // Zobrazenie cesty agenta
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
            }
        }
    }
}