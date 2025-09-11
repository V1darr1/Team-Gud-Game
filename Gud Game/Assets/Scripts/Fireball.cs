using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] float lifeTime = 5f;
    [SerializeField] float damage = 10f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject,lifeTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;
       iDamageable dmg = other.GetComponent<iDamageable>();
        if (other.gameObject.CompareTag("Player") && dmg != null)
        {
            dmg.ApplyDamage(damage);
        }
        Destroy(gameObject);

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
