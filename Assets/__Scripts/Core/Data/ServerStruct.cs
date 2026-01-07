
using Unity.Netcode;
using Unity.Collections;

public struct ServerStruct : INetworkSerializable
{
    public int serverId;
    public ulong[] playerIds;
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> sceneName;

    public int serverMode;
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> ip;
    public ForceNetworkSerializeByMemcpy<FixedString64Bytes> port;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref serverId);
        serializer.SerializeValue(ref sceneName);
        serializer.SerializeValue(ref serverMode);
        serializer.SerializeValue(ref playerIds);
        serializer.SerializeValue(ref ip);
        serializer.SerializeValue(ref port);
    }

}