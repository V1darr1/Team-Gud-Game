using UnityEngine;

public class PlatformCollision : MonoBehaviour
{
    //Handles the player sticking to the platform
    //Makes the player a child of the platform based on triggers

    [SerializeField] Transform platform;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            other.transform.parent = transform;
        }

        if (other.CompareTag("Enemy"))
        {
            other.transform.parent = transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            other.transform.parent = null;
        }

        if (other.CompareTag("Enemy"))
        {
            other.transform.parent = null;
        }
    }

}
