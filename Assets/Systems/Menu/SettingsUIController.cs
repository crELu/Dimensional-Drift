using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Systems.Menu;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.InputSystem.Users;

public class SettingsUIController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject settingsPanel;  // Panel containing all UI elements
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle invertXToggle;
    public Toggle invertYToggle;
    public TextMeshProUGUI volumeValueText;
    public TextMeshProUGUI sensitivityValueText;
    public Button applyButton;
    public Button closeButton;
    
    [Header("Game-Only Buttons")]
    public Button restartButton;
    public Button returnToMenuButton;
    
    private float previousTimeScale = 1.0f;

    private void Start()
    {
        // Check current scene
        string currentScene = SceneManager.GetActiveScene().name;
        bool isMainMenu = currentScene == "Main Menu";
        
        // Hide game-only buttons in main menu
        if (isMainMenu && restartButton != null) {
            restartButton.gameObject.SetActive(false);
        }
            
        if (isMainMenu && returnToMenuButton != null) {
            returnToMenuButton.gameObject.SetActive(false);
        }
        
        InitializeUI();
        
        // Add listeners
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        applyButton.onClick.AddListener(ApplySettings);
        closeButton.onClick.AddListener(CloseSettings);
        applyButton.interactable = false;

        // Add this to Start() method after initializing UI
        StartCoroutine(SelectFirstUIElement());
    }

    private void InitializeUI()
    {
        // Set slider ranges
        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 3f;
        
        // Set initial values
        volumeSlider.value = SettingsManager.Instance.masterVolume;
        sensitivitySlider.value = SettingsManager.Instance.sensitivityMultiplier;
        invertXToggle.isOn = SettingsManager.Instance.invertX;
        invertYToggle.isOn = SettingsManager.Instance.invertY;
        previousTimeScale = Time.timeScale;
        
        // Update text displays
        UpdateVolumeText(SettingsManager.Instance.masterVolume);
        UpdateSensitivityText(SettingsManager.Instance.sensitivityMultiplier);
        applyButton.interactable = false;
    }

    private void OnVolumeChanged(float value)
    {
        SettingsManager.Instance.masterVolume = value;
        UpdateVolumeText(value);
        AudioListener.volume = value;
        applyButton.interactable = true;
    }

    private void OnSensitivityChanged(float value)
    {
        SettingsManager.Instance.sensitivityMultiplier = value;
        UpdateSensitivityText(value);
        applyButton.interactable = true;
    }

    private void OnInvertXChanged(bool value)
    {
        SettingsManager.Instance.invertX = value;
        applyButton.interactable = true;
    }

    private void OnInvertYChanged(bool value)
    {
        SettingsManager.Instance.invertY = value;
        applyButton.interactable = true;
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = $"{Mathf.Round(value * 100)}%";
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = $"{value:F2}";
    }

    private void ApplySettings()
    {
        SettingsManager.Instance.ApplySettings();
        EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
        applyButton.interactable = false;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMenu() {
        SceneManager.LoadScene("Main Menu");
    }

    public void CloseSettings()
    {
        // Resume game if we're not in the main menu
        string currentScene = SceneManager.GetActiveScene().name;
        bool isMainMenu = (currentScene == "Main Menu");
        
        if (!isMainMenu)
        {
            PlayerInputs.main.playerInput.SwitchCurrentActionMap("Player");
            Cursor.lockState = CursorLockMode.Locked;
            Time.timeScale = previousTimeScale;
            EventSystem.current.SetSelectedGameObject(null);
            
            // Re-bind actions after switching maps
            PlayerInputs.main.RebindActions();
        } else {
            GameObject mainMenu = GameObject.FindWithTag("MainMenu");
            MainMenu mainMenuScript = mainMenu.GetComponent<MainMenu>();
            mainMenuScript.CloseSettings();
        }  
        
        // Just deactivate the panel
        gameObject.SetActive(false);
    }

    private IEnumerator SelectFirstUIElement()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(null);
        yield return null;
        EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
        Debug.Log($"Settings UI initialized, selected: {volumeSlider.name}");
    }
}