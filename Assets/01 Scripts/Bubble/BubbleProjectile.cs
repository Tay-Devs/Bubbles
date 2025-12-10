using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    public bool enableDebugLogs = false;
    
    private Rigidbody2D rb;
    private bool hasCollided = false;
    private HexGrid grid;
    private Bubble myBubble;
    
    void Awake()
    {
        myBubble = GetComponent<Bubble>();
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.isKinematic = true;
        
        grid = FindFirstObjectByType<HexGrid>();
        if (grid != null) enableDebugLogs = grid.enableDebugLogs;
    }
    
    void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    
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
        if (hasCollided) return;
        
        // Check if hit ceiling
        if (collision.gameObject.CompareTag("Ceiling"))
        {
            hasCollided = true;
            Log($"Projectile ({myBubble.type}) hit ceiling, attaching to grid");
            AttachToGrid();
            return;
        }
        
        // Check if hit grid bubble
        Bubble hitBubble = collision.gameObject.GetComponent<Bubble>();
        if (hitBubble == null) hitBubble = collision.gameObject.GetComponentInParent<Bubble>();
        
        bool hitGridBubble = (hitBubble != null && hitBubble.isAttached);
        bool hitBubbleTag = collision.gameObject.CompareTag("Bubble");
        
        if (hitGridBubble || hitBubbleTag)
        {
            hasCollided = true;
            Log($"Projectile ({myBubble.type}) hit bubble, attaching to grid");
            AttachToGrid();
        }
    }
    
    void AttachToGrid()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        if (grid != null && myBubble != null)
        {
            // Use the new attacher system
            grid.BubbleAttacher.AttachAndCheckMatches(myBubble, transform.position);
        }
        
        Destroy(this);
    }
}