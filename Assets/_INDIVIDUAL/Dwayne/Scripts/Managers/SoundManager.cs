using UnityEngine;
using Object = UnityEngine.Object;

namespace Managers
{
    /// <summary>
    /// One-shot sound effects for abilities, player, and enemies.
    /// Use SoundManager.Instance.PlaySFX(clip) from anywhere.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField] private int maxConcurrentSFX = 8;
        [SerializeField] private bool dontDestroyOnLoad = true;
        [Tooltip("Save/load volume from PlayerPrefs for UI settings persistence")]
        [SerializeField] private bool persistVolume = true;
        private const string PrefsKey = "Settings.SFXVolume";

        private AudioSource[] _sources;
        private int _nextSourceIndex;

        /// <summary>
        /// Current SFX volume (0–1). Set from UI sliders; persists if persistVolume is true.
        /// </summary>
        public float SFXVolume
        {
            get => masterVolume;
            set => SetMasterVolume(value);
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
                masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefsKey));

            _sources = new AudioSource[maxConcurrentSFX];
            for (int i = 0; i < maxConcurrentSFX; i++)
            {
                var go = new GameObject($"SFX_{i}");
                go.transform.SetParent(transform);
                var src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                _sources[i] = src;
            }
        }

        /// <summary>
        /// Play a one-shot sound effect. Safe to call with null clip (no-op).
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sources == null || _sources.Length == 0) return;
            float vol = Mathf.Clamp01(masterVolume * volumeScale);
            AudioSource src = _sources[_nextSourceIndex];
            _nextSourceIndex = (_nextSourceIndex + 1) % _sources.Length;
            src.pitch = 1f;
            src.volume = vol;
            src.PlayOneShot(clip);
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            if (persistVolume)
                PlayerPrefs.SetFloat(PrefsKey, masterVolume);
        }
    }
}
