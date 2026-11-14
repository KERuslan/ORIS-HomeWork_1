using Microsoft.Data.SqlClient;

namespace MiniHttpServer.ORM;

public sealed class OrmContext : IOrmContext
{
    private readonly string _connectionString;

    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = new SqlConnectionStringBuilder(connectionString).ConnectionString;
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName, CancellationToken cancellationToken = default) where T : class, new()
    {
        await using var command = SqlCommands.Select(tableName);
        await using var connection = new SqlConnection(_connectionString);
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var mapper = new RowMapper<T>(reader);
        var result = new List<T>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(mapper.Map(reader));

        return result;
    }

    public async Task<T?> GetByIdAsync<T>(
        string tableName, int id, CancellationToken cancellationToken = default) where T : class, new()
    {
        await using var command = SqlCommands.SelectById(tableName, id);
        await using var connection = new SqlConnection(_connectionString);
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new RowMapper<T>(reader).Map(reader);
    }

    public Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default)
        where T : class => ExecuteAsync(SqlCommands.Insert(tableName, entity), cancellationToken);

    public Task<int> UpdateAsync<T>(
        string tableName, int id, T entity, CancellationToken cancellationToken = default)
        where T : class => ExecuteAsync(SqlCommands.Update(tableName, id, entity), cancellationToken);

    public Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(SqlCommands.Delete(tableName, id), cancellationToken);

    private async Task<int> ExecuteAsync(SqlCommand command, CancellationToken cancellationToken)
    {
        await using var ownedCommand = command;
        await using var connection = new SqlConnection(_connectionString);
        command.Connection = connection;
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
