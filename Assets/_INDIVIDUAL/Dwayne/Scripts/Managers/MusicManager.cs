using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// Plays BGM per scene: main menu track and one loop per level.
    /// GameManager uses this to switch music when loading scenes.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        [Header("BGM Tracks")]
        [Tooltip("Music for main menu / menu scenes")]
        [SerializeField] private AudioClip mainMenuMusic;

        [Tooltip("Music per level (index 0 = first game level). Leave empty to use gameplayMusic for all levels.")]
        [SerializeField] private AudioClip[] levelMusic;

        [Tooltip("Fallback gameplay loop when levelMusic is empty or index out of range")]
        [SerializeField] private AudioClip gameplayMusic;

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float volume = 0.7f;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [Tooltip("Save/load volume from PlayerPrefs for UI settings persistence")]
        [SerializeField] private bool persistVolume = true;
        private const string PrefsKey = "Settings.MusicVolume";

        private AudioSource _source;
        private AudioClip _currentClip;

        /// <summary>
        /// Current music volume (0–1). Set from UI sliders; persists if persistVolume is true.
        /// </summary>
        public float MusicVolume
        {
            get => volume;
            set => SetVolume(value);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (persistVolume && PlayerPrefs.HasKey(PrefsKey))
                volume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKey));

            _source = GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = volume;
        }

        /// <summary>
        /// Play main menu BGM. Call from GameManager when in a menu scene.
        /// </summary>
        public void PlayMainMenu()
        {
            if (mainMenuMusic != null)
                Play(mainMenuMusic);
        }

        /// <summary>
        /// Play BGM for the given level index (0 = first level). Uses levelMusic[index] or gameplayMusic.
        /// </summary>
        public void PlayLevel(int levelIndex)
        {
            AudioClip clip = null;
            if (levelMusic != null && levelIndex >= 0 && levelIndex < levelMusic.Length && levelMusic[levelIndex] != null)
                clip = levelMusic[levelIndex];
            if (clip == null && gameplayMusic != null)
                clip = gameplayMusic;
            if (clip != null)
                Play(clip);
        }

        /// <summary>
        /// Play a specific clip once assigned (e.g. by scene name lookup). Loops the clip.
        /// </summary>
        public void Play(AudioClip clip)
        {
            if (clip == null || _source == null) return;
            if (_currentClip == clip && _source.isPlaying) return;
            _currentClip = clip;
            _source.clip = clip;
            _source.volume = volume;
            _source.Play();
        }

        /// <summary>
        /// Stop BGM.
        /// </summary>
        public void Stop()
        {
            if (_source != null)
                _source.Stop();
            _currentClip = null;
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            if (_source != null)
                _source.volume = volume;
            if (persistVolume)
                PlayerPrefs.SetFloat(PrefsKey, volume);
        }
    }
}
