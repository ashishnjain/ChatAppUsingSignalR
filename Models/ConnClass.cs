using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace SignalRChat.Models
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ConnClass
    {
        public SqlCommand cmd = new SqlCommand();
        public SqlDataAdapter sda;
        public SqlDataReader sdr;
        public DataSet ds = new DataSet();
        public SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["conStr"].ToString());

        public bool IsExist(string Query)
        {
            bool check = false;
            using (cmd = new SqlCommand(Query, con))
            {
                con.Open();
                sdr = cmd.ExecuteReader();
                if (sdr.HasRows)
                    check = true;
            }
            sdr.Close();
            con.Close();
            return check;

        }

        public bool ExecuteQuery(string Query)
        {
            int j = 0;
            using (cmd = new SqlCommand(Query, con))
            {
                con.Open();
                j = cmd.ExecuteNonQuery();
                con.Close();
            }

            if (j > 0)
                return true;
            else
                return false;

        }

        public int ExecuteChatQuery(string Query)
        {
            Query = Query + Environment.NewLine + "SELECT SCOPE_IDENTITY()";
            int j = 0;
            using (cmd = new SqlCommand(Query, con))
            {
                con.Open();
                j = Convert.ToInt32(cmd.ExecuteScalar());
                con.Close();
            }

            return j;
        }

        public string GetColumnVal(string Query, string ColumnName)
        {
            string RetVal = "";
            using (cmd = new SqlCommand(Query, con))
            {
                con.Open();
                sdr = cmd.ExecuteReader();
                while (sdr.Read())
                {
                    RetVal = sdr[ColumnName].ToString();
                    break;
                }
                sdr.Close();
                con.Close();
            }

            return RetVal;


        }

        public DataTable GetList(string Query)
        {
            DataTable dt = new DataTable();

            using (cmd = new SqlCommand(Query, con))
            {
                con.Open();
                SqlDataAdapter adpt = new SqlDataAdapter(cmd);
                adpt.Fill(dt);
                con.Close();
            }

            return dt;
        }

        public DataTable GetRecentUsers(int UserId)
        {
            DataTable dt = new DataTable();

            using (cmd = new SqlCommand("SP_GetRecentUsers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", UserId);
                con.Open();
                SqlDataAdapter adpt = new SqlDataAdapter(cmd);
                adpt.Fill(dt);
                con.Close();
            }

            return dt;
        }

        public DataSet GetAllUsers(int UserId)
        {
            DataSet ds = new DataSet();

            using (cmd = new SqlCommand("SP_GetAllUsers", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", UserId);
                con.Open();
                SqlDataAdapter adpt = new SqlDataAdapter(cmd);
                adpt.Fill(ds);
                con.Close();
            }

            return ds;
        }

        public bool PinUnpinUser(int UserId, int PinPerson_Id, bool IsPined, bool IsGroup)
        {
            try
            {
                using (cmd = new SqlCommand("SP_SavePinUnpinDetail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserId", UserId);
                    cmd.Parameters.AddWithValue("@PinPersonId", PinPerson_Id);
                    cmd.Parameters.AddWithValue("@IsPined", IsPined);
                    cmd.Parameters.AddWithValue("@IsGroup", IsGroup);
                    con.Open();
                    cmd.ExecuteNonQuery();
                    con.Close();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}