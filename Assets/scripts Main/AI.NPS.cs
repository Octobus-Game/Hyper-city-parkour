using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SELVEL_AI : MonoBehaviour
{
    [Header("Wander Settings")]
    public float wanderRadius = 10f;
    public float wanderDelay = 3f;

    [Header("Follow Settings")]
    public Transform player;
    public bool followPlayer = false;
    public float followDistance = 2f;
    public float stoppingDistance = 1.5f;
    public KeyCode callKey = KeyCode.F;
    public float callDuration = 5f;
    public float rotationSpeed = 5f;

    [Header("Push Away Settings")]
    public GameObject triggerObject;
    public bool enablePushAway = true;
    public float pushAwayDistance = 5f;
    public float pushAwayDuration = 3f;

    [Header("Gizmos Settings")]
    public bool showGizmos = true;
    public Color wanderRadiusColor = new Color(0f, 1f, 0f, 0.2f);
    public Color followDistanceColor = new Color(1f, 1f, 0f, 0.2f);
    public Color stoppingDistanceColor = new Color(1f, 0f, 0f, 0.3f);

    private NavMeshAgent agent;
    private float timer;
    private bool isCalled = false;
    private float callTimer = 0f;
    private bool isPushedAway = false;
    private float pushAwayTimer = 0f;
    private TriggerDetector triggerDetector;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent отсутствует на объекте " + gameObject.name);
            enabled = false;
            return;
        }

        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        SetupTrigger();

        // Проверяем, что агент на NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(gameObject.name + " не находится на NavMesh! Поместите объект на NavMesh.");
            enabled = false;
        }
    }

    void Update()
    {
        if (player == null || !IsAgentValid()) return;

        if (Input.GetKeyDown(callKey))
        {
            CallNPC();
        }

        if (isCalled)
        {
            callTimer += Time.deltaTime;
            if (callTimer >= callDuration)
            {
                isCalled = false;
                callTimer = 0f;
            }
        }

        if (isPushedAway)
        {
            pushAwayTimer += Time.deltaTime;
            if (pushAwayTimer >= pushAwayDuration)
            {
                isPushedAway = false;
                pushAwayTimer = 0f;
            }
        }

        if (isPushedAway)
        {
            RotateTowardsMovement();
        }
        else if (followPlayer || isCalled)
        {
            FollowPlayer();
        }
        else
        {
            Wander();
            RotateTowardsMovement();
        }
    }

    // Проверка валидности агента
    private bool IsAgentValid()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh && agent.isActiveAndEnabled;
    }

    private void SetupTrigger()
    {
        if (triggerObject != null)
        {
            Collider triggerCollider = triggerObject.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                Debug.LogWarning("У объекта " + triggerObject.name + " нет коллайдера!");
                return;
            }

            if (!triggerCollider.isTrigger)
            {
                Debug.LogWarning("Коллайдер объекта " + triggerObject.name + " не является триггером! Устанавливаем isTrigger = true");
                triggerCollider.isTrigger = true;
            }

            triggerDetector = triggerObject.GetComponent<TriggerDetector>();
            if (triggerDetector == null)
            {
                triggerDetector = triggerObject.AddComponent<TriggerDetector>();
            }

            triggerDetector.onPlayerEnter = OnTriggerDetected;
        }
    }

    private void OnTriggerDetected()
    {
        PushAwayFromPlayer();
    }

    private void FollowPlayer()
    {
        if (!IsAgentValid()) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > followDistance)
        {
            agent.SetDestination(player.position);
            RotateTowardsTarget(player.position);
        }
        else
        {
            agent.ResetPath();
            RotateTowardsTarget(player.position);
        }
    }

    private void Wander()
    {
        if (!IsAgentValid()) return;

        timer += Time.deltaTime;

        if (timer >= wanderDelay)
        {
            Vector3 newDestination = GetRandomPoint(transform.position, wanderRadius);
            if (newDestination != transform.position)
            {
                agent.SetDestination(newDestination);
            }
            timer = 0f;
        }
    }

    private void CallNPC()
    {
        isCalled = true;
        callTimer = 0f;
        Debug.Log(gameObject.name + " был подозван к игроку!");
    }

    private void PushAwayFromPlayer()
    {
        if (!enablePushAway || !IsAgentValid()) return;

        isCalled = false;
        callTimer = 0f;

        isPushedAway = true;
        pushAwayTimer = 0f;

        Vector3 directionAway = (transform.position - player.position).normalized;
        Vector3 targetPosition = transform.position + directionAway * pushAwayDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, pushAwayDistance, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            Debug.Log(gameObject.name + " отталкивается от игрока!");
        }
        else
        {
            Debug.LogWarning("Не удалось найти валидную точку на NavMesh для отталкивания");
        }
    }

    private void RotateTowardsTarget(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void RotateTowardsMovement()
    {
        if (!IsAgentValid()) return;

        if (agent.velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private Vector3 GetRandomPoint(Vector3 origin, float radius)
    {
        Vector2 randomDirection = Random.insideUnitCircle * radius;
        Vector3 randomPoint = origin + new Vector3(randomDirection.x, 0, randomDirection.y);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return origin;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Радиус блуждания
        Gizmos.color = wanderRadiusColor;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (player != null)
        {
            // Дистанция следования
            Gizmos.color = followDistanceColor;
            Gizmos.DrawWireSphere(player.position, followDistance);

            // Радиус остановки
            Gizmos.color = stoppingDistanceColor;
            Gizmos.DrawWireSphere(player.position, stoppingDistance);
        }
    }

    private class TriggerDetector : MonoBehaviour
    {
        public System.Action onPlayerEnter;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                onPlayerEnter?.Invoke();
            }
        }
    }
}