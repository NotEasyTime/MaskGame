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
        [Tooltip("Delay in seconds before respawning after death (0 = respawn immediately)")]
        [SerializeField] private float respawnDelay = 2f;
        
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

        /// <summary>
        /// True when we are in a game scene and InitializeGame() has run (player spawned, game started).
        /// AI and other systems can check this before running (e.g. NavMesh agents wait until game is ready).
        /// </summary>
        public static bool IsGameReady { get; private set; }

        private int sceneIndex = 0;
        
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

            // Menu scene: show cursor so player can click Play, Settings, etc.
            if (!IsGameScene(sceneName))
            {
                IsGameReady = false;
                UnlockCursor();
                return;
            }

            // Game scene: spawn player and lock cursor when ready
            isInitializingScene = true;
            StartCoroutine(InitializeGameScene());
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
        /// Check if the given scene is a game scene (not a menu scene).
        /// Used by ObjectPoolManager and others to defer work until a game scene loads.
        /// </summary>
        public bool IsGameScene(string sceneName)
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
                IsGameReady = false;
                // We're in a menu scene: show cursor for UI (Play, Settings, etc.)
                UnlockCursor();
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
        
        /// <summary>
        /// Returns the spawn point to use: the one assigned in the Inspector, or the first PlayerSpawnPoint in the scene.
        /// </summary>
        private Transform GetEffectiveSpawnPoint()
        {
            if (playerSpawnPoint != null)
                return playerSpawnPoint;

            var spawnPoint = Object.FindFirstObjectByType<PlayerSpawnPoint>();
            if (spawnPoint != null)
            {
                Debug.Log($"GameManager: Using PlayerSpawnPoint from scene '{spawnPoint.gameObject.name}' (assign in Inspector to override).");
                return spawnPoint.transform;
            }

            return null;
        }

        private void SpawnPlayer()
        {
            Transform spawn = GetEffectiveSpawnPoint();
            Vector3 spawnPosition = spawn != null ? spawn.position : Vector3.zero;
            Quaternion spawnRotation = spawn != null ? spawn.rotation : Quaternion.identity;

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

            // Snap to spawn point (Rigidbody/CharacterController can override transform on next tick)
            SnapPlayerToSpawnPoint(playerInstance, spawnPosition, spawnRotation);

            EnsurePlayerMeshVisible(playerInstance);

            Debug.Log($"GameManager: Player spawned at {spawnPosition}");
        }

        /// <summary>
        /// Forces the player to the spawn position/rotation and syncs physics so they stay there.
        /// </summary>
        private void SnapPlayerToSpawnPoint(GameObject player, Vector3 position, Quaternion rotation)
        {
            if (player == null) return;

            Transform t = player.transform;
            t.position = position;
            t.rotation = rotation;

            // Rigidbody: sync physics state and zero velocity so gravity/forces don't move them
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = position;
                rb.rotation = rotation;
            }

            // CharacterController: disable before moving (some Unity versions ignore transform while enabled)
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                t.position = position;
                t.rotation = rotation;
                cc.enabled = true;
            }
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

        /// <summary>
        /// Show and unlock the cursor (for main menu and other UI scenes).
        /// </summary>
        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private bool IsInMenuScene()
        {
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return !IsGameScene(current);
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            // When window regains focus, restore cursor state for current scene
            if (hasFocus && Time.timeScale > 0f && !isPaused)
            {
                if (IsInMenuScene())
                    UnlockCursor();
                else
                    StartCoroutine(LockCursorDelayed());
            }
        }
        
        private void Update()
        {
            HandleEnemySpawning();
            
            // In menu scenes: keep cursor visible and unlocked
            if (IsInMenuScene())
            {
                UnlockCursor();
                return;
            }
            // In game: ensure cursor stays locked during gameplay (handles window focus, etc.)
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
            IsGameReady = true;

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
            if (respawnDelay > 0f)
                StartCoroutine(RespawnAfterDelay());
            else
                RespawnPlayer();
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);
            RespawnPlayer();
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

        /// <summary>
        /// Load the first game level (e.g. Level1). Call this from the main menu Play button.
        /// Uses Game Scene Names: index 1 is the first level when index 0 is MainMenu.
        /// </summary>
        public void StartFirstLevel()
        {
            if (gameSceneNames == null || gameSceneNames.Length == 0)
            {
                Debug.LogWarning("GameManager: No game scenes configured. Assign Game Scene Names in the Inspector.");
                return;
            }

            // First level: use index 1 when index 0 is MainMenu, otherwise index 0
            int firstLevelIndex = gameSceneNames.Length > 1 ? 1 : 0;
            string firstLevelName = gameSceneNames[firstLevelIndex];
            sceneIndex = firstLevelIndex;

            if (sceneManager != null)
            {
                sceneManager.LoadScene(firstLevelName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelName);
            }
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
            if (killCount >= 10)
            {
                killCount = 0;
                if(sceneIndex < gameSceneNames.Length) ++sceneIndex;
                Managers.SceneManager.Instance.LoadScene(gameSceneNames[sceneIndex]);
            }
        }
    }
}