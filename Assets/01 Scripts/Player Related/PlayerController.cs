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
    private UIWorldAnchor worldAnchor;
    
    void Start()
    {
        grid = FindFirstObjectByType<HexGrid>();
        mainCam = Camera.main;
        lastValidPointerPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        
        if (grid != null)
        {
            grid.onColorsChanged += OnColorsChanged;
            enableDebugLogs = grid.enableDebugLogs;
        }
        
        UpdateAvailableColors();
        
        // Check if we have a UIWorldAnchor - if so, wait for it to position us first
        worldAnchor = GetComponent<UIWorldAnchor>();
        if (worldAnchor != null)
        {
            worldAnchor.onPositionApplied += OnPositionApplied;
            Debug.Log("[PlayerController] Subscribed to UIWorldAnchor.onPositionApplied, waiting for position...");
            // Don't spawn yet - wait for anchor to position us
        }
        else
        {
            Debug.Log("[PlayerController] No UIWorldAnchor found, spawning immediately");
            // No anchor, spawn immediately
            SpawnNewBubble();
            UpdateAimArrowPosition();
        }
    }
    
    void OnDestroy()
    {
        if (grid != null)
        {
            grid.onColorsChanged -= OnColorsChanged;
        }
        
        if (worldAnchor != null)
        {
            worldAnchor.onPositionApplied -= OnPositionApplied;
        }
    }
    
    void OnPositionApplied()
    {
        Debug.Log($"[PlayerController] OnPositionApplied received! Position is now: {transform.position}");
        SpawnNewBubble();
        UpdateAimArrowPosition();
    }
    
    void UpdateAimArrowPosition()
    {
        if (aimArrow == null) return;
        // Position arrow at default angle (straight up)
        aimArrow.transform.position = transform.position + new Vector3(0, arrowDistance, 0);
        aimArrow.transform.rotation = Quaternion.identity;
    }
    
    void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    
    void OnColorsChanged()
    {
        UpdateAvailableColors();
        ValidateCurrentBubble();
    }
    
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
    
    void ValidateCurrentBubble()
    {
        if (currentBubble == null) return;
        if (availableColors.Count == 0) return;
        
        Bubble bubble = currentBubble.GetComponent<Bubble>();
        if (bubble == null) return;
        
        if (!availableColors.Contains(bubble.type))
        {
            BubbleType newColor = GetRandomAvailableColor();
            Log($"Current bubble color {bubble.type} no longer available, changing to {newColor}");
            bubble.SetType(newColor);
        }
    }
    
    BubbleType GetRandomAvailableColor()
    {
        if (availableColors.Count == 0)
        {
            return (BubbleType)Random.Range(0, System.Enum.GetValues(typeof(BubbleType)).Length);
        }
        
        return availableColors[Random.Range(0, availableColors.Count)];
    }
    
    void Update()
    {
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
    
    Vector2 GetPointerPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return Touchscreen.current.primaryTouch.position.ReadValue();
        }
        
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        
        return lastValidPointerPos;
    }
    
    bool IsPointerPressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            return true;
        }
        
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            return true;
        }
        
        return false;
    }
    
    bool ShouldShoot()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        
        // Use the new IsDestroying property
        if (grid != null && grid.IsDestroying) return false;
        
        if (Time.time < lastShootTime + shootCooldown) return false;
        
        bool isPressed = IsPointerPressed();
        bool shouldShoot = wasPressed && !isPressed;
        wasPressed = isPressed;
        
        return shouldShoot;
    }
    
    void UpdateAimRotation()
    {
        if (aimArrow == null || mainCam == null) return;

        Vector2 pointerPos = lastValidPointerPos;
        
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
        
        Collider2D col = currentBubble.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;
        
        BubbleProjectile projectile = currentBubble.AddComponent<BubbleProjectile>();
        projectile.Fire(shootDirection, shootSpeed);
        
        currentBubble.transform.SetParent(null);
        currentBubble = null;
        
        SpawnNewBubble();
    }
    
    void SpawnNewBubble()
    {
        if (currentBubble != null)
            Destroy(currentBubble);
            
        if (bubblePrefab != null)
        {
            currentBubble = Instantiate(bubblePrefab, transform.position, Quaternion.identity);
            
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
            
            Bubble bubble = currentBubble.GetComponent<Bubble>();
            if (bubble != null)
            {
                bubble.SetType(GetRandomAvailableColor());
            }
        }
    }
}