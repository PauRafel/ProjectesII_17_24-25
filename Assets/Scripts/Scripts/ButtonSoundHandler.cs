using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundHandler : MonoBehaviour
{
    public AudioClip clickSound;

    private const float DEFAULT_VOLUME = 0.7f;

    private AudioSource audioSource;

    private void Start()
    {
        SetupAudioSource();
        AttachToButton();
    }

    private void SetupAudioSource()
    {
        audioSource = CreateAudioSource();
        ConfigureAudioSource();
    }

    private AudioSource CreateAudioSource()
    {
        return gameObject.AddComponent<AudioSource>();
    }

    private void ConfigureAudioSource()
    {
        audioSource.playOnAwake = false;
        audioSource.clip = clickSound;
        audioSource.volume = DEFAULT_VOLUME;
    }

    private void AttachToButton()
    {
        Button button = GetButtonComponent();
        if (IsButtonValid(button))
        {
            RegisterClickListener(button);
        }
    }

    private Button GetButtonComponent()
    {
        return GetComponent<Button>();
    }

    private bool IsButtonValid(Button button)
    {
        return button != null;
    }

    private void RegisterClickListener(Button button)
    {
        button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        if (HasValidSound())
        {
            PlayClickSound();
        }
    }

    private bool HasValidSound()
    {
        return clickSound != null;
    }

    private void PlayClickSound()
    {
        audioSource.PlayOneShot(clickSound);
    }
}