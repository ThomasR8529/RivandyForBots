public interface IFillKeyboardApi
{
    void UpdateKeys();
}

public static class FillKeyboardApi
{
    public static IFillKeyboardApi Instance { get; set; }
}
