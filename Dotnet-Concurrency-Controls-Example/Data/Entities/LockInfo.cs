namespace Dotnet_Concurrency_Controls.Data.Entities
{
    public class LockInfo
    {
        public bool IsLocked { get; set; }
        public string? LockedBy { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public string? LockType { get; set; }
    }
}
