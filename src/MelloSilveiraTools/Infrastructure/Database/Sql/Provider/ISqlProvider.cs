using MelloSilveiraTools.Infrastructure.Database.Models.Entities;

namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

public interface ISqlProvider
{
    string GetBulkInsertSql<T>(int batchSize) where T : EntityBase;
    string GetCountSql<T>() where T : EntityBase;
    string GetDeleteSql<T>() where T : EntityBase;
    string GetDeleteByPrimaryKeySql<T>() where T : EntityBase;
    string GetExistByPrimaryKeySql<T>() where T : EntityBase;
    string GetInsertSql<T>() where T : EntityBase;
    string GetSelectSql<T>() where T : EntityBase;
    string GetSelectDistinctSql<T>() where T : EntityBase;
    string GetSelectByPrimaryKeySql<T>() where T : EntityBase;
    string GetUpdateSql<T>() where T : EntityBase;
    string GetUpdateByPrimaryKeySql<T>() where T : EntityBase;
}
