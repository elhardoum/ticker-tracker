using System;
using System.Data.SqlClient;

namespace TickerTracker.Models
{
    public class Database
    {
        private static readonly Database _instance = new Database();
        public delegate void QueryCallback<SqlConnection>(SqlConnection conn);

        private Database()
        {
        }

        public static Database Instance()
        {
            return _instance;
        }

        private string getConnectionString()
        {
            return String.Format("Server={0},{1};Database={2};User Id={3};Password={4}",
                Util.getEnv("DB_HOST", "mssql"),
                Util.getEnv("DB_PORT", "1433"),
                Util.getEnv("DB_NAME", "TickerTracker"),
                Util.getEnv("SA_USER", "SA"),
                Util.getEnv("SA_PASSWORD"));
        }

        // @todo convert to async/await
        // https://stackoverflow.com/questions/23285753/how-to-await-on-async-delegate
        public static void Query(QueryCallback<SqlConnection> callback)
        {
            using (var conn = new SqlConnection(Instance().getConnectionString()))
            {
                try
                {
                    conn.Open();
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Database connection error: {0}, {1}", e.ToString(), e.Message);
                    return;
                }

                callback(conn);
            }
        }
    }
}
