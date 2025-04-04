using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Settings Values")]
    public float masterVolume = 1.0f;
    public float sensitivityMultiplier = 1.0f;
    public bool invertX { get; set; }
    public bool invertY { get; set; }
    private float baseMouseSensitivity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    private void Start()
    {
    }

    public void SetBaseMouseSensitivity(float value)
    {
        baseMouseSensitivity = value;
    }
    
    private void OnDestroy()
    {
    }

    public void ApplySettings()
    {
        AudioListener.volume = masterVolume;
        
        if (PlayerManager.main != null && PlayerManager.main.movement != null)
        {
            PlayerManager.main.movement.rotateSpeed = sensitivityMultiplier * baseMouseSensitivity;
            PlayerManager.main.movement.invertX = invertX;
            PlayerManager.main.movement.invertY = invertY;
        }
        
        SaveSettings();
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