using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public int damage = 15;
    public float lifetime = 5f;
    public float speed = 10f;

    private void Start()
    {
        // 일정 시간 후 자동 삭제
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어 피격 처리
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            // 투사체 삭제
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            // 지형에 닿으면 삭제
            Destroy(gameObject);
        }
    }
}