using UnityEngine;
using System.Collections;
using Chromatic.Combat;
using UnityEngine.InputSystem.XR;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class GravityTrap : MonoBehaviour, IInteractiveTarget
{
    [SerializeField]
    private int maxHitNumber = 3;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isReacting = false;
    private int hitNumber = 0;

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
        
        sr.color = Color.white;
    }

    public void OnHit(float damage)
    {
        if (isReacting) return;
        hitNumber++;
        ChangeColor();
        if (hitNumber >= maxHitNumber)
        {
            StartCoroutine(ProcessGravityReaction());  
        }
    }

    private IEnumerator ProcessGravityReaction()
    {
        isReacting = true;

        sr.color = Color.black;
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f; 

        yield return new WaitForSeconds(5f);

        rb.bodyType = RigidbodyType2D.Kinematic;

        rb.linearVelocity = Vector2.zero; 
        rb.angularVelocity = 0f;

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        sr.color = Color.white;

        isReacting = false;
    }

    private void ChangeColor()
    {

    }
}