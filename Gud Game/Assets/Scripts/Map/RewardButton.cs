using UnityEngine;
using UnityEngine.UI;

public class RewardButton : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public Text title;
    public Text desc;
    public Button button;

    RewardItem _reward;
    RewardEveryNRooms _flow;

    public void Setup(RewardItem reward, RewardEveryNRooms flow)
    {
        _reward = reward;
        _flow = flow;

        if (icon) icon.sprite = reward.icon;
        if (title) title.text = reward.itemName;
        if (desc) desc.text = reward.description;

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
            button.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (button) button.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    void OnClick()
    {
        if (_flow != null && _reward != null) _flow.Pick(_reward);
        else Debug.LogWarning("RewardButton missing flow or reward.");
    }
}
