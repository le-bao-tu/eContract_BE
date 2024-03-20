using NetCore.Shared;
using Serilog;
using System;
using System.Data;
using System.Data.SqlClient;

namespace NetCore.Business
{
    public class CallStoreHelper : ICallStoreHelper
    {
        public Response CallStoreWithStartAndEndDateAsync(string storeName, DateTime startDate, DateTime endDate)
        {
            try
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter da = new SqlDataAdapter();
                DataTable dt = new DataTable();
                var connectString = Utils.GetConfig("ConnectionString:MSSQLDatabase");
                SqlConnection connection = new SqlConnection(connectString);
                try
                {
                    cmd = new SqlCommand(storeName, connection);
                    cmd.Parameters.Add(new SqlParameter("@StartDate", startDate.ToString("yyyy-MM-dd")));
                    cmd.Parameters.Add(new SqlParameter("@EndDate", endDate.ToString("yyyy-MM-dd")));
                    cmd.CommandType = CommandType.StoredProcedure;
                    da.SelectCommand = cmd;
                    da.Fill(dt);
                    da.Update(dt);

                    return new ResponseObject<DataTable>(dt, "Success", Code.Success);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "CallStore Error");
                    return new ResponseObject<DataTable>(null, ex.Message, Code.ServerError);
                }
                finally
                {
                    dt.Dispose();
                    da.Dispose();
                    cmd.Dispose();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CallStore Error");
                return new ResponseObject<DataTable>(null, ex.Message, Code.ServerError);
            }
        }
    }
}