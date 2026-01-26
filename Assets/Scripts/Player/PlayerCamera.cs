using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;

    public Vector2 offset = Vector2.zero;
    public float smoothTime = 0.15f;
    public bool followX = true;
    public bool followY = true;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (!target)
            target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector3 currentPos = transform.position;

        float targetX = followX ? target.position.x + offset.x : currentPos.x;
        float targetY = followY ? target.position.y + offset.y : currentPos.y;

        Vector3 targetPos = new Vector3(targetX, targetY, currentPos.z);

        transform.position = Vector3.SmoothDamp(
            currentPos,
            targetPos,
            ref velocity,
            smoothTime
        );
    }
}
