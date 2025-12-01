using UnityEngine;
using System.Collections;

public class PatrolBehaviour : MonoBehaviour, IEnemyBehaviour
{
    public Transform[] patrolPoints;
    public float idleChance = 0.3f;
    public float idleMin = 1f;
    public float idleMax = 3f;
    public float chaseCooldown = 0.5f;
    public float lostSightDelay = 1.0f;

    BaseEnemy enemy;
    int idx = 0;
    enum State { Patrolling, Idle, Chasing }
    State state = State.Patrolling;
    float idleTimer = 0f;

    float lostSightTimer = 0f;
    Vector3 lastKnownPlayerPos;

    public void Init(BaseEnemy enemy)
    {
        this.enemy = enemy;
        if (enemy.agent != null) enemy.agent.isStopped = false;
    }

    public void OnEnter() { }

    public void OnExit() { }

    public void Tick()
    {
        if (enemy == null || enemy.playerTransform == null) return;

        bool canSeePlayer = enemy.CanSeePlayer();

        if (canSeePlayer && state != State.Chasing)
        {
            state = State.Chasing;
            enemy.StartChase();
            lostSightTimer = 0f;
            lastKnownPlayerPos = enemy.playerTransform.position;
        }

        switch (state)
        {
            case State.Patrolling:
                if (enemy.agent.isStopped) enemy.agent.isStopped = false;

                if (patrolPoints == null || patrolPoints.Length == 0)
                {
                    // Si on a mis aucun points de patrouilles : on reste en Idle
                    state = State.Idle;
                    idleTimer = Random.Range(idleMin, idleMax);
                    enemy.PlayIdle();
                    break;
                }

                Transform target = patrolPoints[idx];
                enemy.agent.SetDestination(target.position);
                enemy.PlayWalk(true);

                if (!enemy.agent.pathPending && enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.1f)
                {
                    // Soit on reste en idle, soit on bouge
                    enemy.PlayWalk(false);
                    if (Random.value <= idleChance)
                    {
                        state = State.Idle;
                        idleTimer = Random.Range(idleMin, idleMax);
                        enemy.PlayIdle();
                    }
                    else
                    {
                        idx = (idx + 1) % patrolPoints.Length;
                    }
                }
                break;

            case State.Idle:
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    state = State.Patrolling;
                }
                break;

            case State.Chasing:
                if (canSeePlayer)
                {
                    lastKnownPlayerPos = enemy.playerTransform.position;
                    lostSightTimer = 0f;
                    enemy.ChasePlayer();
                }

                else
                {
                    // on continue a avancer sur la dernière position connue du joueur pendant un petit delai :
                    lostSightTimer += Time.deltaTime;
                    enemy.agent.isStopped = false;
                    enemy.agent.SetDestination(lastKnownPlayerPos);
                    enemy.PlayWalk(true);

                    // si le delai est passé ou qu'on est arrivé sur la dernière position du joueur connue : on retourne a la patrouille
                    bool reachedLastKnown = !enemy.agent.pathPending && enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.1f;
                    if (lostSightTimer >= lostSightDelay || reachedLastKnown)
                    {
                        state = State.Patrolling;
                        enemy.StopChase(false);
                        lostSightTimer = 0f;
                    }
                }
                break;
        }
    }
}
