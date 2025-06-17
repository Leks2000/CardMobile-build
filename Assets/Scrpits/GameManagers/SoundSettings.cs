using UnityEngine;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button saveButton;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Toggle muteToggle;
    [SerializeField] private Toggle fullscreenToggle;

    private float currentVolume;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        bool isMuted = PlayerPrefs.GetInt("Muted", 0) == 1;
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        volumeSlider.value = savedVolume;
        muteToggle.isOn = !isMuted;
        fullscreenToggle.isOn = isFullscreen;

        currentVolume = savedVolume;

        UpdateVolume();

        Screen.fullScreen = isFullscreen;

        volumeSlider.onValueChanged.AddListener(delegate
        {
            currentVolume = volumeSlider.value;
        });
        muteToggle.onValueChanged.AddListener(delegate
        {
            UpdateVolume();
        });
        fullscreenToggle.onValueChanged.AddListener(delegate
        {
            FullscreenChanged(fullscreenToggle.isOn);
        });

        saveButton.onClick.AddListener(SaveSettings);
    }

    private void UpdateVolume()
    {
        if (MusicManager.Instance == null)
        {
            return;
        }

        MusicManager.Instance.GetComponent<AudioSource>().volume = muteToggle.isOn ? currentVolume : 0f;
    }


    private void FullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", currentVolume);
        PlayerPrefs.SetInt("Muted", muteToggle.isOn ? 0 : 1);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateVolume();
        Screen.fullScreen = fullscreenToggle.isOn;
    }
}
