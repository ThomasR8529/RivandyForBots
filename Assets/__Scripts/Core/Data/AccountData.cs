using UnityEngine;
using Unity.Netcode;
using System;
using Unity.Collections;

public struct AccountStruct : INetworkSerializable, IEquatable<AccountStruct> {
    public int userId;
    public ulong clientId;

    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> accountId;
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> playerName;

    public int vip;

    public int classId;
    public int facId;
    public int reId;

    public int coin;
    public int redzap;

    public int avatarId;

    public int experience;
    public int level;

    public int win;
    public int lose;
    public int kill;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {

        serializer.SerializeValue(ref accountId);
        serializer.SerializeValue(ref playerName);

    }
    bool IEquatable<AccountStruct>.Equals(AccountStruct other) {
        return userId == other.userId;
    }

}