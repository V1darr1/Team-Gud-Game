using UnityEngine;

public class RewardUI : MonoBehaviour
{
    public RewardButton[] buttons;
    RewardEveryNRooms _flow;

    public void Setup(RewardItem[] rewards, RewardEveryNRooms flow)
    {
        _flow = flow;
        gameObject.SetActive(true);

        int count = Mathf.Min(rewards.Length, buttons.Length);
        for (int i = 0; i < count; i++)
            buttons[i].Setup(rewards[i], _flow);

        for (int i = count; i < buttons.Length; i++)
            buttons[i].Clear();
    }

    public void Hide()
    {
        foreach (var b in buttons) b.Clear();
        gameObject.SetActive(false);
    }
}
