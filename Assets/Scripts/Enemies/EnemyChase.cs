using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[DisallowMultipleComponent]
public class EnemyChase : MonoBehaviour
{
    [SerializeField] private Transform target;
    [Header("Detection")]
    [Tooltip("Chase only while the player is inside this distance from the enemy, in metres.")]
    [SerializeField, Min(0f)] private float detectionRadius = 3f;
    [Header("Pathfinding")]
    [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float targetSampleRadius = 2f;
    private NavMeshAgent agent;
    private float nextRepath;

    public Transform Target { get => target; set => target = value; }

    private void Awake() => agent = GetComponent<NavMeshAgent>();

    private void Start()
    {
        if (target == null)
        {
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (player != null) target = player.transform;
        }
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, agent.areaMask))
                agent.Warp(hit.position);
            if (!agent.isOnNavMesh)
                Debug.LogWarning("Enemy is outside the NavMesh. Move it onto the floor and rebake navigation.", this);
        }
    }

    private void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (target == null || (target.position - transform.position).sqrMagnitude > detectionRadius * detectionRadius)
        {
            agent.isStopped = true;
            // Clear even a pending path so the enemy stops immediately on losing detection.
            if (agent.hasPath || agent.pathPending) agent.ResetPath();
            nextRepath = 0f;
            return;
        }
        agent.isStopped = false;
        if (Time.time < nextRepath) return;
        nextRepath = Time.time + repathInterval;
        // Sample near the player's feet, rather than requiring its origin to be on the mesh.
        if (NavMesh.SamplePosition(target.position, out NavMeshHit hit, targetSampleRadius, agent.areaMask))
            agent.SetDestination(hit.position);
        else if (agent.hasPath)
            agent.ResetPath();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
