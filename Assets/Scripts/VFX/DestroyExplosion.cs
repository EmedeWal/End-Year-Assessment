using UnityEngine;

public class DestroyExplosion : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        audioSource.Play();

        Invoke(nameof(DestroyInstance), 2f);
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}
