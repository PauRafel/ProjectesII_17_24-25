using UnityEngine;
using UnityEngine.UI;

public class ButtonSoundHandler : MonoBehaviour
{
    public AudioClip clickSound; // Sonido que se reproducirá en los botones
    private AudioSource audioSource;

    void Start()
    {
        // Buscar o agregar un AudioSource en el objeto
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; // No queremos que suene al inicio
        audioSource.clip = clickSound;
        audioSource.volume = 0.7f; // Ajustar volumen si es necesario

        // Asegurar que el botón tenga una función asignada
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlaySound);
        }
    }

    private void PlaySound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
