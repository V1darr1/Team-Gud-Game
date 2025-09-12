using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [SerializeField] int sens;
    [SerializeField] int lockVertMin, lockVertMax;

    public bool invertY = false;
    public Camera mainCamera;

    float rotX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //skip look input when paused or a menu is active
        if (gameManager.instance != null && (gameManager.instance.isPaused || gameManager.instance.menuActive != null))
        {
            return;
        }

        //get input
        float mouseX = Input.GetAxisRaw("Mouse X") * sens * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sens * Time.deltaTime;

        //use invert Y to give option to look up/down
        if (invertY)
            rotX += mouseY;
        else
            rotX -= mouseY;

        //Clamp the camera on the X axis
        rotX = Mathf.Clamp(rotX, lockVertMin, lockVertMax);

        //rotate the camera to look up and down
        transform.localRotation = Quaternion.Euler(rotX, 0, 0);

        //rotate the player to look left and right
        transform.parent.Rotate(Vector3.up * mouseX);
    }

    public void SetInvertY(bool value)
    {
        invertY = value;
    }
}
