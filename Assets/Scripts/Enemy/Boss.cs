using UnityEngine;

public class Boss : Enemy
{
    [Header("Boss Settings")]
    public float phase2HealthThreshold = 0.5f; // 50% 체력에서 페이즈 2
    public float specialAttackCooldown = 5f;
    public float chargeDistance = 8f;
    public float chargeSpeed = 10f;

    [Header("Boss Attacks")]
    public int normalAttackDamage = 30;
    public int specialAttackDamage = 50;
    public int chargeAttackDamage = 40;

    private enum BossState
    {
        Idle,
        Chasing,
        NormalAttack,
        SpecialAttack,
        ChargeAttack,
        Stunned,
        Phase2Transition
    }

    private enum BossPhase
    {
        Phase1,
        Phase2
    }

    private BossState currentState = BossState.Idle;
    private BossPhase currentPhase = BossPhase.Phase1;
    private float lastSpecialAttackTime;
    private bool hasTriggeredPhase2 = false;

    // 추가 애니메이션 파라미터
    private readonly int animIDSpecialAttack = Animator.StringToHash("SpecialAttack");
    private readonly int animIDChargeAttack = Animator.StringToHash("ChargeAttack");
    private readonly int animIDPhase2 = Animator.StringToHash("Phase2");

    protected override void Start()
    {
        base.Start();
        
        // Boss 전용 스탯 설정
        maxHealth = 300f;
        attackDamage = normalAttackDamage;
        defense = 10;
        moveSpeed = 3.5f;
        attackRange = 3f;
        detectionRange = 15f;
        attackCooldown = 2.5f;
        stunDuration = 0.5f; // 보스는 스턴 시간이 짧음
        
        // 스탯 재초기화
        InitializeStats();
        lastSpecialAttackTime = Time.time;
    }

    protected override void UpdateBehavior()
    {
        CheckPhaseTransition();

        if (isStunned)
        {
            currentState = BossState.Stunned;
            StopMovement();
            return;
        }

        float distanceToPlayer = GetDistanceToPlayer();

        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState(distanceToPlayer);
                break;
            case BossState.Chasing:
                HandleChasingState(distanceToPlayer);
                break;
            case BossState.NormalAttack:
                HandleNormalAttackState();
                break;
            case BossState.SpecialAttack:
                HandleSpecialAttackState();
                break;
            case BossState.ChargeAttack:
                HandleChargeAttackState();
                break;
            case BossState.Stunned:
                if (!isStunned)
                    currentState = BossState.Chasing;
                break;
            case BossState.Phase2Transition:
                HandlePhase2Transition();
                break;
        }
    }

    private void CheckPhaseTransition()
    {
        if (!hasTriggeredPhase2 && GetHealthPercentage() <= phase2HealthThreshold)
        {
            hasTriggeredPhase2 = true;
            currentState = BossState.Phase2Transition;
            currentPhase = BossPhase.Phase2;
            
            // 페이즈 2 스탯 강화
            moveSpeed *= 1.3f;
            attackCooldown *= 0.8f;
            specialAttackCooldown *= 0.7f;
            
            Debug.Log($"{gameObject.name} entered Phase 2!");
        }
    }

    private void HandleIdleState(float distanceToPlayer)
    {
        StopMovement();

        if (distanceToPlayer <= detectionRange)
        {
            currentState = BossState.Chasing;
            Debug.Log($"Boss {gameObject.name} detected player!");
        }
    }

    private void HandleChasingState(float distanceToPlayer)
    {
        if (distanceToPlayer > detectionRange)
        {
            currentState = BossState.Idle;
            return;
        }

        // 공격 패턴 결정
        if (CanAttack() && distanceToPlayer <= attackRange)
        {
            DecideAttackPattern(distanceToPlayer);
            return;
        }

        MoveTowards(player.position);
    }

    private void DecideAttackPattern(float distanceToPlayer)
    {
        // 페이즈 2에서는 더 다양한 공격 패턴
        if (currentPhase == BossPhase.Phase2)
        {
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue < 0.4f && Time.time >= lastSpecialAttackTime + specialAttackCooldown)
            {
                StartSpecialAttack();
            }
            else if (randomValue < 0.7f && distanceToPlayer > 4f)
            {
                StartChargeAttack();
            }
            else
            {
                StartNormalAttack();
            }
        }
        else
        {
            // 페이즈 1에서는 일반 공격과 가끔 특수 공격
            if (Time.time >= lastSpecialAttackTime + specialAttackCooldown && Random.Range(0f, 1f) < 0.3f)
            {
                StartSpecialAttack();
            }
            else
            {
                StartNormalAttack();
            }
        }
    }

    private void StartNormalAttack()
    {
        currentState = BossState.NormalAttack;
        attackDamage = normalAttackDamage;
        StartAttack();
        Debug.Log($"{gameObject.name} performs normal attack!");
    }

    private void StartSpecialAttack()
    {
        currentState = BossState.SpecialAttack;
        attackDamage = specialAttackDamage;
        lastSpecialAttackTime = Time.time;
        isAttacking = true;
        
        if (animator != null)
        {
            animator.SetTrigger(animIDSpecialAttack);
        }
        
        Debug.Log($"{gameObject.name} performs SPECIAL ATTACK!");
    }

    private void StartChargeAttack()
    {
        currentState = BossState.ChargeAttack;
        attackDamage = chargeAttackDamage;
        isAttacking = true;
        
        if (animator != null)
        {
            animator.SetTrigger(animIDChargeAttack);
        }
        
        Debug.Log($"{gameObject.name} performs CHARGE ATTACK!");
    }

    private void HandleNormalAttackState()
    {
        StopMovement();
        
        if (!isAttacking)
        {
            currentState = BossState.Chasing;
        }
    }

    private void HandleSpecialAttackState()
    {
        StopMovement();
        
        if (!isAttacking)
        {
            currentState = BossState.Chasing;
        }
    }

    private void HandleChargeAttackState()
    {
        if (!isAttacking)
        {
            currentState = BossState.Chasing;
            return;
        }

        // 차지 어택 중에는 플레이어 방향으로 빠르게 이동
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0f;
            
            characterController.Move(direction * chargeSpeed * Time.deltaTime);
            
            // 중력 적용
            if (!characterController.isGrounded)
            {
                characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
        }
    }

    private void HandlePhase2Transition()
    {
        StopMovement();
        
        if (animator != null)
        {
            animator.SetTrigger(animIDPhase2);
        }
        
        // 페이즈 전환 애니메이션이 끝나면 다시 추적 시작
        // 실제로는 애니메이션 이벤트로 처리하는 것이 좋음
        Invoke(nameof(EndPhase2Transition), 2f);
    }

    private void EndPhase2Transition()
    {
        currentState = BossState.Chasing;
        Debug.Log($"{gameObject.name} Phase 2 transition complete!");
    }

    protected override void ApplyStun()
    {
        // 보스는 페이즈 2에서 스턴 저항력이 있음
        if (currentPhase == BossPhase.Phase2 && Random.Range(0f, 1f) < 0.4f)
        {
            Debug.Log($"{gameObject.name} resisted stun!");
            return;
        }
        
        base.ApplyStun();
        Debug.Log($"Boss {gameObject.name} is stunned!");
    }

    protected override void OnDeath()
    {
        Debug.Log($"BOSS {gameObject.name} HAS BEEN DEFEATED!");
        
        // 보스 사망 시 특별한 처리
        // 예: 특별한 아이템 드롭, 대량의 경험치, 게임 클리어 등
        
        // 보스는 바로 제거하지 않고 사망 애니메이션 후 제거
        Destroy(gameObject, 10f);
    }

    // 추가 공격 히트 처리 (특수 공격용)
    public void OnSpecialAttackHit()
    {
        Debug.Log($"{gameObject.name} Special Attack Hit!");
        
        // 범위 공격으로 처리
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange * 1.5f)
        {
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(specialAttackDamage);
            }
        }
    }

    public void OnChargeAttackHit()
    {
        Debug.Log($"{gameObject.name} Charge Attack Hit!");
        
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(chargeAttackDamage);
            }
        }
    }
}