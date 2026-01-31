using System;
using System.Collections;
using Pool;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Generic GameManager stub for editor tools compatibility.
    /// Replace with your actual implementation.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Player Setup")]
        [Tooltip("Player prefab to spawn (optional - if not set, will use existing player in scene)")]
        public GameObject playerPrefab;
        [Tooltip("Location where player spawns/respawns")]
        public Transform playerSpawnPoint;
        
        [Header("Enemy Spawning")]
        public GameObject[] enemyPrefabs;
        public Transform[] enemySpawnPoints;
        public float enemySpawnRate = 5f;
        public int maxEnemies = 100;
        
        [Header("System References (Optional)")]
        [SerializeField] private GameObject objectPoolManagerPrefab;
        [SerializeField] private GameObject sceneManagerPrefab;
        
        [Header("Scene Settings")]
        [Tooltip("Scene names that are considered game scenes (player will spawn here)")]
        [SerializeField] private string[] gameSceneNames = new string[] { };
        
        [Tooltip("Scene names that are menu scenes (player will NOT spawn here)")]
        [SerializeField] private string[] menuSceneNames = new string[] { "MainMenu" };
        
        private ObjectPoolManager objectPoolManager;
        private SceneManager sceneManager;
        private GameObject playerInstance;

        public bool isPaused = false;
        public int score = 0;
        private int killCount;
        private int currentEnemyCount = 0;
        private float nextEnemySpawnTime;
        private bool isInitializingScene = false;
        
        public System.Action<int> OnKillCountChanged;
        public System.Action OnGameStart;
        public System.Action OnGameEnd;
        public System.Action OnGamePaused;
        public System.Action OnGameResumed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize framework systems (this will also subscribe to SceneManager events)
            InitializeSystems();

            // Fallback: subscribe directly to Unity's event if SceneManager not available
            // This ensures we still get scene loaded events even if SceneManager fails to initialize
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedUnity;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene loaded events
            if (sceneManager != null)
            {
                sceneManager.OnSceneLoadComplete -= OnSceneLoaded;
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedUnity;
            }
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
            LockCursor();
            OnGameResumed?.Invoke();
        }

        public void AddScore(int points)
        {
            score += points;
        }
        
        /// <summary>
        /// Called when SceneManager reports a scene has loaded
        /// </summary>
        private void OnSceneLoaded(string sceneName)
        {
            // Prevent duplicate initialization
            if (isInitializingScene) return;

            // Only spawn player if we're in a game scene
            if (IsGameScene(sceneName))
            {
                isInitializingScene = true;
                StartCoroutine(InitializeGameScene());
            }
        }

        /// <summary>
        /// Fallback handler for Unity's scene loaded event (used if SceneManager not available)
        /// </summary>
        private void OnSceneLoadedUnity(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // Only handle if SceneManager didn't already handle it
            // SceneManager's event fires first, so this is just a safety fallback
            if (!isInitializingScene)
            {
                OnSceneLoaded(scene.name);
            }
        }

        /// <summary>
        /// Check if the current scene is a game scene (not a menu scene)
        /// </summary>
        private bool IsGameScene(string sceneName)
        {
            // Check if it's a menu scene first
            foreach (string menuScene in menuSceneNames)
            {
                if (sceneName == menuScene)
                {
                    return false;
                }
            }

            // Check if it's explicitly a game scene
            foreach (string gameScene in gameSceneNames)
            {
                if (sceneName == gameScene)
                {
                    return true;
                }
            }

            // Default: if not in menu scene list, assume it's a game scene
            // This allows for flexibility with custom scene names
            return true;
        }

        private void InitializeSystems()
        {
            // Initialize Object Pool Manager
            objectPoolManager = Object.FindFirstObjectByType<ObjectPoolManager>();
            if (!objectPoolManager)
            {
                // Create object pool manager if prefab provided
                if (objectPoolManagerPrefab)
                {
                    GameObject poolObj = Instantiate(objectPoolManagerPrefab);
                    objectPoolManager = poolObj.GetComponent<ObjectPoolManager>();
                }
                else
                {
                    // Create standalone GameObject for pool manager
                    GameObject poolObj = new GameObject("ObjectPoolManager");
                    objectPoolManager = poolObj.AddComponent<ObjectPoolManager>();
                    DontDestroyOnLoad(poolObj);
                }

                Debug.Log("GameManager: ObjectPoolManager initialized");
            }

            // Initialize Scene Manager
            sceneManager = Object.FindFirstObjectByType<SceneManager>();
            if (!sceneManager)
            {
                // Create scene manager if prefab provided
                if (sceneManagerPrefab)
                {
                    GameObject sceneObj = Instantiate(sceneManagerPrefab);
                    sceneManager = sceneObj.GetComponent<SceneManager>();
                }
                else
                {
                    // SceneManager is a singleton, so we can just access Instance
                    // It will create itself if it doesn't exist
                    sceneManager = SceneManager.Instance;
                }

                Debug.Log("GameManager: SceneManager initialized");
            }

            // Subscribe to SceneManager's scene loaded event now that it's initialized
            if (sceneManager != null)
            {
                sceneManager.OnSceneLoadComplete += OnSceneLoaded;
            }

            Debug.Log("GameManager: Framework systems initialized successfully");
        }

        private void Start()
        {
            // Check if we're in a game scene before spawning player
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            if (IsGameScene(currentSceneName))
            {
                // We're in a game scene, initialize it
                StartCoroutine(InitializeGameScene());
            }
            else
            {
                // We're in a menu scene, don't spawn player
                Debug.Log($"GameManager: In menu scene '{currentSceneName}', skipping player spawn.");
            }
        }

        /// <summary>
        /// Initialize the game scene (spawn player, setup game)
        /// </summary>
        private IEnumerator InitializeGameScene()
        {
            if (playerPrefab != null)
            {
                SpawnPlayer();
            }

            // Wait for player to be ready (with 5 second timeout)
            float timeoutSeconds = 5f;
            float startTime = Time.time;
            bool playerFound = false;
            while (Time.time - startTime < timeoutSeconds)
            {
                if (IsPlayerReady())
                {
                    playerFound = true;
                    break;
                }
                yield return null;
            }

            if (!playerFound)
            {
                Debug.LogWarning("GameManager: Player not found within timeout period! Make sure to either assign a playerPrefab or place a player in the scene.");
            }

            // Initialize game
            InitializeGame();

            // Lock cursor after a short delay to ensure window is fully initialized (especially when maximized)
            yield return StartCoroutine(LockCursorDelayed());

            // Reset initialization flag
            isInitializingScene = false;
        }
        
        private void SpawnPlayer()
        {
            // Determine spawn position
            Vector3 spawnPosition = playerSpawnPoint != null ? playerSpawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = playerSpawnPoint != null ? playerSpawnPoint.rotation : Quaternion.identity;

            // Destroy existing player instance if we spawned it previously
            if (playerInstance != null)
            {
                // Find and destroy any detached cameras to avoid orphaned camera objects
                var cameras = playerInstance.GetComponentsInChildren<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam.transform.parent != playerInstance.transform)
                    {
                        // Camera is not a direct child, might get orphaned
                        Destroy(cam.gameObject);
                    }
                }

                Destroy(playerInstance);
            }

            // Spawn player
            playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            playerInstance.name = "Player"; // Clean up the name (remove "(Clone)")

            EnsurePlayerMeshVisible(playerInstance);

            Debug.Log($"GameManager: Player spawned at {spawnPosition}");
        }
        
        /// <summary>
        /// Returns true when the player instance exists and is ready.
        /// </summary>
        private bool IsPlayerReady()
        {
            return playerInstance != null;
        }

        /// <summary>
        /// Ensures all renderers on the player are visible (e.g. after spawn).
        /// </summary>
        private void EnsurePlayerMeshVisible(GameObject player)
        {
            if (player == null) return;
            foreach (var r in player.GetComponentsInChildren<Renderer>())
                r.enabled = true;
        }
        
        private IEnumerator LockCursorDelayed()
        {
            // Wait a few frames to ensure the window is fully initialized
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Lock cursor for gameplay
            LockCursor();
        }
        
        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // When window regains focus (e.g., after maximizing), lock cursor if game is not paused
            if (hasFocus && Time.timeScale > 0f && !isPaused)
            {
                StartCoroutine(LockCursorDelayed());
            }
        }
        
        private void Update()
        {
            HandleEnemySpawning();
            
            // Ensure cursor stays locked during gameplay (handles cases where window is maximized or regains focus)
            // Only lock if game is not paused and cursor should be locked
            if (Time.timeScale > 0f && Cursor.lockState != CursorLockMode.Locked && !isPaused)
            {
                LockCursor();
            }
        }

        private void InitializeGame()
        {
            score = 0;
            killCount = 0;
            currentEnemyCount = 0;
            
            OnKillCountChanged?.Invoke(killCount);
            OnGameStart?.Invoke();

            // Set initial spawn time
            nextEnemySpawnTime = Time.time + enemySpawnRate;
        }

        private void HandleEnemySpawning()
        {
           // Talk to spawner to spawn enemies at intervals
           if (enemyPrefabs.Length == 0 || enemySpawnPoints.Length == 0)
               return;
        }
        
         public void OnEnemyKilled()
        {
            AddKill();
        }

        public void OnPlayerDeath()
        {
            OnGameEnd?.Invoke();
        }

        public void RespawnPlayer()
        {
            // Otherwise, use player prefab
            if (playerPrefab != null)
            {
                SpawnPlayer();
            }
            // Otherwise, reset existing player
            else
            {
               // Reset existing player position and state
                
            }

            // Reset some game state
            OnGameStart?.Invoke();

            // Lock cursor after respawn
            LockCursor();
        }

        public void RestartGame()
        {
            // Clear all enemies
           

            currentEnemyCount = 0;

            // Reset scores
            score = 0;
            killCount = 0;
            OnKillCountChanged?.Invoke(killCount);

            // Respawn player
            RespawnPlayer();
        }

        private void AddKill()
        {
            killCount++;
            OnKillCountChanged?.Invoke(killCount);
        }
    }

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
