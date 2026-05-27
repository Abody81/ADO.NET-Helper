using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

#pragma warning disable 

namespace ADO_Helper;

[DebuggerStepThrough]
public class ADOHelper
{
    private readonly string _connectionString;
    public ADOHelper(string connectionString) { _connectionString = connectionString; }


    public delegate void SqlCommandDelegate(SqlCommand command);

    private async Task<T> ExecuteInternalAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc,
        CommandType commandType, CancellationToken ct, Func<SqlCommand, Task<T>> executionLogic)
    {
        using SqlConnection connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(ct).ConfigureAwait(false);

        using SqlCommand command = new SqlCommand(Query, connection);

        command.CommandType = commandType;

        SqlCommandFunc?.Invoke(command);

        return await executionLogic.Invoke(command).ConfigureAwait(false);
    }

    // --- DataTable Reader ---

    public Task<DataTable> ExecuteDataTableAsync(string Query, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
        => ExecuteDataTableAsync(Query, null, commandType, cancellationToken);

    public async Task<DataTable> ExecuteDataTableAsync(string Query, SqlCommandDelegate SqlCommandFunc,
        CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cancellationToken, async cmd =>
        {
            DataTable dt = new DataTable();
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                dt.Load(reader);
            }

            return dt;

        }).ConfigureAwait(false);
    }

    // --- Execute NonQuery ---

    public Task<int> ExecuteNonQueryAsync(string Query, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
        => ExecuteNonQueryAsync(Query, null, commandType, cancellationToken);

    public Task<int> ExecuteNonQueryAsync(string Query, SqlCommandDelegate SqlCommandFunc, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
        => ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cancellationToken, cmd => cmd.ExecuteNonQueryAsync(cancellationToken));

    // --- Execute Scalar ---

    public async Task<T?> ExecuteScalarAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, 
        CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cancellationToken,
            cmd => cmd.ExecuteScalarAsync(cancellationToken));

        if (result is null or DBNull) return default;
        return (T)Convert.ChangeType(result, typeof(T));
    }

    public Task<T?> ExecuteScalarAsync<T>(string Query, CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
        => ExecuteScalarAsync<T>(Query, null, commandType, cancellationToken);

    // --- Generic Reader (Instance Mapper) ---

    /// <summary>
    /// Executing the query asynchronously to read and return a single record or null.
    /// </summary>
    public async Task<T> ExecuteReaderAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, Func<SqlDataReader, T> Instance, 
        CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default) where T : class
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cancellationToken,async cmd =>
        {
            using SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return Instance.Invoke(reader);
            }

            return null;

        }).ConfigureAwait(false);
    }

    // --- List Reader ---

    public async Task<List<T>> ExecuteListAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, Func<SqlDataReader, T> Instance,
        CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cancellationToken,async cmd =>
        {
            List<T> list = new List<T>();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(Instance(reader));
            }

            return list;
        }).ConfigureAwait(false);
    }

    public Task<List<T>> ExecuteListAsync<T>(string Query, Func<SqlDataReader, T> Instance,
        CommandType commandType = CommandType.Text, CancellationToken cancellationToken = default)
        => ExecuteListAsync<T>(Query, null, Instance, commandType, cancellationToken);
}
