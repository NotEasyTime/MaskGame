using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Generic GameManager stub for editor tools compatibility.
    /// Replace with your actual implementation.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool isPaused = false;
        public int score = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PauseGame()
        {
            isPaused = true;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;
        }

        public void AddScore(int points)
        {
            score += points;
        }
    }

    /// <summary>
    /// Generic SceneManager stub for editor tools compatibility.
    /// Named to avoid conflict with UnityEngine.SceneManagement.SceneManager.
    /// Replace with your actual implementation.
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        public static SceneManager Instance { get; private set; }

        public string currentSceneName;
        public bool isLoading = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void LoadScene(string sceneName)
        {
            isLoading = true;
            currentSceneName = sceneName;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            isLoading = false;
        }

        public void LoadSceneAsync(string sceneName)
        {
            isLoading = true;
            currentSceneName = sceneName;
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
