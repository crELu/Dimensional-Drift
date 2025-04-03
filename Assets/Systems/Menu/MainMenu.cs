
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Systems.Menu
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private GameObject settingsPanel;


        private void Start()
        {
            settingsPanel.SetActive(false);
            playerInput.actions["Player/Settings"].performed += OnSettingsAction;
            playerInput.actions["UI/Settings"].performed += OnSettingsAction;

            
        }
        
        // This method is called by the Play button
        public void PlayGame()
        {
            // Change "GameScene" to the name of the scene you want to load.
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

        private void OnSettingsAction(InputAction.CallbackContext context)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
            if (settingsPanel.activeSelf)
            {
                playerInput.SwitchCurrentActionMap("UI");
                Cursor.lockState = CursorLockMode.Confined;
            }
            else
            {
                playerInput.SwitchCurrentActionMap("Player");
                Cursor.lockState = playerManager.targetCursorMode;
            }
        }
    }
}