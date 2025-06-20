namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

public interface ISqlProvider
{
    string GetDeleteQuery<T>();
    string GetDeleteByPrimaryKeyQuery<T>();
    string GetInsertQuery<T>();
    string GetSelectQuery<T>();
    string GetSelectByPrimaryKeyQuery<T>();
    string GetUpdateQuery<T>();
    string GetUpdateByPrimaryKeyQuery<T>();
}
