using UnityEngine;
using UnityEngine.AI;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;

    [Header("Variables")]
    [SerializeField] private Vector3 offset;
    [SerializeField] private float smoothTime;
    
    private Vector3 velocity = Vector3.zero;

    private void Update()
    {
        // This basically for these annoying times the player dies and Unity goes like WHERE IS MY FUCKING TARGET YOU ARSESHITE
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
