using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Data;
using System.Data.SqlClient;

namespace CPSSMSToolboxExtension
{
    public static class Executor
    {
        public static DataTable GetDataTable(INodeInformation table, string SQL)
        {
            DataTable dt = new DataTable();

            using (SqlConnection connection = (SqlConnection)table.Connection.CreateConnectionObject())
            {
                connection.Open();
                connection.ChangeDatabase(table.Parent.Name);
                using (SqlCommand command = new SqlCommand(SQL, connection))
                {
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    da.Fill(dt);
                }
            }

            return dt;
        }

        public static SqlDataReader GetSqlDataReader(INodeInformation table, string SQL)
        {
            SqlDataReader dr = null;

            using (SqlConnection connection = (SqlConnection)table.Connection.CreateConnectionObject())
            {
                connection.Open();
                connection.ChangeDatabase(table.Parent.Name);
                using (SqlCommand command = new SqlCommand(SQL, connection))
                {
                    dr = command.ExecuteReader();
                }
            }

            return dr;
        }

        public static object GetSqlScalar(INodeInformation table, string SQL, bool changeDatabase = true)
        {
            using (SqlConnection connection = (SqlConnection)table.Connection.CreateConnectionObject())
            {
                connection.Open();
                if (changeDatabase)
                    connection.ChangeDatabase(table.Parent.Name);

                using (SqlCommand command = new SqlCommand(SQL, connection))
                {
                    return command.ExecuteScalar();
                }
            }
        }
    }
}
