using UnityEngine;

public class BreakableObject : MonoBehaviour
{

    public GameObject fracturedObjectPrefab;


    public float breakForce = 1f;
    public float explosionForce = 100f;
    public float explosionRadius = 5f;


    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Bullet"))
        {

            Destroy(other.gameObject);


            BreakObject();
        }
    }

    private void BreakObject()
    {
        GameObject fracturedObject = Instantiate(fracturedObjectPrefab, transform.position, transform.rotation);


        foreach (Transform piece in fracturedObject.transform)
        {

            if (piece.TryGetComponent<Rigidbody>(out Rigidbody pieceRigidbody))
            {

                pieceRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }


        Destroy(gameObject, 0.1f);
    }
}