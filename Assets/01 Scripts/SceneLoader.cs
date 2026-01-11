using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] [Range(0.3f, 0.9f)] private float screenHeightPercent = 0.75f;
    [SerializeField] private float aspectRatio = 9f / 16f;

    private void Awake()
    {
        SetMobileWindowSize();
    }
    // Calculates window size based on a percentage of your monitor's height.
    // Then applies the mobile aspect ratio (9:16) to determine the width.
    private void SetMobileWindowSize()
    {
        // Get current monitor resolution
        int monitorHeight = Display.main.systemHeight;
        
        // Calculate window height as a percentage of monitor height
        int windowHeight = Mathf.RoundToInt(monitorHeight * screenHeightPercent);
        
        // Calculate width based on mobile aspect ratio (9:16 portrait)
        int windowWidth = Mathf.RoundToInt(windowHeight * aspectRatio);

        Screen.SetResolution(windowWidth, windowHeight, false);
    }

    // Call this from a UI Button's OnClick event.
    // Pass the scene name as a string parameter in the inspector.
    public void LoadScene(string sceneName)
    {
        
        SceneManager.LoadScene(sceneName);
    }

    // Same as above but loads the scene asynchronously for smoother transitions.
    // Useful for larger scenes to avoid freezing.
    public void LoadSceneAsync(string sceneName)
    {
        SceneManager.LoadSceneAsync(sceneName);
    }
}