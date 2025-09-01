using UnityEngine;

public class Dummy : Enemy
{
    [Header("Dummy Settings")]
    public float chaseRange = 8f;
    public float stopDistance = 1.5f;

    private enum DummyState
    {
        Idle,
        Chasing,
        Attacking,
        Stunned
    }

    private DummyState currentState = DummyState.Idle;

    protected override void Start()
    {
        base.Start();
        
        // Dummy 전용 스탯 설정
        maxHealth = 50f;
        attackDamage = 15;
        defense = 2;
        moveSpeed = 2.5f;
        attackRange = 1.8f;
        detectionRange = 6f;
        attackCooldown = 1.5f;
        stunDuration = 0.8f;
        
        // 스탯 재초기화
        InitializeStats();
    }

    protected override void UpdateBehavior()
    {
        if (isStunned)
        {
            currentState = DummyState.Stunned;
            StopMovement();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();

        switch (currentState)
        {
            case DummyState.Idle:
                HandleIdleState(distanceToPlayer);
                break;
            case DummyState.Chasing:
                HandleChasingState(distanceToPlayer);
                break;
            case DummyState.Attacking:
                HandleAttackingState();
                break;
            case DummyState.Stunned:
                if (!isStunned)
                    currentState = DummyState.Idle;
                break;
        }
    }

    private void HandleIdleState(float distanceToPlayer)
    {
        StopMovement();

        if (distanceToPlayer <= detectionRange)
        {
            currentState = DummyState.Chasing;
            Debug.Log($"{gameObject.name} detected player! Starting chase.");
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        if (distanceToPlayer > chaseRange)
        {
            currentState = DummyState.Idle;
            Debug.Log($"{gameObject.name} lost player. Returning to idle.");
            return;
        }

        if (distanceToPlayer <= attackRange && CanAttack())
        {
            currentState = DummyState.Attacking;
            StartAttack();
            Debug.Log($"{gameObject.name} starting attack!");
            return;
        }

        if (distanceToPlayer > stopDistance)
        {
            MoveTowards(player.position);
        }
        else
        {
            StopMovement();
        }
    }

    private void HandleAttackingState()
    {
        StopMovement();

        if (!isAttacking)
        {
            currentState = DummyState.Chasing;
        }
    }

    protected override void OnDeath()
    {
        Debug.Log($"Dummy {gameObject.name} has been defeated!");
        
        // Dummy 사망 시 특별한 처리
        // 예: 간단한 아이템 드롭, 소량의 경험치 등
        
        // 5초 후 오브젝트 제거
        Destroy(gameObject, 5f);
    }

    protected override void ApplyStun()
    {
        base.ApplyStun();
        Debug.Log($"{gameObject.name} is stunned!");
    }
}