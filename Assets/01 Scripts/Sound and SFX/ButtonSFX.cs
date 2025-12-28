using UnityEngine;
using UnityEngine.UI;

public class ButtonSFX : MonoBehaviour
{
    [SerializeField] private SFXData clickSound;
    
    // Checks for Button or Toggle component and hooks into whichever exists.
    private void Start()
    {
        Button button = GetComponent<Button>();
        Toggle toggle = GetComponent<Toggle>();

        if (button != null)
        {
            button.onClick.AddListener(PlayClick);
            
        }
        else if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
            
        }
        else
        {
            Debug.LogWarning($"[ButtonSFX] No Button or Toggle found on {gameObject.name}");
        }
    }

    // Called when a Button is clicked. Plays the assigned click sound.
    private void PlayClick()
    {
        SFXManager.Play(clickSound);
    }

    // Called when a Toggle changes value. Plays sound regardless of on/off state.
    private void OnToggleChanged(bool isOn)
    {
        SFXManager.Play(clickSound);
    }
}