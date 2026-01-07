using System;
using Unity.Netcode;

[Serializable]
public struct GroupInvitationData
{
    public int GroupId;
    public string LeaderName;
    public int LeaderLevel;
    public string LeaderIcon;
}

[Serializable]
public struct GroupMemberData
{
    public string AccountId;
    public int UserId;
    public string Username;
    public int Level;
    public string AvatarGame;

    public bool IsReady;
}

// Classe pour transmettre les données sur le réseau
[Serializable]
public struct ShopItemData : INetworkSerializable
{
    public int ShopId;
    public int Cost;

    public string Name;
    public string Description;
    public string Icon;

    public bool IsEquipped;
    public bool IsPurchased;
    public bool IsDefault;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ShopId);
        serializer.SerializeValue(ref Cost);
        serializer.SerializeValue(ref IsEquipped);
        serializer.SerializeValue(ref IsPurchased);
        serializer.SerializeValue(ref IsDefault);
    }
}

