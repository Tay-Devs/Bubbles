using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Aiming")]
    public GameObject aimArrow;
    public float minRotationAngle = -75f;
    public float maxRotationAngle = 75f;
    public float arrowDistance = 1.5f;
    
    [Header("Bubble")]
    public GameObject bubblePrefab;
    private GameObject currentBubble;
    public float shootSpeed = 15f; // Increased default speed for better feel
    
    void Start()
    {
        SpawnNewBubble();
    }
    
    void Update()
    {
        UpdateAimRotation();


        if (ShouldShoot())
        {
            Shoot();   
        }

    }
    bool ShouldShoot()
    {
        //Touch device logic
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                return true;
        }

        // Editor mouse, BUT only if no touches detected
#if UNITY_EDITOR
        if (Input.mousePresent && Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            return true;
#endif

        return false;
    }
    void UpdateAimRotation()
    {
        if (aimArrow == null) return;

        // Ignore if pointer is outside valid screen area
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width ||
            Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f; // Just to be safe for 2D

        Vector3 direction = (mouseWorld - transform.position).normalized;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, minRotationAngle, maxRotationAngle);

        float rad = angle * Mathf.Deg2Rad;
        aimArrow.transform.position = transform.position + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * arrowDistance;
        aimArrow.transform.rotation = Quaternion.Euler(0, 0, -angle);
    }
    
    void Shoot()
    {
        if (currentBubble == null) return;
        
        Vector3 shootDirection = (aimArrow.transform.position - transform.position).normalized;
        
        // Enable physics components
        Collider2D col = currentBubble.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true; // Enable physics simulation
        
        // Add projectile component and fire
        BubbleProjectile projectile = currentBubble.AddComponent<BubbleProjectile>();
        projectile.Fire(shootDirection, shootSpeed);
        
        // Remove from player
        currentBubble.transform.SetParent(null);
        currentBubble = null;
        
        // Spawn next bubble
        SpawnNewBubble();
    }
    
    void SpawnNewBubble()
    {
        if (currentBubble != null)
            Destroy(currentBubble);
            
        if (bubblePrefab != null)
        {
            currentBubble = Instantiate(bubblePrefab, transform.position, Quaternion.identity);
            
            // Ensure bubble has required components
            Collider2D col = currentBubble.GetComponent<Collider2D>();
            if (col == null) col = currentBubble.AddComponent<CircleCollider2D>();
            col.enabled = false; // Disable for preview
            
            Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
            if (rb == null) 
            {
                rb = currentBubble.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
            }
            rb.simulated = false; // Disable physics for preview
            
            // Random color
            Bubble bubble = currentBubble.GetComponent<Bubble>();
            if (bubble != null)
                bubble.SetType((BubbleType)Random.Range(0, System.Enum.GetValues(typeof(BubbleType)).Length));
        }
    }
}