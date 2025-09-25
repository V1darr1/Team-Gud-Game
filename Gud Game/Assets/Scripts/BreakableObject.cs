using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    public GameObject brokenbarrel;

    private void OnMouseDown()
    {
        Instantiate(brokenbarrel, transform.position, transform.rotation);
        Destroy(gameObject);
    }





}