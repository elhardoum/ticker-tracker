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
            return "Server=mssql,1433;Database=TickerTracker;User Id=SA;Password="
                + Environment.GetEnvironmentVariable("SA_PASSWORD");
        }

        public void Query(QueryCallback<SqlConnection> callback)
        {
            using (var conn = new SqlConnection(getConnectionString()))
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
