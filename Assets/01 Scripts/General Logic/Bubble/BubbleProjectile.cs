using UnityEngine;

public class BubbleProjectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool hasCollided = false;
    private HexGrid grid;
    private Bubble myBubble;
    
    void Awake()
    {
        myBubble = GetComponent<Bubble>();
        
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
        if (hasCollided) return;
        
        Debug.Log($"Projectile collided with: {collision.gameObject.name}");
        
        // Try to find Bubble component on the hit object or its parent
        Bubble hitBubble = collision.gameObject.GetComponent<Bubble>();
        if (hitBubble == null)
            hitBubble = collision.gameObject.GetComponentInParent<Bubble>();
        
        // Attach if we hit a grid bubble (isAttached == true) OR if it has the Bubble tag
        bool hitGridBubble = (hitBubble != null && hitBubble.isAttached);
        bool hitBubbleTag = collision.gameObject.CompareTag("Bubble");
        
        Debug.Log($"hitBubble: {hitBubble != null}, isAttached: {(hitBubble != null ? hitBubble.isAttached : false)}, hasTag: {hitBubbleTag}");
        
        if (hitGridBubble || hitBubbleTag)
        {
            hasCollided = true;
            Debug.Log($"Projectile ({myBubble.type}) collided, attaching to grid");
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
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPos.y > 2f)
        {
            Destroy(gameObject);
        }
    }
}