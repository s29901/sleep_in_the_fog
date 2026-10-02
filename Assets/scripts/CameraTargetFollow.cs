using UnityEngine;

public class CameraTargetFollow : MonoBehaviour
{
    public Transform target;        // игрок
    public BoxCollider2D bounds;    // границы уровня (CameraBounds)

    private Camera cam;
    private float halfHeight;
    private float halfWidth;

    private void Start()
    {
        cam = Camera.main;
        UpdateCameraExtents();
    }

    private void LateUpdate()
    {
        if (target == null || bounds == null) return;

        UpdateCameraExtents();

        Bounds b = bounds.bounds;

        float minX = b.min.x + halfWidth;
        float maxX = b.max.x - halfWidth;
        float minY = b.min.y + halfHeight;
        float maxY = b.max.y - halfHeight;

        Vector3 pos = target.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
    }

    private void UpdateCameraExtents()
    {
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * cam.aspect;
    }
}