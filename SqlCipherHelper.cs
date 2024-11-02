
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace Database
{
    public class SqlCipherHelper : IDisposable
    {

        private SqliteConnection connection;
        private SqliteCommand command;
        private SqliteDataReader reader;
        private SqliteTransaction transaction;
        private string connectionString = "";

        private SqlCipherHelper()
        {
        }

        public SqlCipherHelper(string connectionString)
        {
            this.connectionString = connectionString;
        }


        /// <summary>
        /// Executes a query and returns a SqliteDataReader for reading the results.
        /// </summary>
        public SqliteDataReader ExecuteQuery(string query)
        {
            try
            {
                connection = new SqliteConnection(connectionString);
                connection.Open();
                command = connection.CreateCommand();
                command.CommandText = query;
                reader = command.ExecuteReader();

                return reader;
            }
            catch
            {
                Dispose();
                return null;

            }
            //using (var connection = new SqliteConnection(connectionString))
            //{
            //    connection.Open();
            //    using (var command = new SqliteCommand(query, connection))
            //    {
            //        return command.ExecuteReader();
            //    }
            //}
        }


        /// <summary>
        /// Asynchronously executes a query and returns a DbDataReader for reading the results.
        /// </summary>
        public async Task<SqliteDataReader> ExecuteQueryAsync(string query)
        {
            try
            {
                var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync();
                command = new SqliteCommand(query, connection);
                return await command.ExecuteReaderAsync() as SqliteDataReader;
            }
            catch
            {
                Dispose();
                return null;
            }

            //using (var connection = new SqliteConnection(connectionString))
            //{
            //    await connection.OpenAsync();
            //    using (var command = new SqliteCommand(query, connection))
            //    {
            //        return await command.ExecuteReaderAsync() as SqliteDataReader;
            //    }
            //}
        }

        /// <summary>
        /// Executes a no query and returns a int for reading the results.
        /// </summary>
        public int ExecuteNonQuery(string query)
        {
            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand(query, connection))
                    {
                        return command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }


        /// <summary>
        /// Asynchronously executes a no query and returns a int for reading the results.
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string query)
        {
            try
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqliteCommand(query, connection))
                    {
                        return await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Reads the full table synchronously.
        /// </summary>
        public SqliteDataReader ReadFullTable(string tableName)
        {
            string query = "select * from " + tableName;
            return ExecuteQuery(query);
        }

        /// <summary>
        /// Asynchronously reads the full table.
        /// </summary>
        public async Task<SqliteDataReader> ReadFullTableAsync(string tableName)
        {
            string query = "select * from " + tableName;
            return await ExecuteQueryAsync(query) as SqliteDataReader;
        }

        /// <summary>
        /// Begins a transaction, executes the given query, and commits the transaction.
        /// </summary>
        public bool BeginTransaction(string query)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SqliteCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                            transaction.Commit();
                            return true;
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously begins a transaction, executes the given query, and commits the transaction.
        /// </summary>
        public async Task<bool> BeginTransactionAsync(string query)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction as SqliteTransaction;
                            command.CommandText = query;
                            await command.ExecuteNonQueryAsync();
                        }
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously begins a transaction, executes multiple queries, and commits the transaction.
        /// </summary>
        public async Task<bool> BeginTransactionAsync(List<string> queries)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.Transaction = transaction as SqliteTransaction;

                            foreach (var query in queries)
                            {
                                command.CommandText = query;
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        await transaction.CommitAsync();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Selects rows from a table where the id are met.
        /// </summary>
        public SqliteDataReader Select(string tableName, int id)
        {
            string query = "select * from " + tableName + " where id=" + id;
            return ExecuteQuery(query);
        }

        /// <summary>
        /// Selects rows from a table where the id are met.
        /// </summary>
        public async Task<SqliteDataReader> SelectAsync(string tableName, int id)
        {
            string query = "select * from " + tableName + " where id=" + id;
            return await ExecuteQueryAsync(query);
        }

        /// <summary>
        /// Inserts values into the specified table.
        /// </summary>
        public int Insert(string tableName, string[] values)
        {
            string query = $"insert into {tableName} values ('{string.Join("', '", values)}')";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously inserts values into the specified table.
        /// </summary>
        public async Task<int> InsertAsync(string tableName, string[] values)
        {
            string query = $"insert into {tableName} values ('{string.Join("', '", values)}')";
            return await ExecuteNonQueryAsync(query);
        }

        /// <summary>
        /// Inserts specific columns into the specified table.
        /// </summary>
        public int Insert(string tableName, string[] cols, string[] values)
        {
            if (cols.Length != values.Length)
            {
                throw new ArgumentException("The length of columns and values must be equal.");
            }

            string query = $"insert into {tableName} ({string.Join(", ", cols)}) values ('{string.Join("', '", values)}')";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously inserts specific columns into the specified table.
        /// </summary>
        public async Task<int> InsertAsync(string tableName, string[] cols, string[] values)
        {
            if (cols.Length != values.Length)
            {
                throw new ArgumentException("The length of columns and values must be equal.");
            }

            string query = $"insert into {tableName} ({string.Join(", ", cols)}) values ('{string.Join("', '", values)}')";
            return await ExecuteNonQueryAsync(query);
        }

        /// <summary>
        /// Updates specific columns in the specified table.
        /// </summary>
        public int Update(string tableName, string[] cols, string[] colValues, string selectKey, string selectValue)
        {
            string query = $"UPDATE {tableName} SET {cols[0]} = '{colValues[0]}'";

            for (int i = 1; i < colValues.Length; i++)
            {
                query += $", {cols[i]} = '{colValues[i]}'";
            }

            query += $" WHERE {selectKey} = '{selectValue}'";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously updates specific columns in the specified table.
        /// </summary>
        public async Task<int> UpdateAsync(string tableName, string[] cols, string[] colValues, string selectKey, string selectValue)
        {
            string query = $"update {tableName} set {cols[0]} = '{colValues[0]}'";
            for (int i = 1; i < colValues.Length; i++)
            {
                query += $", {cols[i]} = '{colValues[i]}'";
            }
            query += $" where {selectKey} = '{selectValue}'";

            return await ExecuteNonQueryAsync(query);
        }

        /// <summary>
        /// Deletes specific rows from the specified table.
        /// </summary>
        public int Delete(string tableName, string[] cols, string[] colValues)
        {
            string query = $"DELETE FROM {tableName} WHERE {cols[0]} = '{colValues[0]}'";

            for (int i = 1; i < colValues.Length; i++)
            {
                query += $" OR {cols[i]} = '{colValues[i]}'";
            }

            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously deletes specific rows from the specified table.
        /// </summary>
        public async Task<int> DeleteAsync(string tableName, string[] cols, string[] colValues)
        {
            string query = $"DELETE FROM {tableName} WHERE {cols[0]} = '{colValues[0]}'";

            for (int i = 1; i < colValues.Length; i++)
            {
                query += $" OR {cols[i]} = '{colValues[i]}'";
            }

            return await ExecuteNonQueryAsync(query);
        }


        /// <summary>
        /// Deletes all rows from the specified table.
        /// </summary>
        public int Delete(string tableName)
        {
            string query = $"delete from {tableName}";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously deletes all rows from the specified table.
        /// </summary>
        public async Task<int> DeleteContentsAsync(string tableName)
        {
            string query = $"delete from {tableName}";
            return await ExecuteNonQueryAsync(query);
        }

        /// <summary>
        /// Creates a table with the specified columns and their data types.
        /// </summary>
        public int CreateTable(string name, string[] cols, string[] colTypes)
        {
            if (cols.Length != colTypes.Length)
            {
                throw new ArgumentException("The length of columns and column types must be equal.");
            }

            string query = $"CREATE TABLE {name} ({cols[0]} {colTypes[0]}";
            for (int i = 1; i < cols.Length; i++)
            {
                query += $", {cols[i]} {colTypes[i]}";
            }
            query += ")";

            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Asynchronously creates a table with the specified columns and their data types.
        /// </summary>
        public async Task<int> CreateTableAsync(string name, string[] cols, string[] colTypes)
        {
            if (cols.Length != colTypes.Length)
            {
                throw new ArgumentException("The length of columns and column types must be equal.");
            }

            string query = $"CREATE TABLE {name} ({cols[0]} {colTypes[0]}";

            for (int i = 1; i < cols.Length; i++)
            {
                query += $", {cols[i]} {colTypes[i]}";
            }

            query += ")";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(query, connection))
                {
                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// Selects rows from a table where the conditions are met.
        /// </summary>
        public SqliteDataReader SelectWhere(string tableName, string[] items, string[] cols, string[] operations, string[] values)
        {
            if (cols.Length != operations.Length || operations.Length != values.Length)
            {
                throw new ArgumentException("The length of columns, operations, and values must be equal.");
            }

            string query = $"SELECT {items[0]}";
            for (int i = 1; i < items.Length; i++)
            {
                query += $", {items[i]}";
            }

            query += $" FROM {tableName} WHERE {cols[0]} {operations[0]} '{values[0]}'";
            for (int i = 1; i < cols.Length; i++)
            {
                query += $" AND {cols[i]} {operations[i]} '{values[i]}'";
            }

            return ExecuteQuery(query);
        }


        /// <summary>
        /// Asynchronously selects rows from a table where the conditions are met.
        /// </summary>
        public async Task<SqliteDataReader> SelectWhereAsync(string tableName, string[] items, string[] cols, string[] operations, string[] values)
        {
            if (cols.Length != operations.Length || operations.Length != values.Length)
            {
                throw new ArgumentException("The length of columns, operations, and values must be equal.");
            }

            string query = $"SELECT {items[0]}";
            for (int i = 1; i < items.Length; i++)
            {
                query += $", {items[i]}";
            }

            query += $" FROM {tableName} WHERE {cols[0]} {operations[0]} '{values[0]}'";
            for (int i = 1; i < cols.Length; i++)
            {
                query += $" AND {cols[i]} {operations[i]} '{values[i]}'";
            }

            return (SqliteDataReader)await ExecuteQueryAsync(query);
        }

        public void Dispose()
        {
            transaction?.Dispose();
            transaction = null;

            reader?.Close();
            reader?.Dispose();
            reader = null;

            command?.Dispose();
            command = null;

            connection?.Close();
            connection = null;

        }
    }
}