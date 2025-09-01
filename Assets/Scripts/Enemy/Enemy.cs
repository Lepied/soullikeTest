using UnityEngine;

public abstract class Enemy : MonoBehaviour
{
    [Header("Basic Stats")]
    public float maxHealth = 100f;
    public int attackDamage = 20;
    public int defense = 5;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float detectionRange = 10f;

    [Header("Combat Settings")]
    public float attackCooldown = 2f;
    public float stunDuration = 1f;

    [Header("Components")]
    protected CharacterController characterController;
    protected Animator animator;
    protected Transform player;

    [Header("State")]
    protected float currentHealth;
    protected bool isDead = false;
    protected bool isStunned = false;
    protected bool isAttacking = false;
    protected float lastAttackTime;
    protected float stunTimer;

    [Header("Animation Parameters")]
    protected readonly int animIDSpeed = Animator.StringToHash("Speed");
    protected readonly int animIDAttack = Animator.StringToHash("Attack");
    protected readonly int animIDTakeDamage = Animator.StringToHash("TakeDamage");
    protected readonly int animIDDie = Animator.StringToHash("Die");

    protected virtual void Start()
    {
        InitializeComponents();
        InitializeStats();
        FindPlayer();
    }

    protected virtual void Update()
    {
        if (isDead) return;

        HandleStun();
        UpdateBehavior();
    }

    protected virtual void InitializeComponents()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    protected virtual void InitializeStats()
    {
        currentHealth = maxHealth;
    }

    protected virtual void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    protected virtual void HandleStun()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
        }
    }

    protected abstract void UpdateBehavior();

    public virtual void TakeDamage(int damage)
    {
        if (isDead) return;

        // 방어력 적용
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHealth -= actualDamage;

        Debug.Log($"{gameObject.name} took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");

        // 피격 애니메이션
        if (animator != null)
        {
            animator.SetTrigger(animIDTakeDamage);
        }

        // 스턴 적용
        ApplyStun();

        // 체력 체크
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void ApplyStun()
    {
        isStunned = true;
        stunTimer = stunDuration;
    }

    protected virtual void Die()
    {
        isDead = true;
        
        if (animator != null)
        {
            animator.SetTrigger(animIDDie);
        }

        // 콜라이더 비활성화
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Debug.Log($"{gameObject.name} died!");
        
        // 사망 처리
        OnDeath();
    }

    protected virtual void OnDeath()
    {
        // 하위 클래스에서 오버라이드하여 특별한 사망 처리 구현
        // 예: 아이템 드롭, 경험치 지급 등
    }

    protected virtual bool CanAttack()
    {
        return !isDead && !isStunned && !isAttacking && 
               Time.time >= lastAttackTime + attackCooldown;
    }

    protected virtual void StartAttack()
    {
        if (!CanAttack()) return;

        isAttacking = true;
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger(animIDAttack);
        }
    }

    // 애니메이션 이벤트에서 호출
    public virtual void OnAttackHit()
    {
        // 플레이어가 공격 범위 내에 있는지 확인
        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            // 플레이어 피격 처리
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(attackDamage);
            }
        }
    }

    // 애니메이션 이벤트에서 호출
    public virtual void OnAttackEnd()
    {
        isAttacking = false;
    }

    protected virtual float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }

    protected virtual void MoveTowards(Vector3 targetPosition)
    {
        if (characterController == null || isStunned || isAttacking) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f; // Y축 이동 제거

        // 이동
        characterController.Move(direction * moveSpeed * Time.deltaTime);

        // 회전
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // 애니메이션
        if (animator != null)
        {
            animator.SetFloat(animIDSpeed, direction.magnitude);
        }

        // 중력 적용
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    protected virtual void StopMovement()
    {
        if (animator != null)
        {
            animator.SetFloat(animIDSpeed, 0f);
        }
    }

    // 게터 메서드들
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public bool IsDead() => isDead;
    public bool IsStunned() => isStunned;
    public bool IsAttacking() => isAttacking;
}