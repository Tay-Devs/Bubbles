using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransfer : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string levelSelectSceneName = "LevelSelect";
    
    [Header("Audio")]
    [SerializeField] private SFXData buttonClickSFX;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Called by button OnClick. Plays SFX and loads the level select scene.
    public void GoToLevelSelect()
    {
        if (buttonClickSFX != null)
        {
            SFXManager.Play(buttonClickSFX);
        }
        
        if (enableDebugLogs) Debug.Log($"[MainMenuUI] Loading scene: {levelSelectSceneName}");
        
        SceneManager.LoadScene(levelSelectSceneName);
    }
}