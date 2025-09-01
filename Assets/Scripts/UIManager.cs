using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Health UI")]
    public Slider healthBar;
    public Image healthFillImage;
    public Color healthColor = Color.red;
    public Color healthLowColor = new Color(0.5f, 0f, 0f); // 어두운 빨간색
    public float healthLowThreshold = 0.25f; // 25% 이하일 때 색상 변경

    [Header("Stamina UI")]
    public Slider staminaBar;
    public Image staminaFillImage;
    public Color staminaColor = Color.yellow;
    public Color staminaEmptyColor = Color.gray;
    public float staminaEmptyThreshold = 0.1f; // 10% 이하일 때 색상 변경

    [Header("Animation Settings")]
    public float barAnimationSpeed = 2f; // 바 애니메이션 속도
    public bool smoothAnimation = true; // 부드러운 애니메이션 사용 여부

    [Header("Player Reference")]
    public Player player; // Player 스크립트 참조

    // 내부 변수들
    private float targetHealthPercentage;
    private float targetStaminaPercentage;
    private float currentHealthPercentage;
    private float currentStaminaPercentage;

    void Start()
    {
        // Player 자동 찾기
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                Debug.LogError("Player not found! Please assign Player reference in UIManager.");
                return;
            }
        }

        // UI 요소들 유효성 검사
        ValidateUIElements();

        // 초기값 설정
        InitializeUI();
    }

    void Update()
    {
        if (player == null) return;

        UpdateHealthUI();
        UpdateStaminaUI();
    }

    void ValidateUIElements()
    {
        if (healthBar == null)
            Debug.LogWarning("Health Bar is not assigned in UIManager!");
        
        if (staminaBar == null)
            Debug.LogWarning("Stamina Bar is not assigned in UIManager!");

        // Fill Image 자동 할당
        if (healthBar != null && healthFillImage == null)
            healthFillImage = healthBar.fillRect.GetComponent<Image>();
        
        if (staminaBar != null && staminaFillImage == null)
            staminaFillImage = staminaBar.fillRect.GetComponent<Image>();
    }

    void InitializeUI()
    {
        // 초기 퍼센티지 설정
        targetHealthPercentage = player.GetHealthPercentage();
        targetStaminaPercentage = player.GetStaminaPercentage();
        currentHealthPercentage = targetHealthPercentage;
        currentStaminaPercentage = targetStaminaPercentage;

        // 바 초기값 설정
        if (healthBar != null)
            healthBar.value = currentHealthPercentage;
        
        if (staminaBar != null)
            staminaBar.value = currentStaminaPercentage;

        // 색상 초기 설정
        UpdateHealthColor();
        UpdateStaminaColor();
    }

    void UpdateHealthUI()
    {
        targetHealthPercentage = player.GetHealthPercentage();

        // 부드러운 애니메이션 또는 즉시 업데이트
        if (smoothAnimation)
        {
            currentHealthPercentage = Mathf.Lerp(currentHealthPercentage, targetHealthPercentage, barAnimationSpeed * Time.deltaTime);
        }
        else
        {
            currentHealthPercentage = targetHealthPercentage;
        }

        // 바 업데이트
        if (healthBar != null)
            healthBar.value = currentHealthPercentage;

        // 색상 업데이트
        UpdateHealthColor();
    }

    void UpdateStaminaUI()
    {
        targetStaminaPercentage = player.GetStaminaPercentage();

        // 부드러운 애니메이션 또는 즉시 업데이트
        if (smoothAnimation)
        {
            currentStaminaPercentage = Mathf.Lerp(currentStaminaPercentage, targetStaminaPercentage, barAnimationSpeed * Time.deltaTime);
        }
        else
        {
            currentStaminaPercentage = targetStaminaPercentage;
        }

        // 바 업데이트
        if (staminaBar != null)
            staminaBar.value = currentStaminaPercentage;

        // 색상 업데이트
        UpdateStaminaColor();
    }

    void UpdateHealthColor()
    {
        if (healthFillImage == null) return;

        // 체력이 낮을 때 색상 변경
        if (currentHealthPercentage <= healthLowThreshold)
        {
            healthFillImage.color = healthLowColor;
        }
        else
        {
            healthFillImage.color = healthColor;
        }
    }

    void UpdateStaminaColor()
    {
        if (staminaFillImage == null) return;

        // 스태미너가 낮을 때 색상 변경
        if (currentStaminaPercentage <= staminaEmptyThreshold)
        {
            staminaFillImage.color = staminaEmptyColor;
        }
        else
        {
            staminaFillImage.color = staminaColor;
        }
    }

    // 공개 메서드들 (외부에서 호출 가능)
    
    /// <summary>
    /// 체력 바를 즉시 업데이트 (애니메이션 없이)
    /// </summary>
    public void ForceUpdateHealth()
    {
        targetHealthPercentage = player.GetHealthPercentage();
        currentHealthPercentage = targetHealthPercentage;
        
        if (healthBar != null)
            healthBar.value = currentHealthPercentage;
        
        UpdateHealthColor();
    }

    /// <summary>
    /// 스태미너 바를 즉시 업데이트 (애니메이션 없이)
    /// </summary>
    public void ForceUpdateStamina()
    {
        targetStaminaPercentage = player.GetStaminaPercentage();
        currentStaminaPercentage = targetStaminaPercentage;
        
        if (staminaBar != null)
            staminaBar.value = currentStaminaPercentage;
        
        UpdateStaminaColor();
    }

    /// <summary>
    /// 전체 UI를 즉시 업데이트
    /// </summary>
    public void ForceUpdateAll()
    {
        ForceUpdateHealth();
        ForceUpdateStamina();
    }

    /// <summary>
    /// Player 참조 설정
    /// </summary>
    public void SetPlayer(Player newPlayer)
    {
        player = newPlayer;
        if (player != null)
        {
            InitializeUI();
        }
    }

    /// <summary>
    /// 체력 바 깜빡임 효과 (데미지를 받았을 때 등)
    /// </summary>
    public void FlashHealthBar()
    {
        StartCoroutine(FlashCoroutine(healthFillImage, Color.white, 0.1f));
    }

    /// <summary>
    /// 스태미너 바 깜빡임 효과 (스태미너가 부족할 때 등)
    /// </summary>
    public void FlashStaminaBar()
    {
        StartCoroutine(FlashCoroutine(staminaFillImage, Color.white, 0.1f));
    }

    private System.Collections.IEnumerator FlashCoroutine(Image image, Color flashColor, float duration)
    {
        if (image == null) yield break;

        Color originalColor = image.color;
        image.color = flashColor;
        yield return new WaitForSeconds(duration);
        image.color = originalColor;
    }

    // 디버그용 메서드들
    void OnValidate()
    {
        // Inspector에서 값이 변경될 때마다 호출
        if (Application.isPlaying && player != null)
        {
            UpdateHealthColor();
            UpdateStaminaColor();
        }
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug UI Info")]
    public void DebugUIInfo()
    {
        if (player == null)
        {
            Debug.Log("Player reference is null!");
            return;
        }

        Debug.Log($"=== UI Manager Debug ===");
        Debug.Log($"Health: {player.GetCurrentHealthInt()}/{player.GetMaxHealthInt()} ({player.GetHealthPercentage():P1})");
        Debug.Log($"Stamina: {player.GetCurrentStaminaInt()}/{player.GetMaxStaminaInt()} ({player.GetStaminaPercentage():P1})");
        Debug.Log($"Health Bar: {(healthBar != null ? "OK" : "Missing")}");
        Debug.Log($"Stamina Bar: {(staminaBar != null ? "OK" : "Missing")}");
    }
}
