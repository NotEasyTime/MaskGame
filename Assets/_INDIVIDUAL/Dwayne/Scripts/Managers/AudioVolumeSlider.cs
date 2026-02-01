using UnityEngine;
using UnityEngine.UI;

namespace Managers
{
    /// <summary>
    /// Attach to a UI Slider to control Music or SFX volume.
    /// Slider min/max should be 0–1 (or use normalized value in OnValueChanged).
    /// Syncs slider value from manager on enable and writes to manager when the user drags.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class AudioVolumeSlider : MonoBehaviour
    {
        public enum VolumeType { Music, SFX }

        [SerializeField] private VolumeType volumeType = VolumeType.Music;
        [Tooltip("If true, slider value 0–1 maps directly. If false, slider range is used and we normalize to 0–1.")]
        [SerializeField] private bool sliderRangeIs01 = true;

        private Slider _slider;
        private bool _ignoreChanges;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            if (_slider == null) return;
            _slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnEnable()
        {
            if (_slider == null) _slider = GetComponent<Slider>();
            if (_slider == null) return;

            _ignoreChanges = true;
            float vol = GetVolume();
            if (sliderRangeIs01)
                _slider.value = vol;
            else
                _slider.value = Mathf.Lerp(_slider.minValue, _slider.maxValue, vol);
            _ignoreChanges = false;
        }

        private void OnDestroy()
        {
            if (_slider != null)
                _slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }

        private float GetVolume()
        {
            if (volumeType == VolumeType.Music && MusicManager.Instance != null)
                return MusicManager.Instance.MusicVolume;
            if (volumeType == VolumeType.SFX && SoundManager.Instance != null)
                return SoundManager.Instance.SFXVolume;
            return 1f;
        }

        private void SetVolume(float normalized01)
        {
            normalized01 = Mathf.Clamp01(normalized01);
            if (volumeType == VolumeType.Music && MusicManager.Instance != null)
                MusicManager.Instance.MusicVolume = normalized01;
            else if (volumeType == VolumeType.SFX && SoundManager.Instance != null)
                SoundManager.Instance.SFXVolume = normalized01;
        }

        private void OnSliderValueChanged(float value)
        {
            if (_ignoreChanges) return;
            float normalized = sliderRangeIs01 ? value : Mathf.InverseLerp(_slider.minValue, _slider.maxValue, value);
            SetVolume(normalized);
        }
    }
}
