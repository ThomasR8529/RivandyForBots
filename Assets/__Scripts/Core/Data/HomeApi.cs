public interface IHomeApi
{
    void RequestGetIcons(string steamId, ulong clientId);
    void RequestSetAvatar(string steamId, int avatarId);
}

public static class HomeApi
{
    public static IHomeApi Instance { get; set; }
}

