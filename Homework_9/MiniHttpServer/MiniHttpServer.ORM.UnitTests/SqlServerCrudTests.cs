using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Models;

namespace MiniHttpServer.ORM.UnitTests;

[TestClass]
public sealed class SqlServerCrudTests
{
    [TestMethod]
    [TestCategory("SqlServer")]
    public async Task Crud_WorksForUserAndProductModels()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ORIS_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            Assert.Inconclusive("Укажи ORIS_TEST_CONNECTION_STRING для запуска с SQL Server.");

        var orm = new OrmContext(connectionString!);
        string suffix = Guid.NewGuid().ToString("N");
        string users = $"OrmTestUsers_{suffix}";
        string products = $"OrmTestProducts_{suffix}";

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        try
        {
            await using var create = connection.CreateCommand();
            create.CommandText = $"""
                CREATE TABLE [{users}] (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Email NVARCHAR(255) NOT NULL,
                    Age INT NOT NULL
                );
                CREATE TABLE [{products}] (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Price DECIMAL(10,2) NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync();

            await CheckCrud(orm, users,
                new User { Id = 999, Name = "Олег", Email = "oleg@example.com", Age = 22 },
                new User { Id = 999, Name = "Олег", Email = "new@example.com", Age = 23 },
                user => user.Id, user => (user.Name, user.Email, user.Age));
            await CheckCrud(orm, products,
                new Product { Id = 999, Name = "O'Brien", Price = 40.75m },
                new Product { Id = 999, Name = "Карандаш", Price = 38.50m },
                product => product.Id, product => (product.Name, product.Price));
        }
        finally
        {
            await using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP TABLE IF EXISTS [{products}], [{users}];";
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task CheckCrud<T, TFields>(IOrmContext orm, string table, T original, T updated,
        Func<T, int> getId, Func<T, TFields> getFields) where T : class, new()
    {
        Assert.AreEqual(0, (await orm.GetAllAsync<T>(table)).Count);
        Assert.AreEqual(1, await orm.InsertAsync(table, original));

        var saved = (await orm.GetAllAsync<T>(table)).Single();
        int id = getId(saved);
        Assert.AreEqual(1, id);
        Assert.AreEqual(getFields(original), getFields(saved));

        Assert.AreEqual(1, await orm.UpdateAsync(table, id, updated));
        var changed = await orm.GetByIdAsync<T>(table, id);
        Assert.IsNotNull(changed);
        Assert.AreEqual(id, getId(changed));
        Assert.AreEqual(getFields(updated), getFields(changed));

        Assert.AreEqual(1, await orm.DeleteAsync(table, id));
        Assert.AreEqual(0, await orm.DeleteAsync(table, id));
        Assert.AreEqual(0, await orm.UpdateAsync(table, id, updated));
        Assert.IsNull(await orm.GetByIdAsync<T>(table, id));
        Assert.AreEqual(0, (await orm.GetAllAsync<T>(table)).Count);
    }
}
