using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    public AudioClip[] playlist;  // Lista de canciones
    private AudioSource audioSource;
    private int currentTrackIndex = 0;

    private static MusicPlayer instance; // Singleton para que no se destruya

    void Awake()
    {
        // Asegurar que solo haya un MusicPlayer
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();

        if (playlist.Length > 0)
        {
            PlayTrack(currentTrackIndex);
        }
    }

    void Update()
    {
        // Si la canción ha terminado, reproducir la siguiente
        if (!audioSource.isPlaying)
        {
            NextTrack();
        }
    }

    void PlayTrack(int index)
    {
        if (index >= 0 && index < playlist.Length)
        {
            audioSource.clip = playlist[index];
            audioSource.Play();
        }
    }

    void NextTrack()
    {
        currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
        PlayTrack(currentTrackIndex);
    }
}
