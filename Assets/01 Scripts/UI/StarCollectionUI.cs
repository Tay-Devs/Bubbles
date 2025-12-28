using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

[DefaultExecutionOrder(-50)]
public class StarCollectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSession gameSession;
    [SerializeField] private RectTransform targetPosition;
    [SerializeField] private TotalStarsUI totalStarsUI;
    [SerializeField] private Canvas canvas;
    
    [Header("Star Prefab")]
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private RectTransform spawnParent;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnSpread = 30f;
    
    [Header("Animation Settings")]
    [SerializeField] private float delayBeforeStart = 0.3f;
    [SerializeField] private float delayBetweenStars = 0.15f;
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private Ease flyEase = Ease.InOutQuad;
    
    [Header("Star Animation")]
    [SerializeField] private float startScale = 1.2f;
    [SerializeField] private float endScale = 0.6f;
    [SerializeField] private float rotationAmount = 360f;
    
    [Header("Impact Effect")]
    [SerializeField] private float punchScale = 0.3f;
    [SerializeField] private float punchDuration = 0.2f;
    
    [Header("Audio")]
    [SerializeField] private SFXData starCollectSFX;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private int starsEarned = 0;
    private int completedLevelNumber = 0;
    private bool hasNewStars = false;
    
    void Awake()
    {
        CacheResultsData();
    }
    
    void Start()
    {
        if (hasNewStars)
        {
            StartCoroutine(StartCollectionDelayed());
        }
    }
    
    // Caches star data from GameSession before it gets cleared.
    // Calculates delta (new stars) vs what player already had on this level.
    private void CacheResultsData()
    {
        if (gameSession == null || !gameSession.hasResults)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[StarCollectionUI] No results to process");
            }
            return;
        }
        
        if (gameSession.starsEarned <= 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[StarCollectionUI] No stars earned");
            }
            return;
        }
        
        completedLevelNumber = gameSession.selectedLevel != null ? gameSession.selectedLevel.levelNumber : 0;
        
        // Get stars player already had on this level BEFORE results are saved
        int previousStarsOnLevel = 0;
        if (LevelDataManager.Instance != null && completedLevelNumber > 0)
        {
            previousStarsOnLevel = LevelDataManager.Instance.GetStarsForLevel(completedLevelNumber);
        }
        
        // Calculate delta - only new stars count
        int deltaStars = Mathf.Max(0, gameSession.starsEarned - previousStarsOnLevel);
        
        if (deltaStars <= 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[StarCollectionUI] No new stars (had {previousStarsOnLevel}, earned {gameSession.starsEarned})");
            }
            return;
        }
        
        starsEarned = deltaStars;
        hasNewStars = true;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarCollectionUI] Level {completedLevelNumber}: had {previousStarsOnLevel}, earned {gameSession.starsEarned}, delta {deltaStars}");
        }
    }
    
    // Waits for LevelMapController to be ready, then starts animation.
    private IEnumerator StartCollectionDelayed()
    {
        // Wait for LevelMapController to initialize and scroll
        yield return null;
        yield return null;
        
        // Calculate previous total
        int currentTotal = LevelDataManager.Instance != null ? LevelDataManager.Instance.TotalStars : 0;
        int previousTotal = currentTotal - starsEarned;
        
        // Set display to previous total
        if (totalStarsUI != null)
        {
            totalStarsUI.SetDisplayValue(Mathf.Max(0, previousTotal));
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarCollectionUI] Previous: {previousTotal}, Current: {currentTotal}");
        }
        
        // Get spawn position from LevelMapController
        Vector2 spawnScreenPos = GetLevelNodeScreenPosition();
        Vector2 spawnLocalPos = ScreenToLocalPosition(spawnScreenPos);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarCollectionUI] Spawn screen: {spawnScreenPos}, local: {spawnLocalPos}");
        }
        
        // Start spawning stars
        StartCoroutine(SpawnAndAnimateStars(spawnLocalPos));
    }
    
    // Gets the screen position of the completed level's node.
    private Vector2 GetLevelNodeScreenPosition()
    {
        if (LevelMapController.Instance != null && completedLevelNumber > 0)
        {
            // First scroll to the completed level so it's visible
            LevelMapController.Instance.ScrollToLevel(completedLevelNumber);
            
            // Use the new helper method
            Vector2 screenPos = LevelMapController.Instance.GetLevelScreenPosition(completedLevelNumber);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[StarCollectionUI] Level {completedLevelNumber} screen pos: {screenPos}");
            }
            
            return screenPos;
        }
        
        // Fallback to center of screen
        if (enableDebugLogs)
        {
            Debug.Log("[StarCollectionUI] Using fallback center position");
        }
        return new Vector2(Screen.width / 2f, Screen.height / 2f);
    }
    
    // Converts screen position to local position within spawn parent.
    private Vector2 ScreenToLocalPosition(Vector2 screenPos)
    {
        if (spawnParent == null || canvas == null) return Vector2.zero;
        
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            spawnParent,
            screenPos,
            canvasCamera,
            out Vector2 localPos
        );
        
        return localPos;
    }
    
    // Spawns star prefabs and animates each one flying to the target.
    private IEnumerator SpawnAndAnimateStars(Vector2 spawnLocalPos)
    {
        yield return new WaitForSeconds(delayBeforeStart);
        
        for (int i = 0; i < starsEarned; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpread;
            SpawnStar(spawnLocalPos + randomOffset, i);
            
            yield return new WaitForSeconds(delayBetweenStars);
        }
    }
    
    // Spawns a single star and animates it to the target position.
    private void SpawnStar(Vector2 spawnPos, int starIndex)
    {
        if (starPrefab == null || spawnParent == null || targetPosition == null) return;
        
        GameObject star = Instantiate(starPrefab, spawnParent);
        RectTransform starRect = star.GetComponent<RectTransform>();
        
        if (starRect == null)
        {
            Destroy(star);
            return;
        }
        
        // Set initial position and scale
        starRect.anchoredPosition = spawnPos;
        starRect.localScale = Vector3.one * startScale;
        
        // Play SFX at animation start with combo pitch
        if (starCollectSFX != null)
        {
            SFXManager.Play(starCollectSFX, starIndex);
        }
        
        // Calculate target position in local space
        Vector2 targetLocalPos = GetTargetLocalPosition();
        
        // Create animation sequence
        Sequence sequence = DOTween.Sequence();
        
        // Move to target
        sequence.Append(starRect.DOAnchorPos(targetLocalPos, flyDuration).SetEase(flyEase));
        
        // Scale down while flying
        sequence.Join(starRect.DOScale(endScale, flyDuration).SetEase(Ease.InQuad));
        
        // Rotate while flying
        sequence.Join(starRect.DORotate(new Vector3(0, 0, rotationAmount), flyDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));
        
        // On complete
        sequence.OnComplete(() => OnStarArrived(star));
        
        if (enableDebugLogs)
        {
            Debug.Log($"[StarCollectionUI] Spawned star {starIndex} at {spawnPos}, flying to {targetLocalPos}");
        }
    }
    
    // Gets the target position in spawn parent's local space.
    private Vector2 GetTargetLocalPosition()
    {
        if (spawnParent == null || targetPosition == null) return Vector2.zero;
        
        Vector3 targetWorld = targetPosition.position;
        return spawnParent.InverseTransformPoint(targetWorld);
    }
    
    // Called when a star reaches the target position.
    private void OnStarArrived(GameObject star)
    {
        // Increment counter with punch effect
        if (totalStarsUI != null)
        {
            totalStarsUI.IncrementWithPunch();
        }
        
        // Punch the target container
        if (targetPosition != null)
        {
            targetPosition.DOKill();
            targetPosition.localScale = Vector3.one;
            targetPosition.DOPunchScale(Vector3.one * punchScale, punchDuration, 5, 0.5f);
        }
        
        // Destroy the star
        Destroy(star);
        
        if (enableDebugLogs)
        {
            Debug.Log("[StarCollectionUI] Star arrived");
        }
    }
    
    // Public method to manually trigger star collection (for testing).
    public void TestCollectStars(int count)
    {
        starsEarned = count;
        Vector2 centerLocal = Vector2.zero;
        StartCoroutine(SpawnAndAnimateStars(centerLocal));
    }
}