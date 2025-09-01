using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target; // 플레이어 Transform
    public float distance = 8f; // 플레이어와의 거리 (엘든링 스타일)
    public float height = 1.5f; // 플레이어 위의 높이
    public float mouseSensitivity = 3f; // 마우스 감도
    public float smoothTime = 0.2f; // 카메라 이동 부드러움
    
    [Header("Camera Limits")]
    public float minVerticalAngle = -30f; // 최소 수직 각도
    public float maxVerticalAngle = 70f; // 최대 수직 각도
    
    [Header("Elden Ring Style Settings")]
    public float followSpeed = 10f; // 플레이어 추적 속도
    public float rotationDamping = 5f; // 회전 댐핑
    public LayerMask obstacleLayerMask = -1; // 장애물 레이어
    public float cameraRadius = 0.3f; // 카메라 충돌 반지름
    
    [Header("Input")]
    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction escapeAction;
    
    [Header("Private Variables")]
    private float currentX = 0f; // 현재 수평 회전 각도
    private float currentY = 0f; // 현재 수직 회전 각도
    private Vector3 velocity = Vector3.zero; // 카메라 이동 속도 (SmoothDamp용)
    private float currentDistance; // 현재 카메라 거리 (충돌 감지용)
    
    void Start()
    {
        // PlayerInput 컴포넌트 찾기
        playerInput = FindFirstObjectByType<PlayerInput>();
        
        if (playerInput != null)
        {
            // Look 액션 찾기
            try
            {
                lookAction = playerInput.actions["Look"];
            }
            catch
            {
                Debug.LogWarning("Look action not found in Input Actions. Mouse look will not work.");
            }
            
            // Escape 액션 찾기 (없다면 null로 유지)
            try
            {
                escapeAction = playerInput.actions["Escape"];
            }
            catch
            {
                Debug.LogWarning("Escape action not found in Input Actions. ESC key will not work for cursor toggle.");
            }
        }
        
        // 마우스 커서 잠금 (엘든링 스타일)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 초기 설정
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            
            // 플레이어 뒤쪽에서 시작
            currentX = target.eulerAngles.y;
        }
        
        currentDistance = distance;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseInput();
        UpdateCameraPosition();
    }
    
    void HandleMouseInput()
    {
        Vector2 lookInput = Vector2.zero;
        
        // 마우스 커서가 잠금 상태일 때만 카메라 회전
        if (Cursor.lockState == CursorLockMode.Locked && lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }
        
        // 마우스 입력에 따른 회전 계산 (엘든링 스타일 - 더 민감하게)
        currentX += lookInput.x * mouseSensitivity;
        currentY -= lookInput.y * mouseSensitivity;
        
        // 수직 각도 제한
        currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
    }
    
    void UpdateCameraPosition()
    {
        if (target == null) return;
        
        // 엘든링 스타일 카메라 위치 계산
        Vector3 targetPosition = target.position + Vector3.up * height;
        
        // 회전 계산
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        
        // 원하는 카메라 위치 (플레이어 뒤쪽)
        Vector3 desiredPosition = targetPosition - (rotation * Vector3.forward * distance);
        
        // 장애물 충돌 검사 (엘든링처럼 벽 뒤로 가지 않게)
        CheckForObstacles(targetPosition, desiredPosition, rotation);
        
        // 최종 위치로 부드럽게 이동
        Vector3 finalPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        transform.position = Vector3.SmoothDamp(transform.position, finalPosition, ref velocity, smoothTime);
        
        // 카메라가 플레이어를 바라보도록 설정
        transform.LookAt(targetPosition);
    }
    
    void CheckForObstacles(Vector3 targetPosition, Vector3 desiredPosition, Quaternion rotation)
    {
        // 플레이어에서 원하는 카메라 위치까지 레이캐스트
        Vector3 direction = desiredPosition - targetPosition;
        float desiredDistance = direction.magnitude;
        
        RaycastHit hit;
        if (Physics.SphereCast(targetPosition, cameraRadius, direction.normalized, out hit, desiredDistance, obstacleLayerMask))
        {
            // 장애물이 있으면 거리 조정
            currentDistance = Mathf.Lerp(currentDistance, hit.distance - cameraRadius, Time.deltaTime * followSpeed);
            currentDistance = Mathf.Clamp(currentDistance, 1f, distance);
        }
        else
        {
            // 장애물이 없으면 원래 거리로 복귀
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * followSpeed);
        }
    }
    
    void Update()
    {
        // ESC키로 마우스 커서 토글 (New Input System)
        if (escapeAction != null && escapeAction.WasPressedThisFrame())
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
