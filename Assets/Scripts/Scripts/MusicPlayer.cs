using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [Header("Music Configuration")]
    public AudioClip[] playlist;

    private AudioSource audioSource;
    private int currentTrackIndex = 0;

    private static MusicPlayer instance;

    private const int FIRST_TRACK_INDEX = 0;

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Update()
    {
        HandleTrackProgression();
    }

    private void InitializeSingleton()
    {
        if (ShouldCreateInstance())
        {
            CreatePersistentInstance();
            SetupAudioSystem();
        }
        else
        {
            DestroyDuplicateInstance();
        }
    }

    private bool ShouldCreateInstance()
    {
        return instance == null;
    }

    private void CreatePersistentInstance()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void DestroyDuplicateInstance()
    {
        Destroy(gameObject);
    }

    private void SetupAudioSystem()
    {
        CacheAudioSource();
        StartPlaylistIfAvailable();
    }

    private void CacheAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void StartPlaylistIfAvailable()
    {
        if (HasPlaylistTracks())
        {
            PlayTrack(FIRST_TRACK_INDEX);
        }
    }

    private bool HasPlaylistTracks()
    {
        return playlist.Length > 0;
    }

    private void HandleTrackProgression()
    {
        if (ShouldAdvanceToNextTrack())
        {
            NextTrack();
        }
    }

    private bool ShouldAdvanceToNextTrack()
    {
        return !audioSource.isPlaying;
    }

    private void PlayTrack(int index)
    {
        if (IsValidTrackIndex(index))
        {
            SetCurrentTrack(index);
            StartPlayback();
        }
    }

    private bool IsValidTrackIndex(int index)
    {
        return index >= 0 && index < playlist.Length;
    }

    private void SetCurrentTrack(int index)
    {
        audioSource.clip = playlist[index];
    }

    private void StartPlayback()
    {
        audioSource.Play();
    }

    private void NextTrack()
    {
        AdvanceTrackIndex();
        PlayTrack(currentTrackIndex);
    }

    private void AdvanceTrackIndex()
    {
        currentTrackIndex = (currentTrackIndex + 1) % playlist.Length;
    }
}