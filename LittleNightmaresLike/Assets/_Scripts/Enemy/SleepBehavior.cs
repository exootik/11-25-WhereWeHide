using UnityEngine;

public class SleepBehaviour : MonoBehaviour, IEnemyBehaviour
{
    public float wakeUpWait = 1.0f;
    public float lostSightDelay = 1.0f;

    BaseEnemy enemy;

    enum State { Sleeping, Idle, Chasing }
    State state = State.Sleeping;

    float lostSightTimer = 0f;
    Vector3 lastKnownPlayerPos;

    public void Init(BaseEnemy enemy)
    {
        this.enemy = enemy;

        if (enemy != null)
        {
            enemy.PlaySleep();
        }
    }

    public void OnEnter() { }

    public void OnExit() { }

    public void Tick()
    {
        if (enemy == null || enemy.playerTransform == null) return;

        bool canSeePlayer = enemy.CanSeePlayer();

        if (canSeePlayer && state == State.Sleeping)
        {
            state = State.Chasing;
            lastKnownPlayerPos = enemy.playerTransform.position;
            lostSightTimer = 0f;

            enemy.StartWakeUp(wakeUpWait);
            return;
        }

        switch (state)
        {
            case State.Sleeping:
                break;

            case State.Idle:
                if (canSeePlayer)
                {
                    state = State.Chasing;
                    lastKnownPlayerPos = enemy.playerTransform.position;
                    lostSightTimer = 0f;

                    enemy.StartWakeUp(wakeUpWait);
                }
                break;

            case State.Chasing:
                if (canSeePlayer)
                {
                    lastKnownPlayerPos = enemy.playerTransform.position;
                    lostSightTimer = 0f;

                    enemy.PlayRun(true);
                    enemy.ChasePlayer();
                }
                else
                {
                    lostSightTimer += Time.deltaTime;
                    enemy.agent.isStopped = false;
                    enemy.agent.SetDestination(lastKnownPlayerPos);
                    enemy.PlayRun(true);

                    bool reachedLastKnown = !enemy.agent.pathPending && enemy.agent.remainingDistance <= enemy.agent.stoppingDistance + 0.1f;
                    if (lostSightTimer >= lostSightDelay || reachedLastKnown)
                    {
                        state = State.Sleeping;
                        lostSightTimer = 0f;
                        lastKnownPlayerPos = Vector3.zero;
                        enemy.PlaySleep();
                        enemy.StopChase(true);
                    }
                }
                break;
        }
    }
}
