namespace Anon.NET.Anonimization.Interfaces;

public interface IAnonymizationProcessor
{
    object? ProcessEntity(object entity);
    T? ProcessEntity<T>(T entity) where T : class;
}
