using UnityEngine;
using Chromatic.Combat;

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
        }

        private void Start()
        {
            rb.linearVelocity = transform.right * speed;
            rb.gravityScale = 0f; 

            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D hitInfo)
        {
            
            if (hitInfo.CompareTag("Player")) return;
            IInteractiveTarget target = hitInfo.GetComponent<IInteractiveTarget>();
            if (target != null)
            {
                target.OnHit(Damage);
            }

            EnemyHealth enemy = hitInfo.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage); 
            }

            if(!(hitInfo.CompareTag("NotInteractable")))
                Destroy(gameObject);
        }
    }
}