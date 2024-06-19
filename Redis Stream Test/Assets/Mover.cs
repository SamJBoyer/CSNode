using UnityEngine;

public class Mover : MonoBehaviour
{
    public float speed = 5f; // Speed of movement
    public float distance = 5f; // Distance to move

    private Vector3 startPosition; // Initial position
    private Vector3 endPosition; // Final position
    private bool movingForward = true; // Flag to track movement direction

    void Start()
    {
        // Initialize start and end positions
        startPosition = transform.position;
        endPosition = startPosition + Vector3.right * distance;
    }

    void Update()
    {
        // Move the object back and forth
        if (movingForward)
        {
            transform.position = Vector3.MoveTowards(transform.position, endPosition, speed * Time.deltaTime);
            if (transform.position == endPosition)
            {
                // Change direction
                movingForward = false;
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, speed * Time.deltaTime);
            if (transform.position == startPosition)
            {
                // Change direction
                movingForward = true;
            }
        }
    }
}
