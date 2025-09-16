using UnityEngine;

public class Door : MonoBehaviour
{

    [SerializeField] private Animator animator;
    [SerializeField] private bool startLocked = true;

    bool _permanentLock;
    public bool IsLocked { get; private set; }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        IsLocked = startLocked;
        ApplyVisuals();
    }

    public void Lock() 
    { 
        IsLocked = true; 
        ApplyVisuals(); 
    }
    public void Unlock() 
    {
        if (_permanentLock) return;
        IsLocked = false; 
        ApplyVisuals(); 
    }

    public void SetPermanentLock(bool value)
    {
        _permanentLock = value;
        if (value) Lock();
    }

    void ApplyVisuals()
    {
        if (!animator) return;
        animator.SetBool("Locked", IsLocked);
        animator.SetTrigger(IsLocked ? "Close" : "Open");
    }
}
