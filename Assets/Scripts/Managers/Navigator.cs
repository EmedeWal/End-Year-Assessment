using UnityEngine;
using UnityEngine.SceneManagement;

public class Navigator : MonoBehaviour
{
    private void Awake()
    {
        Cursor.visible = true;
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Main");
    }
}
