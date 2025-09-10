using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class cameraControl : MonoBehaviour
{
    [SerializeField] int sens;
    [SerializeField] int lockVertMin, LockVertMax;
    [SerializeField] bool invertY;

    float rotateX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //get input
        float mouseX = Input.GetAxisRaw("Mouse X") * sens * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * sens * Time.deltaTime;
        //inverseY Check
        if (invertY)
            rotateX += mouseY;
        else
            rotateX -= mouseY;
        //Clamp the camera on the Axis (to avoid inverting camera)
        rotateX = Mathf.Clamp(rotateX, lockVertMin, LockVertMax);

        //rotate camera up/down
        transform.localRotation = Quaternion.Euler(rotateX, 0, 0);

        //rotate player for left/right
        transform.parent.Rotate(Vector3.up * mouseX);

    }
}
