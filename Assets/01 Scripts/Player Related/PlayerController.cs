using System;
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
    
    [Header("Next Bubble Preview")]
    public Transform nextBubblePreviewParent; // Optional: parent for world space preview
    private GameObject nextBubblePreview;
    private BubbleType nextBubbleType;
    
    [Header("Cooldown")]
    private float resumeGraceTime = 0f;
    private const float RESUME_GRACE_DURATION = 0.1f;
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    // Events
    public static event Action onBubbleConnected;
    public static event Action<BubbleType> onNextBubbleChanged; // UI can subscribe to this
    
    private HexGrid grid;
    private Camera mainCam;
    private bool wasPressed = false;
    private Vector2 lastValidPointerPos;
    private List<BubbleType> availableColors = new List<BubbleType>();
    private UIWorldAnchor worldAnchor;
    
    private bool isBubbleFlying = false;
    
    public bool IsBubbleFlying => isBubbleFlying;
    public BubbleType NextBubbleType => nextBubbleType;
    
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
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onResume.AddListener(OnGameResume);
        }
        
        UpdateAvailableColors();
        
        worldAnchor = GetComponent<UIWorldAnchor>();
        if (worldAnchor != null)
        {
            worldAnchor.onPositionApplied += OnPositionApplied;
            Debug.Log("[PlayerController] Subscribed to UIWorldAnchor.onPositionApplied, waiting for position...");
        }
        else
        {
            Debug.Log("[PlayerController] No UIWorldAnchor found, spawning immediately");
            InitializeBubbles();
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
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onResume.RemoveListener(OnGameResume);
        }
    }
    
    void OnGameResume()
    {
        resumeGraceTime = Time.unscaledTime + RESUME_GRACE_DURATION;
        wasPressed = false;
    }
    
    void OnPositionApplied()
    {
        Debug.Log($"[PlayerController] OnPositionApplied received! Position is now: {transform.position}");
        InitializeBubbles();
    }
    
    // Initializes current bubble and next bubble preview.
    void InitializeBubbles()
    {
        // Generate first "next" bubble type
        nextBubbleType = GetRandomAvailableColor();
        
        // Spawn current bubble (uses nextBubbleType, then generates new next)
        SpawnNewBubble();
        UpdateAimArrowPosition();
        
        // Create preview if parent is assigned
        CreateNextBubblePreview();
    }
    
    void UpdateAimArrowPosition()
    {
        if (aimArrow == null) return;
        aimArrow.transform.position = transform.position + new Vector3(0, arrowDistance, 0);
        aimArrow.transform.rotation = Quaternion.identity;
    }
    
    void Log(string msg) { if (enableDebugLogs) Debug.Log(msg); }
    
    void OnColorsChanged()
    {
        UpdateAvailableColors();
        ValidateCurrentBubble();
        ValidateNextBubble();
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
    
    // Validates that next bubble type is still available in grid.
    // If not, picks a new random available color.
    void ValidateNextBubble()
    {
        if (availableColors.Count == 0) return;
        
        if (!availableColors.Contains(nextBubbleType))
        {
            BubbleType newColor = GetRandomAvailableColor();
            Log($"Next bubble color {nextBubbleType} no longer available, changing to {newColor}");
            nextBubbleType = newColor;
            
            UpdateNextBubblePreview();
            onNextBubbleChanged?.Invoke(nextBubbleType);
        }
    }
    
    BubbleType GetRandomAvailableColor()
    {
        if (availableColors.Count == 0)
        {
            return (BubbleType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(BubbleType)).Length);
        }
        
        return availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
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
        
        if (grid != null && grid.IsDestroying) return false;
        
        if (isBubbleFlying) return false;
        
        if (Time.unscaledTime < resumeGraceTime) return false;
        
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
        
        isBubbleFlying = true;
        Log("[PlayerController] Bubble fired, waiting for connection...");
        
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
    
    public void OnBubbleConnectedToGrid()
    {
        isBubbleFlying = false;
        Log("[PlayerController] Bubble connected to grid");
        
        onBubbleConnected?.Invoke();
    }
    
    // Spawns current bubble using nextBubbleType, then generates new next bubble.
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
                // Use the pre-determined next bubble type
                bubble.SetType(nextBubbleType);
            }
            
            // Generate new next bubble type
            nextBubbleType = GetRandomAvailableColor();
            UpdateNextBubblePreview();
            onNextBubbleChanged?.Invoke(nextBubbleType);
            
            Log($"[PlayerController] Spawned bubble, next will be: {nextBubbleType}");
        }
    }
    
    // Creates a preview bubble in world space if parent is assigned.
    void CreateNextBubblePreview()
    {
        if (nextBubblePreviewParent == null || bubblePrefab == null) return;
        
        if (nextBubblePreview != null)
            Destroy(nextBubblePreview);
        
        nextBubblePreview = Instantiate(bubblePrefab, nextBubblePreviewParent);
        nextBubblePreview.transform.localPosition = Vector3.zero;
        
        // Disable physics
        Collider2D col = nextBubblePreview.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        Rigidbody2D rb = nextBubblePreview.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        
        UpdateNextBubblePreview();
    }
    
    // Updates the preview bubble's color to match nextBubbleType.
    void UpdateNextBubblePreview()
    {
        if (nextBubblePreview == null) return;
        
        Bubble bubble = nextBubblePreview.GetComponent<Bubble>();
        if (bubble != null)
        {
            bubble.SetType(nextBubbleType);
        }
    }
}