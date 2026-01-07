using System.Collections.Generic;

public interface IPlayerGroupsApi
{
    List<ulong> GetUsersInGroup(int groupId);
}

public static class PlayerGroupsApi
{
    public static IPlayerGroupsApi Instance { get; set; }
}

