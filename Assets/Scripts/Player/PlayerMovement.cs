using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{ 
    [SerializeField][Range(0.1f, 5f)] private float historicalPositionDuration = 1f;
    [SerializeField][Range(0.001f, 1f)] private float historicalPositionInterval = 0.1f;

    private Rigidbody rb;
    private Vector3 previousPosition;

    public Vector3 AverageVelocity
    {
        get
        {
            Vector3 average = Vector3.zero;
            foreach (Vector3 velocity in historicalVelocities)
            {
                average += velocity;
            }
            average.y = 0;

            return average / historicalVelocities.Count;
        }
    }

    private Queue<Vector3> historicalVelocities;
    private float lastPositionTime;
    private int maxQueueSize;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        maxQueueSize = Mathf.CeilToInt(1f / historicalPositionInterval * historicalPositionDuration);
        historicalVelocities = new Queue<Vector3>(maxQueueSize);
    }

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        // Custom velocity calculation based on position change
        Vector3 currentVelocity = (transform.position - previousPosition) / Time.deltaTime;
        // Optional: You might want to ignore Y-axis changes if they're irrelevant
        currentVelocity.y = 0;

        // Queue management as before
        if (lastPositionTime + historicalPositionInterval <= Time.time)
        {
            if (historicalVelocities.Count == maxQueueSize)
            {
                historicalVelocities.Dequeue();
            }

            historicalVelocities.Enqueue(currentVelocity);
            lastPositionTime = Time.time;
        }

        // Update previousPosition for the next frame's calculation
        previousPosition = transform.position;
    }
}
