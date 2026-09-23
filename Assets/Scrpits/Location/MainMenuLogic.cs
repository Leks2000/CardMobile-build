using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scrpits.Location
{

    public class MainMenuLogic : MonoBehaviour
    {
        public string sceneName;
        public void QuitGame()
        {
            Application.Quit();
            Debug.Log("Выход из игры");
        }
        public void LoadScene()
        {
            Time.timeScale = 1.0f;
            if (sceneName == Assets.Scrpits.Run.RunState.MapSceneName)
            {
                Assets.Scrpits.Run.RunState.StartNewRun();
            }
            SceneManager.LoadScene(sceneName);
        }
        public void Pause()
        {
            Time.timeScale = 0.0f;
        }
        public void UnPause()
        {
            Time.timeScale = 1.0f;
        }
    }
}
