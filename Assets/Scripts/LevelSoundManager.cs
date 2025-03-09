using UnityEngine;

public class LevelSoundManager : MonoBehaviour
{
    public static LevelSoundManager instance;

    public AudioClip winSound;   // Sonido de victoria
    public AudioClip loseSound;  // Sonido de derrota
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
            audioSource.PlayOneShot(winSound);
        }
    }

    public void PlayLoseSound()
    {
        if (loseSound != null)
        {
            audioSource.PlayOneShot(loseSound);
        }
    }
}
