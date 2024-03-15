using UnityEngine;

public class DestroyExplosion : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(DestroyInstance), 2f);
    }

    private void DestroyInstance()
    {
        Destroy(gameObject);
    }
}
