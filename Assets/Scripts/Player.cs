using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Player Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float sprintMultiplier = 1.5f;

    [Header("Health & Stamina System")]
    public float maxHealth = 100f;
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 1f; // 스태미너 사용 후 회복 시작까지의 딜레이
    public float sprintStaminaCost = 15f; // 초당 스태미너 소모량
    public int rollStaminaCost = 25; // 구르기당 스태미너 소모량 (int로 변경)

    [Header("Roll Settings")]
    public float rollDistance = 3f;
    public float rollDuration = 0.5f;
    public float rollCooldown = 1f;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;
    public float attackTimer = 0f;
    public float attackStaminaCost = 20f;
    private bool canAttack = true;
    private bool isAttacking = false;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Components")]
    private CharacterController characterController;
    private Animator animator;

    [Header("Input")]
    private Vector2 inputVector;
    private Vector3 moveDirection;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction rollAction;
    private InputAction attackAction;
    private bool isSprinting = false;

    [Header("Roll State")]
    private bool isRolling = false;
    private float rollTimer = 0f;
    private float rollCooldownTimer = 0f;
    private Vector3 rollDirection;

    [Header("Health & Stamina State")]
    private float currentHealth;
    private float currentStamina;
    private float lastStaminaUseTime;
    private bool canUseStamina = true;

    [Header("Animation Parameters")]
    private readonly int animIDSpeed = Animator.StringToHash("Speed");
    private readonly int animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    private readonly int animIDInputX = Animator.StringToHash("InputX");
    private readonly int animIDInputY = Animator.StringToHash("InputY");
    private readonly int animIDIsSprinting = Animator.StringToHash("IsSprinting");
    private readonly int animIDIsRolling = Animator.StringToHash("IsRolling");

    private readonly int animIDIsAttacking = Animator.StringToHash("Attack");

    [Header("Camera-Relative Movement")]
    private Vector3 lastMoveDirection;

    [Header("Equipment")]
    public GameObject weaponModel;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        // 체력/스태미너 초기화
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        lastStaminaUseTime = 0f;

        // 캐릭터를 땅에 정확히 위치시키기
        if (characterController != null)
        {
            // CharacterController의 높이의 절반만큼 위로 올려서 발이 땅에 닿게 함
            float groundOffset = characterController.height * 0.5f;
            transform.position = new Vector3(transform.position.x, groundOffset, transform.position.z);
        }

        if (playerInput == null)
        {
            Debug.LogError("PlayerInput component is missing on " + gameObject.name);
        }
        else
        {
            moveAction = playerInput.actions["Move"];
            sprintAction = playerInput.actions["Sprint"];
            rollAction = playerInput.actions["Roll"];
            attackAction = playerInput.actions["Attack"];
        }
    }

    void Update()
    {
        HandleInput();
        HandleStamina(); // 추가
        HandleRoll();
        HandleMovement();
        HandleAnimation();
        HandleAttack();
    }

    void HandleStamina()
    {
        // 스태미너 소모 체크
        bool isUsingStamina = false;

        // 달리기 스태미너 소모
        if (isSprinting && inputVector.magnitude > 0.1f && !isRolling)
        {
            float staminaCost = sprintStaminaCost * Time.deltaTime;
            if (currentStamina >= staminaCost)
            {
                currentStamina -= staminaCost;
                lastStaminaUseTime = Time.time;
                isUsingStamina = true;
            }
            else
            {
                // 스태미너 부족시 달리기 강제 해제
                isSprinting = false;
                canUseStamina = false;
            }
        }

        // 스태미너 회복
        if (!isUsingStamina && Time.time > lastStaminaUseTime + staminaRegenDelay)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

            // 스태미너가 30% 이상 회복되면 다시 사용 가능
            if (currentStamina >= maxStamina * 0.3f)
            {
                canUseStamina = true;
            }
        }

        // 스태미너 범위 제한
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
    }

    bool CanUseStamina(float requiredStamina)
    {
        return canUseStamina && currentStamina >= requiredStamina;
    }

    // 구르기용 오버로드: 스태미너가 1 이상이면 사용 가능
    bool CanUseStaminaForRoll(int requiredStamina)
    {
        return currentStamina >= 1f; // 스태미너가 1 이상이면 구르기 가능
    }

    void UseStamina(float amount)
    {
        currentStamina -= amount;
        lastStaminaUseTime = Time.time;

        if (currentStamina <= 0f)
        {
            canUseStamina = false;
        }
    }

    // 즉시 스태미너 사용 (int 버전)
    void UseStaminaInstant(int amount)
    {
        float previousStamina = currentStamina;
        currentStamina = Mathf.Max(0f, currentStamina - amount);
        lastStaminaUseTime = Time.time;

        if (currentStamina <= 0f)
        {
            canUseStamina = false;
        }

        float actualUsed = previousStamina - currentStamina;
        Debug.Log($"Stamina used: {actualUsed:F0}. Remaining: {currentStamina:F0}/{maxStamina:F0}");
    }

    // 즉시 스태미너 회복 (int 버전)
    public void RestoreStaminaInstant(int amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        Debug.Log($"Stamina restored: {amount}. Current: {currentStamina:F0}/{maxStamina:F0}");
    }


    void HandleInput()
    {
        if (isAttacking) return;

        if (moveAction != null)
        {
            inputVector = moveAction.ReadValue<Vector2>();
        }

        // Sprint 입력 처리
        if (sprintAction != null)
        {
            bool sprintInput = sprintAction.IsPressed();

            // 스태미너가 있고 이동 중일 때만 달리기 허용
            if (sprintInput && inputVector.magnitude > 0.1f && CanUseStamina(sprintStaminaCost * Time.deltaTime))
            {
                isSprinting = true;
            }
            else
            {
                isSprinting = false;
            }
        }

        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }

        // Roll 쿨다운 업데이트
        if (rollCooldownTimer > 0f)
        {
            rollCooldownTimer -= Time.deltaTime;
        }
    }

    void HandleAttack()
    {
        if (attackAction != null && attackAction.WasPressedThisFrame() && canAttack && currentStamina >= attackStaminaCost)
        {
            ShowWeapon();
            animator.SetTrigger(animIDIsAttacking);
            currentStamina -= attackStaminaCost;
            canAttack = false;
            attackTimer = 0f;
            isAttacking = true;
        }
        if (!canAttack)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                canAttack = true;
                attackTimer = 0f;
            }

        }
    }
    public void EndAttack()
    {
        isAttacking = false;
        HideWeapon();
        print("Attack ended");
    }
    void HandleRoll()
    {
        // Roll 입력 처리 (스태미너가 1 이상이면 구르기 가능)
        if (rollAction != null && rollAction.WasPressedThisFrame() && !isRolling && rollCooldownTimer <= 0f && CanUseStaminaForRoll(rollStaminaCost))
        {
            StartRoll();
        }

        // Roll 진행 중 처리
        if (isRolling)
        {
            rollTimer -= Time.deltaTime;

            // Roll 이동 처리
            float rollSpeed = rollDistance / rollDuration;
            Vector3 horizontalRollMovement = new Vector3(rollDirection.x, 0f, rollDirection.z) * rollSpeed * Time.deltaTime;
            characterController.Move(horizontalRollMovement);

            // Roll 종료 체크
            if (rollTimer <= 0f)
            {
                EndRoll();
            }
        }
    }

    void StartRoll()
    {
        // 스태미너 체크 (1 이상이면 구르기 가능)
        if (!CanUseStaminaForRoll(rollStaminaCost))
        {
            Debug.Log("Not enough stamina to roll!");
            return;
        }
        HideWeapon();
        // 스태미너 소모 (int 버전 사용)
        UseStaminaInstant(rollStaminaCost);

        isRolling = true;
        rollTimer = rollDuration;
        rollCooldownTimer = rollCooldown;

        // Roll 방향 결정
        if (inputVector.magnitude > 0.1f)
        {
            // 입력이 있을 때: 입력 방향으로 구르기 (카메라 기준)
            Vector3 cameraForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 cameraRight = cameraTransform != null ? cameraTransform.right : Vector3.right;

            cameraForward.y = 0f;
            cameraForward.Normalize();
            cameraRight.y = 0f;
            cameraRight.Normalize();

            rollDirection = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;
        }
        else
        {
            // 입력이 없을 때: 정면으로 구르기 (캐릭터가 바라보는 방향)
            rollDirection = transform.forward;
        }

        // Roll 중에는 캐릭터를 Roll 방향으로 회전
        if (rollDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(rollDirection);
        }

    }

    void EndRoll()
    {
        isRolling = false;
        rollTimer = 0f;
    }

    void HandleMovement()
    {
        if (characterController == null) return;

        // Roll 중에는 일반 이동 제한
        if (isRolling)
        {
            if (!characterController.isGrounded)
            {
                characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
            }
            return;
        }

        // 카메라 기준으로 방향 계산 (엘든링 스타일)
        Vector3 cameraForward = Vector3.zero;
        Vector3 cameraRight = Vector3.zero;

        if (cameraTransform != null)
        {
            // 카메라의 forward와 right 벡터를 사용하되, Y축은 제거 (수평 이동만)
            cameraForward = cameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            cameraRight = cameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();
        }
        else
        {
            // 카메라가 없다면 월드 좌표계 사용
            cameraForward = Vector3.forward;
            cameraRight = Vector3.right;
        }

        // 입력에 따른 이동 방향 계산 (카메라 기준)
        Vector3 inputDirection = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;

        // 현재 이동 속도 계산 (Sprint 고려)
        float currentSpeed = moveSpeed;
        if (isSprinting && inputVector.magnitude > 0.1f)
        {
            currentSpeed *= sprintMultiplier;
        }

        // 캐릭터 이동
        if (inputDirection.magnitude > 0.1f)
        {
            moveDirection = inputDirection;
            lastMoveDirection = moveDirection;

            // 다크소울/엘든링 스타일 회전 처리
            if (isSprinting)
            {
                // 달리기 중: 항상 이동 방향으로 회전
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                // 걷기 중: 앞으로 이동하거나 좌우 이동시 회전
                bool shouldRotate = false;

                // 앞으로 이동하는 경우 (W키 포함)
                if (inputVector.y > 0.1f)
                {
                    shouldRotate = true;
                }
                // 순수 좌우 이동인 경우 (A, D키만)
                else if (Mathf.Abs(inputVector.x) > 0.1f && Mathf.Abs(inputVector.y) < 0.1f)
                {
                    shouldRotate = true;
                }

                if (shouldRotate)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
                // 뒤로 이동이나 뒤 대각선 이동시에는 회전하지 않음 (백스텝 느낌)
            }

            // 이동 적용
            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // 중력 적용
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        // Roll 중에는 Roll 애니메이션만 처리
        if (isRolling)
        {
            animator.SetBool(animIDIsRolling, true);
            // 다른 파라미터들은 0으로 설정
            animator.SetFloat(animIDSpeed, 0f);
            animator.SetFloat(animIDInputX, 0f);
            animator.SetFloat(animIDInputY, 0f);
            animator.SetBool(animIDIsSprinting, false);
            return;
        }
        else
        {
            animator.SetBool(animIDIsRolling, false);
        }

        // 입력 크기 계산
        float inputMagnitude = inputVector.magnitude;

        // 실제 속도 계산 (Sprint 고려)
        float speed = inputMagnitude;
        if (isSprinting && inputMagnitude > 0.1f)
        {
            speed = inputMagnitude * sprintMultiplier; // Sprint시 속도 증가
        }

        // Sprint 상태 고려한 모션 속도
        float motionSpeed = 1.0f;
        if (isSprinting && inputMagnitude > 0.1f)
        {
            motionSpeed = sprintMultiplier;
        }

        // 애니메이션용 입력값 계산
        float animInputX = 0f;
        float animInputY = 0f;

        if (inputMagnitude > 0.1f)
        {
            if (isSprinting)
            {
                // 달리기: 항상 앞으로 달리는 애니메이션 (엘든링 스타일)
                animInputX = 0f;
                animInputY = 1f; // 항상 앞으로
            }
            else
            {
                // 걷기: 다크소울/엘든링 스타일 애니메이션 처리
                if (cameraTransform != null)
                {
                    Vector3 cameraForward = cameraTransform.forward;
                    cameraForward.y = 0f;
                    cameraForward.Normalize();

                    Vector3 cameraRight = cameraTransform.right;
                    cameraRight.y = 0f;
                    cameraRight.Normalize();

                    // 카메라 기준 이동 방향을 캐릭터 로컬 좌표계로 변환
                    Vector3 worldMoveDirection = (cameraForward * inputVector.y + cameraRight * inputVector.x).normalized;
                    Vector3 localMoveDirection = transform.InverseTransformDirection(worldMoveDirection);

                    // 앞으로 이동하거나 좌우 이동시 (회전하는 경우)
                    if ((inputVector.y > 0.1f) || (Mathf.Abs(inputVector.x) > 0.1f && Mathf.Abs(inputVector.y) < 0.1f))
                    {
                        animInputX = 0f;
                        animInputY = 1f; // 앞으로 걷기 (회전하므로)
                    }
                    else
                    {
                        // 뒤로 이동이나 뒤 대각선 (회전하지 않는 경우)
                        animInputX = localMoveDirection.x;
                        animInputY = localMoveDirection.z;
                    }
                }
            }
        }

        // 값 범위 제한 (안전장치) - Speed는 Sprint 고려해서 더 큰 값 허용
        animInputX = Mathf.Clamp(animInputX, -1f, 1f);
        animInputY = Mathf.Clamp(animInputY, -1f, 1f);
        // speed는 Sprint시 1.0보다 클 수 있음 (sprintMultiplier만큼)

        // 애니메이션 파라미터 설정
        animator.SetFloat(animIDSpeed, speed);
        animator.SetFloat(animIDMotionSpeed, motionSpeed);
        animator.SetFloat(animIDInputX, animInputX);
        animator.SetFloat(animIDInputY, animInputY);
        animator.SetBool(animIDIsSprinting, isSprinting && inputMagnitude > 0.1f);
    }

    public void ShowWeapon()
    {
        if (weaponModel != null)
            weaponModel.SetActive(true);
    }
    public void HideWeapon()
    {
        if (weaponModel != null)
            weaponModel.SetActive(false);
    }


    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"Player took {damage} damage. Health: {currentHealth:F0}/{maxHealth:F0}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Debug.Log($"Player healed {amount}. Health: {currentHealth:F0}/{maxHealth:F0}");
    }

    private void Die()
    {
        Debug.Log("Player died!");
        // 사망 처리 로직 (나중에 구현)
    }

    // 부드러운 바 애니메이션용 (0.0~1.0 비율)
    public float GetCurrentHealth() { return currentHealth; }
    public float GetMaxHealth() { return maxHealth; }
    public float GetHealthPercentage() { return currentHealth / maxHealth; }

    public float GetCurrentStamina() { return currentStamina; }
    public float GetMaxStamina() { return maxStamina; }
    public float GetStaminaPercentage() { return currentStamina / maxStamina; }

    // 정확한 숫자 표시용 (UI에서 "75/100" 같은 표시)
    public int GetCurrentHealthInt() { return Mathf.RoundToInt(currentHealth); }
    public int GetMaxHealthInt() { return Mathf.RoundToInt(maxHealth); }
    public int GetCurrentStaminaInt() { return Mathf.RoundToInt(currentStamina); }
    public int GetMaxStaminaInt() { return Mathf.RoundToInt(maxStamina); }
}

