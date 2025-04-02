using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Systems.Menu
{
    public class MainMenu : MonoBehaviour
    {
        // public PlayerInput playerInput;

        void Start()
        {
            // playerInput.SwitchCurrentActionMap("UI");
            // Cursor.lockState = CursorLockMode.Confined;
            
        }
        
        // This method is called by the Play button
        public void PlayGame()
        {
            // playerInput.SwitchCurrentActionMap("Player");
            // Cursor.lockState = CursorLockMode.Locked;
            SceneManager.LoadScene("Main Scene");
        }

        public void ReturnToMainMenu()
        {
            SceneManager.LoadScene("Main Menu");
        }

        // This method is called by the Quit button
        public void QuitGame()
        {
            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}