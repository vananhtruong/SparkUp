using System;
using System.Data.SqlClient;

namespace DatabaseCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "server=db16450.public.databaseasp.net;database=db16450;uid=db16450;pwd=12345678;TrustServerCertificate=True;";
            
            Console.WriteLine("Starting database cleanup...");
            
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connected to database.");

                    // First drop all foreign keys
                    string dropFKScript = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += 'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + '.' +
                            QUOTENAME(OBJECT_NAME(parent_object_id)) + ' DROP CONSTRAINT ' +
                            QUOTENAME(name) + ';'
                        FROM sys.foreign_keys;
                        EXEC sp_executesql @sql;
                    ";

                    using (SqlCommand command = new SqlCommand(dropFKScript, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Foreign keys dropped.");
                    }

                    // Then drop all tables except __EFMigrationsHistory
                    string dropTablesScript = @"
                        DECLARE @sql NVARCHAR(MAX) = N'';
                        SELECT @sql += 'DROP TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(object_id)) + '.' +
                            QUOTENAME(name) + ';'
                        FROM sys.tables
                        WHERE name != '__EFMigrationsHistory';
                        EXEC sp_executesql @sql;
                    ";

                    using (SqlCommand command = new SqlCommand(dropTablesScript, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("Tables dropped.");
                    }

                    // Finally drop the migrations history table
                    string dropMigrationsHistoryScript = @"
                        IF OBJECT_ID('__EFMigrationsHistory') IS NOT NULL
                            DROP TABLE __EFMigrationsHistory;
                    ";

                    using (SqlCommand command = new SqlCommand(dropMigrationsHistoryScript, connection))
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine("__EFMigrationsHistory table dropped.");
                    }

                    Console.WriteLine("Database cleanup completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
