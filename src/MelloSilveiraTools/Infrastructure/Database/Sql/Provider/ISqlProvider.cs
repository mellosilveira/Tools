namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

public interface ISqlProvider
{
    string GetBulkInsertSql<T>(int batchSize);
    string GetCountSql<T>();
    string GetDeleteSql<T>();
    string GetDeleteByPrimaryKeySql<T>();
    string GetExistByPrimaryKeySql<T>();
    string GetInsertSql<T>();
    string GetSelectSql<T>();
    string GetSelectByPrimaryKeySql<T>();
    string GetUpdateSql<T>();
    string GetUpdateByPrimaryKeySql<T>();
}
