using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCLogic : MonoBehaviour
{
    private Animator anim;
    [SerializeField] private AudioSource chaseSound;
    [SerializeField] private AudioSource heartbeatSound;
    [SerializeField] private AudioSource heavyBreathingSound;
    [SerializeField] private Transform standPosition; // Assign the target position in the Inspector
    [SerializeField] private Transform player; // Assign the player Transform in the Inspector
    private AudioSource alarmingSound;
    private NavMeshAgent agent;
    private bool isChasing = false; // Flag to control chasing behavior

    void Start()
    {
        anim = GetComponent<Animator>();
        alarmingSound = GetComponent<AudioSource>();
        agent = GetComponent<NavMeshAgent>();

        anim.StopPlayback();
        anim.Play("walk");

        if (agent != null && standPosition != null)
        {
            agent.speed = 2.0f;
            agent.acceleration = 3.0f;

            // Ensure the agent is enabled and on the NavMesh before setting destination
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(standPosition.position);
            }
            else
            {
                Debug.LogWarning("NavMeshAgent is not active or not placed on a NavMesh. Ensure the NavMesh is baked and the agent is positioned correctly.");
            }
        }
    }

    void Update()
    {
        // If chasing, continuously update destination to player's position
        if (isChasing && player != null && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            Debug.Log("CHASE");
            agent.SetDestination(player.position);
            agent.isStopped = false; // Ensure the agent is moving
            anim.StopPlayback();
            anim.Play("walk");
        }
        else
        {
            // Check if the NPC has arrived at the stand position, with proper checks to avoid errors
            if (agent != null && agent.enabled && agent.isOnNavMesh && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= agent.stoppingDistance + 0.1f && agent.velocity.magnitude < 0.1f)
            {
                Debug.Log("STOP");
                // Stop moving and stay at the position (only if not chasing)
                agent.isStopped = true;
                anim.StopPlayback();
                anim.Play("idle");
            }
        }

        // Always look at the player if player is assigned
        if (player != null)
        {
            // Make the NPC face the player (only rotate on Y-axis to avoid tilting)
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Keep the NPC upright
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    // New function to start chasing the player
    public void StartChase()
    {
        StartCoroutine(StartChaseAfterDelay());
    }
    private IEnumerator StartChaseAfterDelay()
    {
        //enable chase effects
        alarmingSound.Play();
        heartbeatSound.Play();
        heavyBreathingSound.Play();

        yield return new WaitForSeconds(3.0f);

        //Start chase
        chaseSound.Play();
        agent.speed = 1.5f;

        if (agent != null && player != null)
        {
            isChasing = true;
            // Immediately set destination to player and ensure agent is active
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.SetDestination(player.position);
                agent.isStopped = false;
            }
            else
            {
                Debug.LogWarning("Cannot start chase: NavMeshAgent is not active or not on NavMesh.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot start chase: Agent or Player is null.");
        }
    }
}
