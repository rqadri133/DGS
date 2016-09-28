using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Web;

/** This data layer is being added to get rid of EntityFrame work  and for improve sync security model between 
 * BLOB Storage Connection 
 * ***/
namespace DGS.Models.DataLayer
{
    interface IConnection
    {

        System.Data.IDbConnection  Connect(string dbtype);
        int ConnectSqlServer();

    }




    /// <summary>
    /// only handling sql server 2012 no abstraction at this point
    /// </summary>
    public class DBSQLConnection : IConnection
    {

        SqlConnection _sqlConnection = null;
         
       


        public System.Data.IDbConnection  Connect(string dbtype)
        {
            int _connected = -111;
            string connection = System.Configuration.ConfigurationManager.ConnectionStrings["DGS"].ConnectionString;
            System.Data.IDbConnection dbConnection = null;

            try
            {
                switch (dbtype)
                {
                    case "sqlserver":
                        dbConnection = new SqlConnection(connection);
                        System.Threading.Thread.Sleep(100);
                        dbConnection.Open();
            
                       
                        break;
                    case "oracle":
                        dbConnection = new System.Data.OleDb.OleDbConnection(connection);
                        break;

                }

                if (dbConnection.State == System.Data.ConnectionState.Closed || dbConnection.State == System.Data.ConnectionState.Broken)
                    dbConnection.Open();

                if (dbConnection.State == System.Data.ConnectionState.Open)
                {
                    _connected = 1;
                }


            }
            catch (System.Data.DataException ex)
            {



            }

            catch (System.Data.SqlClient.SqlException ex)
            {



            }

            return  dbConnection;  

        }


       public int ConnectSqlServer( )
       {
           int _connected = -111;
           System.Data.IDbConnection dbConnection = null;

               try
               {
                       dbConnection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DGS"].ConnectionString);
               
               

               if (dbConnection.State == System.Data.ConnectionState.Closed || dbConnection.State == System.Data.ConnectionState.Broken)
               {
                   dbConnection.Open();
               }
                System.Threading.Thread.Sleep(50);
                if (dbConnection.State == System.Data.ConnectionState.Open)
                {
                    _connected = 1;
                }


           }
           catch(System.Data.DataException ex)
           {



           }

           catch (System.Data.SqlClient.SqlException ex)
           {



           }

           return _connected;

       }



    }









}