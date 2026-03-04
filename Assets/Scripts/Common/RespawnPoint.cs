using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class RespawnPoint : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool startsActive = false;
    [SerializeField] private Color activeColor = Color.white;

    [Header("Auto activation")]
    [SerializeField] private bool activateWhenPlayerTouches = true;
    public bool IsActive { get; private set; }
    private SpriteRenderer sr;
    private ParticleSystem ps;
    public Vector3 SpawnPosition
    {
        get
        {
            return transform.position;
        }
    }

    private void Awake()
    {
        IsActive = startsActive;
        sr = GetComponent<SpriteRenderer>();
        ps = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (RespawnManager.Instance != null)
            RespawnManager.Instance.Unregister(this);
    }

    public void Activate()
    {
        IsActive = true;
        if (sr) sr.color = activeColor;
        if (ps) ps.Play();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateWhenPlayerTouches) return;
        if (!other.CompareTag("Player")) return;

        Activate();
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
}
