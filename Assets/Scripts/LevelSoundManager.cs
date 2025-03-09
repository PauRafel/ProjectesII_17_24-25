using UnityEngine;

public class LevelSoundManager : MonoBehaviour
{
    public static LevelSoundManager instance;

    public AudioClip winSound;   // Sonido de victoria
    public AudioClip loseSound;  // Sonido de derrota
    public AudioClip startGameButtonSound; // Sonido btn main menu

    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // Mantener el objeto entre escenas
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayWinSound()
    {
        if (winSound != null)
        {
            audioSource.PlayOneShot(winSound, 0.2f);
        }
    }

    public void PlayLoseSound()
    {
        if (loseSound != null)
        {
            audioSource.PlayOneShot(loseSound, 0.4f);
        }
    }

    public void PlayStartSound()
    {
        if (startGameButtonSound != null)
        {
            audioSource.PlayOneShot(startGameButtonSound, 0.4f);
        }
    }
}
