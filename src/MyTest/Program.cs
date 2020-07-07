using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Microsoft.SqlServer.Types;
using Microsoft.SqlServer.Types.Tests;

namespace sqlTestCpre
{
    class Program
    {
        const string connstr = @"Data Source=(localdb)\mssqllocaldb;Integrated Security=True;AttachDbFileName=";
        private static System.Data.SqlClient.SqlConnection conn;
        private static string path;
        public static string ConnectionString => connstr + path;

        public static void Init()
        {
            if (path == null)
            {
                path = Path.Combine(new FileInfo(typeof(Program).Assembly.Location).Directory.FullName,
                    "UnitTestData.mdf");
                CreateSqlDatabase(path);
                using (var conn = new System.Data.SqlClient.SqlConnection(connstr + path))
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = OgcConformanceMap.DropTables;
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = OgcConformanceMap.CreateTables;
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = OgcConformanceMap.CreateRows;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        
        private static void CreateSqlDatabase(string filename)
        {
            string databaseName = System.IO.Path.GetFileNameWithoutExtension(filename);
            if (File.Exists(filename))
                File.Delete(filename);
            if (File.Exists(filename.Replace(".mdf","_log.ldf")))
                File.Delete(filename.Replace(".mdf", "_log.ldf"));
            using (var connection = new System.Data.SqlClient.SqlConnection(
                @"Data Source=(localdb)\mssqllocaldb;Initial Catalog=master; Integrated Security=true;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        String.Format("CREATE DATABASE {0} ON PRIMARY (NAME={0}, FILENAME='{1}')", databaseName, filename);
                    command.ExecuteNonQuery();

                    command.CommandText =
                        String.Format("EXEC sp_detach_db '{0}', 'true'", databaseName);
                    command.ExecuteNonQuery();
                }
            }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            Init();
            
            using var client = new SqlConnection(ConnectionString);
            
            client.Open();

            var dataAdapter = new SqlDataAdapter("select top 100 * from geopoints", client);
            var dt = new DataTable();
            dataAdapter.Fill(dt);
        }
        
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == "Microsoft.SqlServer.Types, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" ||
               args.Name == "Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91" ||
               args.Name == "Microsoft.SqlServer.Types, Version=12.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91")
            return typeof(SqlGeography).Assembly;
            return null;
        }
    }
}