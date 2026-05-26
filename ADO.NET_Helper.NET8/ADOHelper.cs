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

    private async Task<T> ExecuteInternalAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, CommandType commandType, Func<SqlCommand, Task<T>> executionLogic)
    {
        using SqlConnection connection = new SqlConnection(_connectionString);

        await connection.OpenAsync().ConfigureAwait(false);

        using SqlCommand command = new SqlCommand(Query, connection);

        command.CommandType = commandType;

        SqlCommandFunc?.Invoke(command);

        return await executionLogic.Invoke(command).ConfigureAwait(false);
    }

    // --- DataTable Reader ---

    public Task<DataTable> ExecuteDataTableAsync(string Query, CommandType commandType = CommandType.Text)
        => ExecuteDataTableAsync(Query, null, commandType);

    public async Task<DataTable> ExecuteDataTableAsync(string Query, SqlCommandDelegate SqlCommandFunc, CommandType commandType = CommandType.Text)
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, async cmd =>
        {
            DataTable dt = new DataTable();
            using (SqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
            {
                dt.Load(reader);
            }

            return dt;

        }).ConfigureAwait(false);
    }

    // --- Execute NonQuery ---

    public Task<int> ExecuteNonQueryAsync(string Query, CommandType commandType = CommandType.Text)
        => ExecuteNonQueryAsync(Query, null, commandType);

    public Task<int> ExecuteNonQueryAsync(string Query, SqlCommandDelegate SqlCommandFunc, CommandType commandType = CommandType.Text)
        => ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cmd => cmd.ExecuteNonQueryAsync());

    // --- Execute Scalar ---

    public Task<object> ExecuteScalarAsync(string Query, SqlCommandDelegate SqlCommandFunc, CommandType commandType = CommandType.Text)
       => ExecuteInternalAsync(Query, SqlCommandFunc, commandType, cmd => cmd.ExecuteScalarAsync());

    public Task<object> ExecuteScalarAsync(string Query, CommandType commandType = CommandType.Text)
        => ExecuteScalarAsync(Query, null, commandType);

    // --- Generic Reader (Instance Mapper) ---

    public async Task<T> ExecuteReaderAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, Func<SqlDataReader, T> Instance, CommandType commandType = CommandType.Text) where T : class
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, async cmd =>
        {
            using SqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

            if (await reader.ReadAsync().ConfigureAwait(false))
            {
                return Instance.Invoke(reader);
            }

            return null;

        }).ConfigureAwait(false);
    }

    // --- List Reader ---

    public async Task<List<T>> ExecuteListAsync<T>(string Query, SqlCommandDelegate SqlCommandFunc, Func<SqlDataReader, T> Instance, CommandType commandType = CommandType.Text)
    {
        return await ExecuteInternalAsync(Query, SqlCommandFunc, commandType, async cmd =>
        {
            List<T> list = new List<T>();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                list.Add(Instance(reader));
            }

            return list;
        }).ConfigureAwait(false);
    }

    public Task<List<T>> ExecuteListAsync<T>(string Query, Func<SqlDataReader, T> Instance, CommandType commandType = CommandType.Text)
        => ExecuteListAsync<T>(Query, null, Instance, commandType);
}
