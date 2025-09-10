using UnityEngine;
using UnityEngine.UI;

public class FOVSliderSimple : MonoBehaviour
{
    [SerializeField] Camera mainCam;
    [SerializeField] float minFOV = 60f;
    [SerializeField] float maxFOV = 110f;
    [SerializeField] float defaultFOV = 85f;

    const string PrefKey = "FOV";
    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();

        slider.minValue = minFOV;
        slider.maxValue = maxFOV;
        slider.wholeNumbers = true;

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(OnSliderChanged);

        float start = Mathf.Clamp(PlayerPrefs.GetFloat(PrefKey, defaultFOV), minFOV, maxFOV);
        slider.SetValueWithoutNotify(start);
    }

    void OnEnable()
    {
        Apply(slider.value);
    }

    void OnSliderChanged(float v)
    {
        Apply(v);
        PlayerPrefs.SetFloat(PrefKey, v);
        PlayerPrefs.Save();
    }

    void Apply(float fov)
    {
        fov = Mathf.Clamp(fov, minFOV, maxFOV);
        if (!mainCam) mainCam = Camera.main;
        if (!mainCam) return;

        mainCam.orthographic = false;
#if UNITY_2021_3_OR_NEWER
        mainCam.usePhysicalProperties = false;
#endif
        mainCam.fieldOfView = fov;
    }
}