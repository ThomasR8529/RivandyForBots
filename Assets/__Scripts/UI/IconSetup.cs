using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconSetup : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button iconButton;

    public void Setup(NetIcon icon)
    {
        iconImage.sprite = IconController.GetSpriteByAvatarId(icon.avatarId);

        // Configurez l'image de l'ic�ne
        // iconImage.sprite = ... // Assignez l'image appropri�e ici

        iconButton.onClick.AddListener(() => ChangeAvatar(icon.avatarId));

        // Configurez le texte du co�t
        costText.text = icon.cost.ToString();

        // Si l'ic�ne n'est pas obtenue
        if (icon.obtained == 0 && icon.avatarId != 0)
        {
            // Rendre l'image grise
            iconImage.color = Color.grey;

            // D�sactiver le bouton
            iconButton.interactable = false;
        }
        else
        {
            // Rendre l'image normale
            iconImage.color = Color.white;

            // Activer le bouton
            iconButton.interactable = true;
        }
    }

    private void ChangeAvatar(int avatarId)
    {
        string steamId = PlayerData.player.data.accountId.Value.ToString();
        if (HomeApi.Instance != null)
        {
            HomeApi.Instance.RequestSetAvatar(steamId, avatarId);
        }
    }
}