public interface IDidacticielApi
{
    void StartIt();
}

public static class DidacticielApi
{
    public static IDidacticielApi Instance { get; set; }
}
