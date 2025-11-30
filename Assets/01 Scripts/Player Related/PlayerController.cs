using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public float shootSpeed = 15f;
    
    [Header("Cooldown")]
    public float shootCooldown = 0.5f;
    private float lastShootTime = -Mathf.Infinity;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    private HexGrid grid;
    private Camera mainCam;
    private bool wasPressed = false;
    private Vector2 lastValidPointerPos;
    private List<BubbleType> availableColors = new List<BubbleType>();
    
    void Start()
    {
        grid = FindFirstObjectByType<HexGrid>();
        mainCam = Camera.main;
        lastValidPointerPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        // Subscribe to color change events
        if (grid != null)
        {
            grid.onColorsChanged += OnColorsChanged;
            enableDebugLogs = grid.enableDebugLogs;
        }
        
        // Initialize available colors from grid
        UpdateAvailableColors();
        
        SpawnNewBubble();
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (grid != null)
        {
            grid.onColorsChanged -= OnColorsChanged;
        }
    }
    
    void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    
    // Called when bubbles are destroyed and colors may have changed
    void OnColorsChanged()
    {
        UpdateAvailableColors();
        ValidateCurrentBubble();
    }
    
    // Update the list of available colors from the grid
    void UpdateAvailableColors()
    {
        availableColors.Clear();
        
        if (grid != null)
        {
            var gridColors = grid.GetAvailableColors();
            availableColors.AddRange(gridColors);
        }
        
        Log($"Available colors: {availableColors.Count} - [{string.Join(", ", availableColors)}]");
    }
    
    // Check if current bubble's color is still valid, change if needed
    void ValidateCurrentBubble()
    {
        if (currentBubble == null) return;
        if (availableColors.Count == 0) return;
        
        Bubble bubble = currentBubble.GetComponent<Bubble>();
        if (bubble == null) return;
        
        // If current color is no longer available, change it
        if (!availableColors.Contains(bubble.type))
        {
            BubbleType newColor = GetRandomAvailableColor();
            Log($"Current bubble color {bubble.type} no longer available, changing to {newColor}");
            bubble.SetType(newColor);
        }
    }
    
    // Get a random color from available colors
    BubbleType GetRandomAvailableColor()
    {
        if (availableColors.Count == 0)
        {
            // Fallback to any color if grid is empty (shouldn't happen normally)
            return (BubbleType)Random.Range(0, System.Enum.GetValues(typeof(BubbleType)).Length);
        }
        
        return availableColors[Random.Range(0, availableColors.Count)];
    }
    
    void Update()
    {
        // Update aim BEFORE checking shoot, and only when pressed
        if (IsPointerPressed())
        {
            lastValidPointerPos = GetPointerPosition();
            UpdateAimRotation();
        }

        if (ShouldShoot())
        {
            Shoot();   
        }
    }
    
    // Get pointer position (works for both mouse and touch)
    Vector2 GetPointerPosition()
    {
        // Check touch first
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        
        // Fall back to mouse
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        
        return lastValidPointerPos;
    }
    
    // Check if pointer is currently pressed
    bool IsPointerPressed()
    {
        // Check touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }
        
        // Check mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
        
        return false;
    }
    
    bool ShouldShoot()
    {
        // Don't shoot if game is not playing
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        
        // Don't shoot while bubbles are being destroyed
        if (grid != null && grid.IsDestroying) return false;
        
        // Don't shoot if still on cooldown
        if (Time.time < lastShootTime + shootCooldown) return false;
        
        // Detect release (was pressed, now not pressed)
        bool isPressed = IsPointerPressed();
        bool shouldShoot = wasPressed && !isPressed;
        wasPressed = isPressed;
        
        return shouldShoot;
    }
    
    void UpdateAimRotation()
    {
        if (aimArrow == null || mainCam == null) return;

        // Use the last valid position
        Vector2 pointerPos = lastValidPointerPos;
        
        // Ignore if pointer is outside valid screen area
        if (pointerPos.x < 0 || pointerPos.x > Screen.width ||
            pointerPos.y < 0 || pointerPos.y > Screen.height)
            return;

        Vector3 pointerWorld = mainCam.ScreenToWorldPoint(new Vector3(pointerPos.x, pointerPos.y, 0));
        pointerWorld.z = 0f;

        Vector3 direction = (pointerWorld - transform.position).normalized;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, minRotationAngle, maxRotationAngle);

        float rad = angle * Mathf.Deg2Rad;
        aimArrow.transform.position = transform.position + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * arrowDistance;
        aimArrow.transform.rotation = Quaternion.Euler(0, 0, -angle);
    }
    
    void Shoot()
    {
        if (currentBubble == null) return;
        
        lastShootTime = Time.time;
        
        Vector3 shootDirection = (aimArrow.transform.position - transform.position).normalized;
        
        // Enable physics components
        Collider2D col = currentBubble.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;
        
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
            col.enabled = false;
            
            Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
            if (rb == null) 
            {
                rb = currentBubble.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.linearDamping = 0f;
            }
            rb.simulated = false;
            
            // Set random color from available colors only
            Bubble bubble = currentBubble.GetComponent<Bubble>();
            if (bubble != null)
            {
                bubble.SetType(GetRandomAvailableColor());
            }
        }
    }
}