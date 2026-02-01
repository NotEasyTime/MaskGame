using System;
using System.Collections;
using Pool;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Managers
{

/// <summary>
    /// Generic SceneManager stub for editor tools compatibility.
    /// Named to avoid conflict with UnityEngine.SceneManagement.SceneManager.
    /// Replace with your actual implementation.
    /// </summary>
    public class SceneManager : MonoBehaviour
    {
        private static SceneManager _instance;
        public static SceneManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<SceneManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SceneManager");
                        _instance = go.AddComponent<SceneManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("Loading Screen")]
        [Tooltip("Optional UI GameObject to show during scene loading")]
        [SerializeField] private GameObject loadingScreenUI;
        [Tooltip("Minimum time to show loading screen (prevents flashing)")]
        [SerializeField] private float minLoadingScreenTime = 0.5f;

        [Header("Settings")]
        [Tooltip("Reset time scale when loading scenes")]
        [SerializeField] private bool resetTimeScaleOnLoad = true;
        [Tooltip("Default time scale value")]
        [SerializeField] private float defaultTimeScale = 1f;

        public string currentSceneName;
        private string previousSceneName;
        private bool isLoading = false;
        private UnityEngine.AsyncOperation currentLoadOperation;
        
        // Events
        public System.Action<string> OnSceneLoadStarted;
        public System.Action<string> OnSceneLoadComplete;
        public System.Action<string> OnSceneUnloadStarted;
        public System.Action<float> OnLoadingProgressChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            previousSceneName = currentSceneName;
            currentSceneName = scene.name;
            isLoading = false;
            currentLoadOperation = null;

            OnSceneLoadComplete?.Invoke(scene.name);
        }

        #region Public Loading Methods

        /// <summary>
        /// Load a scene synchronously by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: Already loading a scene. Ignoring request to load '{sceneName}'");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneManager: Cannot load scene with null or empty name");
                return;
            }

            if (!SceneExists(sceneName))
            {
                Debug.LogError($"SceneManager: Scene '{sceneName}' does not exist in build settings");
                return;
            }

            isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            if (resetTimeScaleOnLoad)
            {
                Time.timeScale = defaultTimeScale;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Load a scene synchronously by build index
        /// </summary>
        public void LoadScene(int sceneBuildIndex)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: Already loading a scene. Ignoring request to load scene at index {sceneBuildIndex}");
                return;
            }

            if (sceneBuildIndex < 0 || sceneBuildIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneManager: Scene build index {sceneBuildIndex} is out of range");
                return;
            }

            isLoading = true;
            string sceneName = GetSceneNameByBuildIndex(sceneBuildIndex);
            OnSceneLoadStarted?.Invoke(sceneName);

            if (resetTimeScaleOnLoad)
            {
                Time.timeScale = defaultTimeScale;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);
        }

        /// <summary>
        /// Load a scene asynchronously by name with optional loading screen
        /// </summary>
        public void LoadSceneAsync(string sceneName, bool showLoadingScreen = true)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: Already loading a scene. Ignoring request to load '{sceneName}'");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("SceneManager: Cannot load scene with null or empty name");
                return;
            }

            if (!SceneExists(sceneName))
            {
                Debug.LogError($"SceneManager: Scene '{sceneName}' does not exist in build settings");
                return;
            }

            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, showLoadingScreen));
        }

        /// <summary>
        /// Load a scene asynchronously by build index with optional loading screen
        /// </summary>
        public void LoadSceneAsync(int sceneBuildIndex, bool showLoadingScreen = true)
        {
            if (isLoading)
            {
                Debug.LogWarning($"SceneManager: Already loading a scene. Ignoring request to load scene at index {sceneBuildIndex}");
                return;
            }

            if (sceneBuildIndex < 0 || sceneBuildIndex >= UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError($"SceneManager: Scene build index {sceneBuildIndex} is out of range");
                return;
            }

            string sceneName = GetSceneNameByBuildIndex(sceneBuildIndex);
            StartCoroutine(LoadSceneAsyncCoroutine(sceneName, showLoadingScreen));
        }

        /// <summary>
        /// Reload the current scene
        /// </summary>
        public void ReloadScene()
        {
            if (string.IsNullOrEmpty(currentSceneName))
            {
                currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            LoadScene(currentSceneName);
        }

        /// <summary>
        /// Reload the current scene asynchronously
        /// </summary>
        public void ReloadSceneAsync(bool showLoadingScreen = true)
        {
            if (string.IsNullOrEmpty(currentSceneName))
            {
                currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }
            LoadSceneAsync(currentSceneName, showLoadingScreen);
        }

        /// <summary>
        /// Quit the game (works in editor and build)
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("SceneManager: Quitting game...");
            
            if (resetTimeScaleOnLoad)
            {
                Time.timeScale = defaultTimeScale;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Private Coroutines

        private IEnumerator LoadSceneAsyncCoroutine(string sceneName, bool showLoadingScreen)
        {
            isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            if (resetTimeScaleOnLoad)
            {
                Time.timeScale = defaultTimeScale;
            }

            // Show loading screen
            if (showLoadingScreen && loadingScreenUI != null)
            {
                loadingScreenUI.SetActive(true);
            }

            float startTime = Time.realtimeSinceStartup;

            // Start loading
            currentLoadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            currentLoadOperation.allowSceneActivation = false;

            // Wait until scene is loaded (but don't activate yet)
            while (currentLoadOperation.progress < 0.9f)
            {
                OnLoadingProgressChanged?.Invoke(currentLoadOperation.progress);
                yield return null;
            }

            // Ensure we've shown loading screen for minimum time
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            if (elapsedTime < minLoadingScreenTime)
            {
                yield return new WaitForSecondsRealtime(minLoadingScreenTime - elapsedTime);
            }

            // Activate the scene
            currentLoadOperation.allowSceneActivation = true;

            // Wait for activation to complete
            while (!currentLoadOperation.isDone)
            {
                OnLoadingProgressChanged?.Invoke(1f);
                yield return null;
            }

            // Hide loading screen
            if (showLoadingScreen && loadingScreenUI != null)
            {
                loadingScreenUI.SetActive(false);
            }

            isLoading = false;
            currentLoadOperation = null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if a scene exists in build settings
        /// </summary>
        private bool SceneExists(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get scene name by build index
        /// </summary>
        private string GetSceneNameByBuildIndex(int buildIndex)
        {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
            return System.IO.Path.GetFileNameWithoutExtension(scenePath);
        }

        #endregion

        #region Properties

        public string CurrentSceneName => currentSceneName;
        public string PreviousSceneName => previousSceneName;
        public bool IsLoading => isLoading;
        public float LoadingProgress => currentLoadOperation != null ? currentLoadOperation.progress : 0f;
        public GameObject LoadingScreenUI
        {
            get => loadingScreenUI;
            set => loadingScreenUI = value;
        }

        #endregion
    }
}