namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

public interface ISqlProvider
{
    string GetDeleteQuery<T>();
    string GetInsertQuery<T>();
    string GetSelectQuery<T>();
    string GetUpdateQuery<T>();
}
