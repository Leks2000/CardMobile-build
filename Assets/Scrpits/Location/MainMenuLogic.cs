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
            // START: продолжить идущий забег, иначе начать новый
            if (sceneName == Assets.Scrpits.Run.RunState.MapSceneName && !Assets.Scrpits.Run.RunState.IsActive)
            {
                try { Assets.Scrpits.Run.RunState.StartNewRun(); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
            var target = sceneName;
            SceneFade.Out(0.35f, () => SceneManager.LoadScene(target));
        }

        /// <summary>RESTART: бросить текущий забег и начать новый.</summary>
        public void RestartRun()
        {
            Time.timeScale = 1.0f;
            Assets.Scrpits.Run.RunState.StartNewRun();
            SceneFade.Out(0.35f, () => SceneManager.LoadScene(Assets.Scrpits.Run.RunState.MapSceneName));
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
