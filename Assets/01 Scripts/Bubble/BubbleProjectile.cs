using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    public bool enableDebugLogs = false;
    
    private Rigidbody2D rb;
    private bool hasCollided = false;
    private HexGrid grid;
    private Bubble myBubble;
    private PlayerController playerController;
    
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
        playerController = FindFirstObjectByType<PlayerController>();
        
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
        
        if (collision.gameObject.CompareTag("Ceiling"))
        {
            hasCollided = true;
            Log($"Projectile ({myBubble.type}) hit ceiling, attaching to grid");
            AttachToGrid();
            return;
        }
        
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
    
    // Stops the bubble, attaches to grid, and notifies PlayerController.
    void AttachToGrid()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }
        
        if (grid != null && myBubble != null)
        {
            grid.BubbleAttacher.AttachAndCheckMatches(myBubble, transform.position);
        }
        
        // Notify PlayerController that bubble has connected
        if (playerController != null)
        {
            playerController.OnBubbleConnectedToGrid();
        }
        
        Destroy(this);
    }
}