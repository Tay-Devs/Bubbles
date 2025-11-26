using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool hasCollided = false;
    private HexGrid grid;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.isKinematic = true;
        
        grid = FindFirstObjectByType<HexGrid>();
    }
    
    public void Fire(Vector2 direction, float speed)
    {
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = direction.normalized * speed;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble") && !hasCollided)
        {
            hasCollided = true;
            AttachToGrid();
        }
    }
    
    void AttachToGrid()
    {
        // Stop movement
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        // Attach to grid at current position
        if (grid != null)
        {
            Bubble bubble = GetComponent<Bubble>();
            if (bubble != null)
            {
                grid.AttachBubble(bubble, transform.position);
            }
        }
        
        // Remove this component
        Destroy(this);
    }
    
    void Update()
    {
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPos.y > 2f)
        {
            Destroy(gameObject);
        }
    }
}