using Npgsql;
using System.Data;

namespace LoyaltyAPI.Helpers
{
    public static class DatabaseConnectionHelper
    {
        public static IDbConnection CreateConnection(string connectionString, ILogger logger)
        {
            try
            {
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();
                
                // Log thông tin connection
                var host = connection.Host;
                var port = connection.Port;
                var database = connection.Database;
                
                logger.LogInformation("✓ Database connected successfully - Host: {Host}, Port: {Port}, Database: {Database}", 
                    host, port, database);
                
                return connection;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "✗ Failed to connect to database");
                throw;
            }
        }
    }
}
