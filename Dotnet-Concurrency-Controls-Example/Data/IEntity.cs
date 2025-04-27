namespace Dotnet_Concurrency_Controls.Data
{
    public interface IEntity<T>
    {
        T Id { get; }
    }
}
