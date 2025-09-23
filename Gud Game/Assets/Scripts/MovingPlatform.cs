using System.IO;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private MovingPath path;
    [SerializeField] private float speed;

    private int targetPathIndex;

    private Transform prevPath;
    private Transform targetPath;

    private float timeToPath;
    private float elapsedTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TargetNextPath();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        elapsedTime += Time.deltaTime;

        float elapsedPecentage = elapsedTime / timeToPath;

        //Change pos based on amount of time in journey of the path
        transform.position = Vector3.Lerp(prevPath.position, targetPath.position, elapsedPecentage);

        if(elapsedPecentage >= 1)
        {
            TargetNextPath();
        }
    }

    private void TargetNextPath()
    {
        prevPath = path.GetPath(targetPathIndex);
        targetPathIndex = path.GetNextPathIndex(targetPathIndex);
        targetPath = path.GetPath(targetPathIndex);

        elapsedTime = 0;

        float disToPath = Vector3.Distance(prevPath.position, targetPath.position);
        timeToPath = disToPath / speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        other.transform.SetParent(transform);
    }

    private void OnTriggerExit(Collider other)
    {
        other.transform.SetParent(null);
    }
}
