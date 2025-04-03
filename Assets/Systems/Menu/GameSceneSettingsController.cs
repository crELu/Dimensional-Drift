using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class GameSceneSettingsController : MonoBehaviour
{
    public GameObject settingsPanel;
    private float previousTimeScale = 1f;
    public PlayerInput playerInput;

    // public GameObject firstShopSelectable;
    public GameObject firstSettingsSelectable;
    public GameObject deathScreenReplayButton;

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

            SettingsUIController settingsUI = settingsPanel.GetComponentInChildren<SettingsUIController>();
            StartCoroutine(SelectUIWithDelay(settingsUI.volumeSlider.gameObject));
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

    private IEnumerator SelectUIWithDelay(GameObject selectableObject)
    {
        // Wait for UI to be fully initialized
        yield return new WaitForEndOfFrame();
        
        // Select the UI element
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        EventSystem.current.SetSelectedGameObject(selectableObject);
        Debug.Log($"Selected: {selectableObject.name}");
    }
    
    public void PlayMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }
}