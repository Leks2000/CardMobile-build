using UnityEngine;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button saveButton;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        volumeSlider.value = savedVolume;
        audioSource.volume = savedVolume;

        volumeSlider.onValueChanged.AddListener(SetVolume);

        saveButton.onClick.AddListener(SaveVolume);
    }

    private void SetVolume(float value)
    {
        audioSource.volume = value;
    }

    private void SaveVolume()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.Save();
    }
}
