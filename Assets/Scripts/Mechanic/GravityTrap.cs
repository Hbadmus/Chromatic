using UnityEngine;
using System.Collections;
using Chromatic.Combat; 

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class GravityTrap : MonoBehaviour, IInteractiveTarget
{
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isReacting = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        sr.color = Color.black; 
    }

    public void OnHit(float damage)
    {
        if (isReacting) return;
        StartCoroutine(ProcessGravityReaction());
    }

    private IEnumerator ProcessGravityReaction()
    {
        isReacting = true;

        sr.color = Color.white;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f; 

        yield return new WaitForSeconds(5f);

        rb.bodyType = RigidbodyType2D.Kinematic;

        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        sr.color = Color.black;

        isReacting = false;
    }
}