using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using UnityEditor.Build.Content;



public class enemyAI : MonoBehaviour
{

    [SerializeField] Renderer model;

    [SerializeField] int HP;
    [SerializeField] int faceTargetSpeed;


    bool playerInTrigger;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void faceTarget()
    {
        //Quaternion rot = Quaternion.LookRotation();
        //transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.deltaTime * faceTargetSpeed);
    }

    public void takeDamage(int amount)
    {
        if (HP > 0)
        {
            HP -= amount;
            //StartCoroutine());
        }
        if (HP <= 0)
        {
            //gamemanager.instance.updateGameGoal(-1);
            // Destroy(gameObject);
        }
    }

}
