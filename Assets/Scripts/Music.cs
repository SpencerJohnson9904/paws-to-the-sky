using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [SerializeField] AudioClip backgroundMusic;
    [SerializeField] float volume = 0.5f;

    AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f; // 2D so it's always the same volume
        audioSource.Play();
    }
}