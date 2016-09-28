using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using System.Data.Sql;
using System.Threading.Tasks;

namespace DGS.Models.DataLayer
{
    /*** wHY IS IT HERE ef REMOVED FROM cODE FROM SECURING SYNC TO BLOB REFERENCE **/

    public class ConnecterContainer
    {

        public string CommandText
        {
            get;
            set;

        }

        public IDbConnection ConnectionObject
        {
            get;
            set;

        }


        public string DBType
        {
            get;
            set;

        }

        public int ExecutionStatus
        {
            get;
            set;

        }

    }


    [Serializable]
    public class SqlConnecter
    {
        IDbConnection connection = null;
        IEnumerable<IDbConnection> connections = null;
        List<ConnecterContainer> connecters = null;
        

        
        public SqlConnecter(IDbConnection dbConnection)
        {

            connection = dbConnection;

            if (connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
            {
                connection.Open();

            }

        }
       

        public List<SqlParameter> Parameters
        {
            get;
            set;

        }


        public SqlConnecter (List<ConnecterContainer> lstConnections)
        {
            connecters = lstConnections;

        }


        public int ExecuteTranQuery(string commandText)
        {
            IDbCommand command = null;
            SqlDataAdapter adapter = null;
            int status = 0; 
            try
            {
                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandText;
                
                status = command.ExecuteNonQuery();
    

            }
            catch (Exception excp)
            {


            }
            return status;




        }

        public int ExecuteTranQuery(string commandText , List<IDbDataParameter> parameters)
        {
            IDbCommand command = null;
            SqlDataAdapter adapter = null;
            int status = 0;
            try
            {

                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                foreach(IDbDataParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);

                }
                command.CommandText = commandText;
                command.CommandTimeout = 
                status = command.ExecuteNonQuery();


            }
            catch (Exception excp)
            {


            }
            return status;




        }


        
        public List<DataRow>  ExecuteResultSet(string commandText)
        {

            List<DataRow> rows = null;

            IDbCommand command = null;
            SqlDataAdapter adapter = null;
            int status = 0;
            DataSet ds = new DataSet("DS");
            try
            {

                command = new SqlCommand();
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandText;

                adapter = new SqlDataAdapter((SqlCommand)command);
                adapter.Fill(ds);


            }
            catch (Exception excp)
            {


            }
            finally
            {


            }

            if(ds.Tables.Count >  0)
            {
                if(ds.Tables[0].Rows.Count > 0)
                {

                    rows = ds.Tables[0].AsEnumerable().ToList();

                }


            }
            return rows;





        }

        public List<DataRow> ExecuteResultSet(string commandText , List<IDbDataParameter> parameters )
        {

            List<DataRow> rows = null;

            SqlCommand command = null;
            SqlDataAdapter adapter = null;
            int status = 0;
            DataSet ds = new DataSet("DS");
            try
            {

                command = new SqlCommand();
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                command.Connection = (SqlConnection) connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandText;
                 foreach (IDbDataParameter parameter in parameters)
                 {
                     command.Parameters.Add(parameter);
                 }

                 adapter = new SqlDataAdapter();
                 adapter.SelectCommand = command;
                 
                 adapter.Fill(ds); 


            }
            catch (Exception excp)
            {


            }
            finally
            {


            }

            if (ds.Tables[0] != null)
            {
                if (ds.Tables[0].Rows.Count > 0)
                {

                    rows = ds.Tables[0].AsEnumerable().ToList();

                }


            }
            return rows;





        }


        public object ExecuteScalarResult(string commandText, List<IDbDataParameter> parameters)
        {

            object objResult = null;
            IDbCommand command = null;
            SqlDataAdapter adapter = null;
            int status = 0;
            DataSet ds = new DataSet("DS");
            try
            {

                command = new SqlCommand();
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = commandText;
                foreach (IDbDataParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }


                objResult = command.ExecuteScalar();
            }
            catch (Exception excp)
            {


            }
            finally
            {


            }



            return objResult;





        }


        #region "Close connection"
        public void Close()
        {
            if (this.connection != null)
            {
                if (this.connection.State == ConnectionState.Open)
                    this.connection.Close();
            }
        }
        #endregion

        #region "for parallel execution connections"
        public List<ConnecterContainer> ExecuteParallelQueries( )
        {
            // Execute multiple queries from Parallel Connections 

           // Parallel.ForEach()

            IDbCommand  command = null;
            SqlDataAdapter adapter = null;
            List<ConnecterContainer> container = new List<ConnecterContainer>();
            try
            {

                System.Threading.Tasks.Parallel.ForEach(connecters, connectionSingle =>
                {
                    // The more computational work you do here, the greater  
                    // the speedup compared to a sequential foreach loop. in parallel requests
                    if (connectionSingle.DBType == "sqlserver")
                    {
                        command = new SqlCommand();

                    }
                    else if (connectionSingle.DBType == "oracle")
                    {
                        command = new System.Data.OleDb.OleDbCommand();
                    }
                    command.CommandText = connectionSingle.CommandText;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Connection = connectionSingle.ConnectionObject;
                    connectionSingle.ExecutionStatus = command.ExecuteNonQuery();
                    container.Add(connectionSingle);
                }); //close lambda express


            }
            catch(Exception excp)
            {


            }


            return container;

        }
        #endregion


        // Load parallel procs and avoid ef 
        public List<ConnecterContainer> ExecuteParallelQueries(List<ConnecterContainer> connections )
        {
            // Execute multiple queries from Parallel Connections 
            // Parallel.ForEach()
            IDbCommand command = null;
            SqlDataAdapter adapter = null;
            List<ConnecterContainer> container = new List<ConnecterContainer>();
            try
            {

                System.Threading.Tasks.Parallel.ForEach(connections, connectionSingle =>
                {
                    // The more computational work you do here, the greater  
                    // the speedup compared to a sequential foreach loop. in parallel requests
                    command = new SqlCommand();
                    command.CommandText = connectionSingle.CommandText;
                    command.CommandType = CommandType.StoredProcedure;
                    command.Connection = connectionSingle.ConnectionObject;
                    connectionSingle.ExecutionStatus = command.ExecuteNonQuery();
                    container.Add(connectionSingle);
                }); //close lambda express


            }
            catch (Exception excp)
            {


            }


            return container;

        }
      

    }
}