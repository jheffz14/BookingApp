using BookingAppV2.Connection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace BookingAppV2.Connection
{
  public class dbAccess
  {
              // removed static dependency on Connection_String and receive via DI
    private readonly string _connectionStringBK;


    public dbAccess(ConnectionOptions options)
    {
      _connectionStringBK = options.BookingApp ?? throw new ArgumentNullException(nameof(options.BookingApp));
    }

    public DataTable ExecuteQueryBooking(string query, List<OleDbParameter>? parameters = null)
    {
      DataTable dt = new DataTable();
      try
      {
        using (OleDbConnection conn = new OleDbConnection(_connectionStringBK))
        using (OleDbCommand cmd = new OleDbCommand(query, conn))
        {
          if (parameters != null && parameters.Count > 0)
            cmd.Parameters.AddRange(parameters.ToArray());

          conn.Open();
          using (OleDbDataReader reader = cmd.ExecuteReader())
          {
            dt.Load(reader);
          }
        }
      }
      catch (Exception ex)
      {
        // In development you can throw. In production consider logging.
        throw new Exception("Error executing query: " + ex.Message, ex);
      }
      return dt;
    }

    public int ExecuteNonQueryBooking(string query, List<OleDbParameter>? parameters = null)
    {
      int rowsAffected = 0;
      try
      {
        using (OleDbConnection conn = new OleDbConnection(_connectionStringBK))
        using (OleDbCommand cmd = new OleDbCommand(query, conn))
        {
          if (parameters != null && parameters.Count > 0)
            cmd.Parameters.AddRange(parameters.ToArray());

          conn.Open();
          rowsAffected = cmd.ExecuteNonQuery();
        }
      }
      catch (Exception ex)
      {
        throw new Exception("Error executing non-query: " + ex.Message, ex);
      }
      return rowsAffected;
    }
  }
}
