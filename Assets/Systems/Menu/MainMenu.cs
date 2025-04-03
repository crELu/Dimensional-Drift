using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;

namespace Systems.Menu
{
    public class MainMenu : MonoBehaviour
    {

        public PlayerInput playerInput;
        public GameObject settingsPanel;


        private void Start()
        {
            playerInput.SwitchCurrentActionMap("UI");
            Cursor.lockState = CursorLockMode.Confined;
            settingsPanel.SetActive(false);
        }
        
        // This method is called by the Play button
        public void PlayGame()
        {
            SceneManager.LoadScene("Main Scene");
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
        }
    }
}