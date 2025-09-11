using UnityEngine;

public class magicCircle : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag != "Player" || other.isTrigger) { return; }
        else { gameManager.instance.OpenWinMenu(); }
    }
}
