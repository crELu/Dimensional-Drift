
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.Menu
{
    public class MainMenu : MonoBehaviour
    {
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
    }
}