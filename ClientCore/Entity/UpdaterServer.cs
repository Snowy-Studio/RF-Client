namespace ClientCore.Entity
{
    public readonly record struct UpdaterServer(
#nullable enable
        string? id,
#nullable restore
        string name,
        int type,
        string location,
        string url,
        int priority
    );
}
