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
        
        Bubble hitBubble = collision.gameObject.GetComponent<Bubble>();
        if (hitBubble == null) hitBubble = collision.gameObject.GetComponentInParent<Bubble>();
        
        bool hitGridBubble = (hitBubble != null && hitBubble.isAttached);
        bool hitBubbleTag = collision.gameObject.CompareTag("Bubble");
        
        if (hitGridBubble || hitBubbleTag)
        {
            hasCollided = true;
            Log($"Projectile ({myBubble.type}) hit, attaching to grid");
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
            Vector2Int attachedPos = grid.AttachBubble(myBubble, transform.position);
            grid.CheckAndDestroyMatches(attachedPos);
        }
        
        Destroy(this);
    }
    
    void Update()
    {
        if (Camera.main.WorldToViewportPoint(transform.position).y > 2f)
            Destroy(gameObject);
    }
}