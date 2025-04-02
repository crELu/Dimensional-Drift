using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject settingsPanel;  // Panel containing all UI elements
    public Slider volumeSlider;
    public Slider sensitivitySlider;
    public Toggle invertXToggle;
    public Toggle invertYToggle;
    public TextMeshProUGUI volumeValueText;
    public TextMeshProUGUI sensitivityValueText;
    public Button applyButton;
    
    [Header("Settings Values")]
    public float masterVolume = 1.0f;
    public float sensitivityMultiplier = 1.0f;
    private bool invertX = false;
    private bool invertY = false;
    private float baseMouseSensitivity;
    
    [Header("References")]
    public PlayerInput playerInput;  // Make this public and assign in Inspector

    private float previousTimeScale = 1f; // Store previous time scale

    private void Awake()
    {
        // if (Instance != null && Instance != this)
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        
        // Hide settings panel initially
        settingsPanel.SetActive(false);
    }

    private void Start()
    {
        // If not assigned in Inspector, try to find in scene
        if (playerInput == null)
        {
            playerInput = FindObjectOfType<PlayerInput>();
            Debug.Log($"Found PlayerInput: {playerInput != null}");
        }
        
        // Subscribe to settings action
        if (playerInput != null)
        {
            Debug.Log("Subscribing to settings actions");
            playerInput.actions["Player/Settings"].performed += OnSettingsAction;
            playerInput.actions["UI/Settings"].performed += OnSettingsAction;
        }
        else
        {
            Debug.LogError("PlayerInput reference is missing!");
        }
        
        // Initialize UI
        InitializeUI();

        if (PlayerManager.main != null && PlayerManager.main.movement != null)
        {
            baseMouseSensitivity = PlayerManager.main.movement.rotateSpeed;
        }
        
        // Add listeners
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        applyButton.onClick.AddListener(ApplySettings);
        applyButton.interactable = false;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from settings action
        if (playerInput != null)
        {
            playerInput.actions["UI/Settings"].performed -= OnSettingsAction;
        }
    }

    private void OnSettingsAction(InputAction.CallbackContext context)
    {
        Debug.Log("OnSettingsAction");
        if (settingsPanel.activeSelf)
        {
            // Hide settings panel
            settingsPanel.SetActive(false);
            playerInput.SwitchCurrentActionMap("Player");
            Cursor.lockState = CursorLockMode.Locked;
            
            // Resume game
            Time.timeScale = previousTimeScale;
        }
        else
        {
            // Show settings panel
            settingsPanel.SetActive(true);
            playerInput.SwitchCurrentActionMap("UI");
            Cursor.lockState = CursorLockMode.Confined;
            
            // Pause game
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            
            // Update UI in case values changed elsewhere
            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        // Set slider ranges
        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 3f;
        
        // Set initial values
        volumeSlider.value = masterVolume;
        sensitivitySlider.value = sensitivityMultiplier;
        invertXToggle.isOn = invertX;
        invertYToggle.isOn = invertY;
        
        // Update text displays
        UpdateVolumeText(masterVolume);
        UpdateSensitivityText(sensitivityMultiplier);
        applyButton.interactable = false;
    }

    private void OnVolumeChanged(float value)
    {
        masterVolume = value;
        UpdateVolumeText(value);
        AudioListener.volume = value;
        applyButton.interactable = true;
    }

    private void OnSensitivityChanged(float value)
    {
        sensitivityMultiplier = value;
        UpdateSensitivityText(value);
        applyButton.interactable = true;
    }

    private void OnInvertXChanged(bool value)
    {
        invertX = value;
        applyButton.interactable = true;
    }

    private void OnInvertYChanged(bool value)
    {
        invertY = value;
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

    public void ApplySettings()
    {
        AudioListener.volume = masterVolume;
        
        if (PlayerManager.main != null && PlayerManager.main.movement != null)
        {
            PlayerManager.main.movement.rotateSpeed = sensitivityMultiplier * baseMouseSensitivity;
        }
        
        // Apply invert settings
        if (PlayerManager.main != null && PlayerManager.main.movement != null)
        {
            // You'll need to add these properties to PlayerMovement
            PlayerManager.main.movement.invertX = invertX;
            PlayerManager.main.movement.invertY = invertY;
        }
        
        SaveSettings();
        Debug.Log("Settings applied");
        applyButton.interactable = false;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("SensitivityMultiplier", sensitivityMultiplier);
        PlayerPrefs.SetInt("InvertX", invertX ? 1 : 0);
        PlayerPrefs.SetInt("InvertY", invertY ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        sensitivityMultiplier = PlayerPrefs.GetFloat("SensitivityMultiplier", 1.0f);
        invertX = PlayerPrefs.GetInt("InvertX", 0) == 1;
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
    }
}