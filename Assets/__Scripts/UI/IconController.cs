using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class IconController : MonoBehaviour
{
    [SerializeField] Transform iconContainer;
    [SerializeField] GameObject iconContainerPrefab;

    private void OnEnable()
    {
        RequestIcons();
    }

    private void OnDisable()
    {
        foreach (Transform child in iconContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void ReceiveIcons(NetIcon[] icons)
    {
        foreach (Transform child in iconContainer)
        {
            Destroy(child.gameObject);
        }

        if (icons != null)
        {
            foreach (var icon in icons)
            {
                GameObject iconInstance = Instantiate(iconContainerPrefab, iconContainer);
                iconInstance.GetComponent<IconSetup>().Setup(icon);
            }
        }
    }

    private void RequestIcons()
    {
        string steamId = PlayerData.player.data.accountId.Value.ToString();
        if (HomeApi.Instance != null && NetworkManager.Singleton != null)
        {
            HomeApi.Instance.RequestGetIcons(steamId, NetworkManager.Singleton.LocalClientId);
        }
    }

    private void RequestChangeAvatar(int avatarId)
    {
        string steamId = PlayerData.player.data.accountId.Value.ToString();
        if (HomeApi.Instance != null)
        {
            HomeApi.Instance.RequestSetAvatar(steamId, avatarId);
        }
    }

    public static Sprite GetSpriteByAvatarId(int avatarId)
    {
        int spriteSheetIndex = avatarId / 16;
        int spriteIndex = avatarId % 16;
        string fullPath = "UI/icons/player/" + spriteSheetIndex;
        Sprite[] sprites = Resources.LoadAll<Sprite>(fullPath);
        return sprites[spriteIndex];
    }

    public static Sprite GetSpriteByClassId(int classId)
    {
        var character = GameDataController.instance.GetCharacter(classId);
        if (character != null)
            return Resources.Load<Sprite>(character.icon);
        return null;
    }

    public static Sprite GetSpriteByReId(int reId)
    {
        var reincarnation = GameDataController.instance.GetReincarnation(reId);
        if (reincarnation != null)
            return Resources.Load<Sprite>(reincarnation.icon);
        return null;
    }

    public static Sprite GetSpriteByGameMode(string mode)
    {
        string key = mode.ToLower();
        switch (key)
        {
            case "battleroyale":
                return Resources.Load<Sprite>("UI/icons/modes/battleroyale");
            case "survivor":
                return Resources.Load<Sprite>("UI/icons/modes/golem");
            case "streamer":
                return Resources.Load<Sprite>("UI/icons/modes/help");
            case "polemos":
                return Resources.Load<Sprite>("UI/icons/modes/polemos");
            default:
                return Resources.Load<Sprite>("UI/icons/modes/sanct");
        }
    }
}
