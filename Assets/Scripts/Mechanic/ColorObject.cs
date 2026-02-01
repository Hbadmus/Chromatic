using UnityEngine;
using System.Collections;
using Chromatic.Combat; 

namespace Chromatic.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ColorObject : MonoBehaviour, IInteractiveTarget, IDrainable
    {
        [Header("Settings")]
        [SerializeField] private int maxHitNumber = 3;
        [SerializeField] private float resetTime = 5f; 
        [SerializeField] private float returnDuration = 3f; 
        
        [Header("Colors")]
        [SerializeField] private Color initialColor = Color.white;
        [SerializeField] private Color finalColor = Color.black;

        private Vector3 originalPosition;
        private Quaternion originalRotation;
        
        private SpriteRenderer sr;
        private Rigidbody2D rb;
        
        private bool isReacting = false; 
        private int hitNumber = 0;
        
        private Coroutine activeCoroutine;

        public bool CanDrain => isReacting;

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
            
            sr.color = initialColor;
        }

        public void OnHit(float damage)
        {

            if (isReacting) return;

            ChangeColor(); 

            if (hitNumber >= maxHitNumber)
            {
                ActivateGravityState();
            }
        }

        public void OnDrain()
        {
            if (!isReacting) return;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(ReturnToOriginRoutine());
        }


        private void ActivateGravityState()
        {
            isReacting = true;
            sr.color = finalColor; 
            
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.mass = 100000f; 

            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(AutoResetRoutine());
        }

        private IEnumerator AutoResetRoutine()
        {
            yield return new WaitForSeconds(resetTime);
            yield return StartCoroutine(ReturnToOriginRoutine());
        }

        private IEnumerator ReturnToOriginRoutine()
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            Color startColor = sr.color;

            float time = 0f;
            while (time < returnDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / returnDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, originalPosition, easedT);
                transform.rotation = Quaternion.Slerp(startRot, originalRotation, easedT);
                sr.color = Color.Lerp(startColor, initialColor, easedT);

                yield return null;
            }

            transform.position = originalPosition;
            transform.rotation = originalRotation;
            
            ResetProgress();
        }

        private void ChangeColor()
        {
            if (maxHitNumber <= 0) return;
            hitNumber++;
            float t = (hitNumber / (float)maxHitNumber) * 0.5f;
            t = Mathf.Clamp01(t);
            sr.color = Color.Lerp(sr.color, finalColor, t);
        }

        private void ResetProgress()
        {
            hitNumber = 0;
            isReacting = false;
            sr.color = initialColor;
            activeCoroutine = null;
        }
    }
}