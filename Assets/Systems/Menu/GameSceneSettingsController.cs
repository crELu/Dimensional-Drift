using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Users;

public class GameSceneSettingsController : MonoBehaviour
{
    public GameObject settingsPanel;
    private float previousTimeScale = 1f;

    public GameObject firstSettingsSelectable;
    public GameObject deathScreenReplayButton;
    private bool _isUsingController;
    [SerializeField] private GameObject controllerUI, keyboardUI;
    
    private void Start()
    {
        settingsPanel.SetActive(false);
        InputUser.onChange += HandleInputChange;
        _isUsingController = Gamepad.all.Count > 0;
        ChangeUI();
    }

    private void ChangeUI()
    {
        controllerUI.SetActive(_isUsingController);
        keyboardUI.SetActive(!_isUsingController);
    }
    
    private void HandleInputChange(InputUser user, InputUserChange change, InputDevice device)
    {
        if (device != null && (change == InputUserChange.DevicePaired || change == InputUserChange.DeviceLost))
        {
            if (device is Keyboard || device is Mouse)
            {
                _isUsingController = false;
            }
            else if (device is Gamepad)
            {
                _isUsingController = true;
            }
        }

        ChangeUI();
    }    

    private void Update()
    {
        if (PlayerInputs.main.Settings) OnSettingsAction();
    }

    private void OnSettingsAction()
    {
        // Debug.Log("OnSettingsAction");
        bool isActive = settingsPanel.activeSelf;

        if (!isActive) // Opening settings
        {
            settingsPanel.SetActive(true);
            PlayerInputs.main.playerInput.SwitchCurrentActionMap("UI");
            Cursor.lockState = CursorLockMode.Confined;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            SettingsUIController settingsUI = settingsPanel.GetComponentInChildren<SettingsUIController>();
            StartCoroutine(SelectUIWithDelay(settingsUI.volumeSlider.gameObject));
        }
        else // Closing settings
        {
            settingsPanel.SetActive(false);
            PlayerInputs.main.playerInput.SwitchCurrentActionMap("Player");
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

    public void OnDeathScreenShown()
    {
        PlayerInputs.main.playerInput.SwitchCurrentActionMap("UI");
        Cursor.lockState = CursorLockMode.Confined;
        
        // Select the replay button
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(deathScreenReplayButton);
        Debug.Log("Death screen - selected replay button");
    }
}