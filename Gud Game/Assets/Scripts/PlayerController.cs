using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] LayerMask ignoreLayer;
    [SerializeField] CharacterController controller;

    [SerializeField] int speed;
    //[SerializeField] int sprintMod;
    //[SerializeField] int jumpSpeed;
    //[SerializeField] int jumpMax;
    //[SerializeField] int gravity;

    Vector3 moveDir;
    Vector3 playerVel;

    int jumpCount;

    bool isSprinting;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        movement();
    }

    void movement()
    {
        moveDir = (Input.GetAxis("Horizontal") * transform.right)
            + (Input.GetAxis("Vertical") * transform.forward);

        controller.Move(moveDir * speed * Time.deltaTime);
    }
}
