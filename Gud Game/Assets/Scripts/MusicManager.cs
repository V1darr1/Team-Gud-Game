using System.Collections;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField]
    private MusicLibrary musicLibrary;
    [SerializeField]
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        // Stop any existing coroutines to ensure a clean start.
        StopAllCoroutines();

        // Start the music transition coroutine with the new track.
        StartCoroutine(AnimateMusicCrossfade(musicLibrary.GetClipFromName(trackName), fadeDuration));
    }

    IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration = 0.5f)
    {
        float percent = 0;
        float currentVolume = musicSource.volume;

        // Fade out the current music.
        while (percent < 1)
        {
            percent += Time.deltaTime * (1 / fadeDuration);
            musicSource.volume = Mathf.Lerp(currentVolume, 0, percent);
            yield return null;
        }

        // Change the audio clip after the fade-out is complete.
        musicSource.clip = nextTrack;
        musicSource.Play();

        percent = 0;
        // Fade in the new music.
        while (percent < 1)
        {
            percent += Time.deltaTime * (1 / fadeDuration);
            musicSource.volume = Mathf.Lerp(0, 1f, percent);
            yield return null;
        }
    }
}