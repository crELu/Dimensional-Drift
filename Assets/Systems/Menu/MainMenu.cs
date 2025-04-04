using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.EventSystems;
using System.Collections;
namespace Systems.Menu
{
    public class MainMenu : MonoBehaviour
    {
        
        public GameObject settingsPanel;
        public GameObject firstSelectable;


        private void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
            PlayerInputs.main.playerInput.SwitchCurrentActionMap("UI");
            settingsPanel.SetActive(false);
            EventSystem.current.SetSelectedGameObject(firstSelectable);
        }
        
        // This method is called by the Play button
        public void PlayGame()
        {
            SceneManager.LoadScene("Main Scene");
            Time.timeScale = 1;
        }

        // This method is called by the Quit button
        public void QuitGame()
        {
            // This will quit the game when built.
            Application.Quit();

            // If you're testing in the editor, you can use the following line to stop play mode:
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        // This method is called by the Options/Settings button
        public void OpenSettings()
        {
            // Debug.Log("OpenSettings");
            settingsPanel.SetActive(true);
            
            // Get the SettingsUIController
            SettingsUIController settingsUI = settingsPanel.GetComponentInChildren<SettingsUIController>();
            
            // Set the first selectable UI element with a delay to ensure UI is initialized
            StartCoroutine(SelectUIWithDelay(settingsUI.volumeSlider.gameObject));
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

        public void CloseSettings() {
            EventSystem.current.SetSelectedGameObject(firstSelectable);
            Debug.Log($"Selected: {firstSelectable.name}");
        }
    }
}