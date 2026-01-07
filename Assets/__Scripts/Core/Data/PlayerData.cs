using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

[Serializable]
public class PlayerData : MonoBehaviour
{
    public static PlayerData player;
    public PlayerStruct data;
    public StatStruct statData;
    public ServerStruct serverInfo;


    //public EquipStruct equip;
    //public PlayerItem item;


    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (player == null)
        {
            player = this;
        }
    }
}
[Serializable]

public struct PlayerStruct : INetworkSerializable, IEquatable<PlayerStruct>
{
    public int userId;
    public ulong clientId;

    public int groupId;

    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> accountId;
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> playerName;

    public int vip;

    public int classId;
    public int factionId;
    public int reId;

    public int coin;
    public int redzap;

    public int avatarId;


    public float experience;
    public int level;

    public int win;
    public int lose;
    public int rankedPoint;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref userId);
        serializer.SerializeValue(ref factionId);
        serializer.SerializeValue(ref clientId);

        serializer.SerializeValue(ref groupId);

        serializer.SerializeValue(ref accountId);
        serializer.SerializeValue(ref playerName);

        serializer.SerializeValue(ref classId);
        serializer.SerializeValue(ref reId);

        serializer.SerializeValue(ref vip);

        serializer.SerializeValue(ref coin);
        serializer.SerializeValue(ref redzap);

        serializer.SerializeValue(ref avatarId);

        serializer.SerializeValue(ref experience);
        serializer.SerializeValue(ref level);
        serializer.SerializeValue(ref win);
        serializer.SerializeValue(ref lose);
        serializer.SerializeValue(ref rankedPoint);
    }
    bool IEquatable<PlayerStruct>.Equals(PlayerStruct other)
    {
        return userId == other.userId;
    }

}

[Serializable]
public struct StatStruct : INetworkSerializable, IEquatable<StatStruct>
{
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> accountId;

    public float health;
    public float maxHealth;
    public int kill;
    public int monsterKills;
    public float shield;
    public int soul;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {

        serializer.SerializeValue(ref accountId);
        serializer.SerializeValue(ref health);
        serializer.SerializeValue(ref maxHealth);
        serializer.SerializeValue(ref kill);
        serializer.SerializeValue(ref monsterKills);
        serializer.SerializeValue(ref soul);
        serializer.SerializeValue(ref shield);
    }
    bool IEquatable<StatStruct>.Equals(StatStruct other)
    {
        return accountId.Equals(other.accountId);
    }

}





public struct NetIcon : INetworkSerializable, IEquatable<PlayerStruct>
{
    public int avatarId;
    public int isVisible;
    public int cost;
    public int shop;
    public int obtained;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref avatarId);
        serializer.SerializeValue(ref isVisible);
        serializer.SerializeValue(ref cost);
        serializer.SerializeValue(ref shop);
        serializer.SerializeValue(ref obtained);
    }
    bool IEquatable<PlayerStruct>.Equals(PlayerStruct other)
    {
        return avatarId == other.avatarId;
    }

}