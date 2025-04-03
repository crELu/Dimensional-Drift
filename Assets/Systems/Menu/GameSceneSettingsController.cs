using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class GameSceneSettingsController : MonoBehaviour
{
    public GameObject settingsPanel;
    private float previousTimeScale = 1f;
    public PlayerInput playerInput;

    private void Start()
    {
        settingsPanel.SetActive(false);
        if (playerInput != null)
        {
            playerInput.actions["Player/Settings"].performed += OnSettingsAction;
            playerInput.actions["UI/Settings"].performed += OnSettingsAction;
        }
    }

    private void OnSettingsAction(InputAction.CallbackContext context)
    {
        // Debug.Log("OnSettingsAction");
        bool isActive = settingsPanel.activeSelf;

        if (!isActive) // Opening settings
        {
            settingsPanel.SetActive(true);
            playerInput.SwitchCurrentActionMap("UI");
            Cursor.lockState = CursorLockMode.Confined;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else // Closing settings
        {
            settingsPanel.SetActive(false);
            playerInput.SwitchCurrentActionMap("Player");
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = previousTimeScale;
            
            // Re-bind actions after switching maps
            if (PlayerInputs.main != null)
            {
                PlayerInputs.main.RebindActions();
            }
        }
    }
    
    public void PlayMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}