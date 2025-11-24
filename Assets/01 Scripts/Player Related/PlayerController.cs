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
    
    void Start()
    {
        SpawnNewBubble();
    }
    
    void Update()
    {
        UpdateAimRotation();
        
        if (Input.GetMouseButtonDown(0))
            Shoot();
    }
    
    void UpdateAimRotation()
    {
        if (aimArrow == null) return;
        
        // Get angle from player to mouse
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 direction = (mouseWorld - transform.position).normalized;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, minRotationAngle, maxRotationAngle);
        
        // Position and rotate arrow
        float rad = angle * Mathf.Deg2Rad;
        aimArrow.transform.position = transform.position + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * arrowDistance;
        aimArrow.transform.rotation = Quaternion.Euler(0, 0, -angle);
    }
    
    void Shoot()
    {
        if (currentBubble == null) return;
        
        Vector3 shootDirection = (aimArrow.transform.position - transform.position).normalized;
        Debug.Log($"Shooting in direction: {shootDirection}");
        
        SpawnNewBubble();
    }
    
    void SpawnNewBubble()
    {
        if (currentBubble != null)
            Destroy(currentBubble);
            
        if (bubblePrefab != null)
        {
            currentBubble = Instantiate(bubblePrefab, transform.position, Quaternion.identity);
            
            // Disable collision for preview
            Collider2D col = currentBubble.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            
            // Random color
            Bubble bubble = currentBubble.GetComponent<Bubble>();
            if (bubble != null)
                bubble.SetType((BubbleType)Random.Range(0, System.Enum.GetValues(typeof(BubbleType)).Length));
        }
    }
}