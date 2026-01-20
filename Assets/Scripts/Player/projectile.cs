using UnityEngine;

namespace Chromatic.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifeTime = 3f; 

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            rb.linearVelocity = transform.right * speed;
            
            rb.gravityScale = 0f; 

            Destroy(gameObject, lifeTime);
        }

        private void OnTriggerEnter2D(Collider2D hitInfo)
        {
            Debug.Log("Hit: " + hitInfo.name);
            
            Destroy(gameObject);
        }
    }
}