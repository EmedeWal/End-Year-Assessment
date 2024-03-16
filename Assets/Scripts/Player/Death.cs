using UnityEngine;
using UnityEngine.SceneManagement;

public class Death : MonoBehaviour
{
    [SerializeField] private float deathDelay;
    [SerializeField] private string sceneToLoad;

    public void Die()
    {
        Invoke(nameof(DestroyInstance), deathDelay);
    }

    private void DestroyInstance()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
