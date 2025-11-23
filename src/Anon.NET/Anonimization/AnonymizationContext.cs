namespace Anon.NET.Anonimization;

public static class AnonymizationContext
{
    private static readonly AsyncLocal<bool> _isUpdating = new AsyncLocal<bool>();

    public static bool IsUpdating { get => _isUpdating.Value; set => _isUpdating.Value = value; }

    public static void ExecuteUpdate(Action action)
    {
        var previousState = IsUpdating;
        try
        {
            IsUpdating = true;
            action();
        }
        finally
        {
            IsUpdating = previousState;
        }
    }
}
