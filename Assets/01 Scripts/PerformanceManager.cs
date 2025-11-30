using UnityEngine;

public class PerformanceManager : MonoBehaviour
{
    [Header("Frame Rate")]
    public int targetFrameRate = 60;
    public bool disableVSync = true;
    
    [Header("Quality")]
    public bool autoAdjustQuality = true;
    public int mobileQualityLevel = 0; // Lowest quality for mobile
    
    [Header("Physics (2D)")]
    public bool optimizePhysics = true;
    public float physicsTimeStep = 0.02f; // 50 physics updates per second
    
    void Awake()
    {
        // Set target frame rate
        Application.targetFrameRate = targetFrameRate;
        
        // Disable V-Sync for higher FPS
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }
        
        // Adjust quality for mobile
        if (autoAdjustQuality && (Application.platform == RuntimePlatform.Android || 
                                  Application.platform == RuntimePlatform.IPhonePlayer))
        {
            QualitySettings.SetQualityLevel(mobileQualityLevel, true);
        }
        
        // Optimize physics
        if (optimizePhysics)
        {
            Time.fixedDeltaTime = physicsTimeStep;
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        }
        
        // Disable screen dimming on mobile
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
    
    void Start()
    {
        LogPerformanceInfo();
    }
    
    void LogPerformanceInfo()
    {
        Debug.Log($"[Performance] Target FPS: {Application.targetFrameRate}");
        Debug.Log($"[Performance] VSync: {QualitySettings.vSyncCount}");
        Debug.Log($"[Performance] Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
        Debug.Log($"[Performance] Platform: {Application.platform}");
    }
}