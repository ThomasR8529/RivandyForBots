using Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;

    public void SetupSpirit(Reincarnation spirit)
    {
        if (icon != null)
            icon.sprite = Resources.Load<Sprite>(spirit.icon);
        if (nameText != null)
            nameText.text = spirit.name;
    }
}
