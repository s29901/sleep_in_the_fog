using UnityEngine;

public class Player : MonoBehaviour
{
   
    public float moveSpeed = 5f;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    private Rigidbody2D rb;
    private Vector2 input;

    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
     
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");   

        input = new Vector2(h, v).normalized;
    }

    private void FixedUpdate()
    {
        Vector2 newPos = rb.position + input * moveSpeed * Time.fixedDeltaTime;
        
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        rb.MovePosition(newPos);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
       
        Vector2 center = (minBounds + maxBounds) / 2f;
        Vector2 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}