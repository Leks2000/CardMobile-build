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
            var target = sceneName;
            if (sceneName == Assets.Scrpits.Run.RunState.MapSceneName)
            {
                try
                {
                    // сохранённый забег -> продолжить (прямо в бой/магазин, если вышли из него), иначе новый
                    if (!Assets.Scrpits.Run.RunState.IsActive) Assets.Scrpits.Run.RunSave.TryLoad();
                    if (!Assets.Scrpits.Run.RunState.IsActive) Assets.Scrpits.Run.RunState.StartNewRun();
                    target = Assets.Scrpits.Run.RunState.ResumeSceneName;
                }
                catch (System.Exception e) { Debug.LogException(e); }
            }
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
