using UnityEngine;

namespace Chromatic.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 3f; 
        public float Damage { get; private set; } = 10f;

        public Color ProjectileColor { get; private set; }
        
        private Rigidbody2D rb;
        private SpriteRenderer sr;
        
        // 1. 新增一个开关，默认是关着的
        private bool isInitialized = false; 

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
        }

        public void Initialize(Color color, Vector2 direction)
        {
            ProjectileColor = color;
            if (sr != null) sr.color = color;
            rb.linearVelocity = direction * speed; 
            rb.gravityScale = 0f; 

            // 2. 初始化做完了，打开开关，允许撞击
            isInitialized = true; 
            
        }

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D hitInfo)
        {
            // 3. 如果还没初始化（开关没开），直接忽略这次撞击！
            if (!isInitialized) return;

            if (hitInfo.CompareTag("Player")) return;
            if (hitInfo.CompareTag("Bullet")) return;

            IInteractiveTarget target = hitInfo.GetComponent<IInteractiveTarget>();
            if (target != null)
            {
                target.OnHit(Damage, ProjectileColor);
            }

            EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage); 
            }

            if(!hitInfo.CompareTag("NotInteractable"))
            {
                Destroy(gameObject);
            }
        }
    }
}