using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookingAppV2.Connection
{
  public static class Connection_String
  {
    // note: ensure Access Database Engine x64 is installed and platform target set to x64
    public static string connectionStringBooking =
        @"provider=microsoft.ace.oledb.16.0;data source=\\172.21.10.123\SHARERE\Database\BookingApp.accdb;Mode=share Deny None;";
    //public static string connectionStringBooking =
    //  @"Provider=Microsoft.ACE.OLEDB.16.0;Data Source=C:\inetpub\wwwroot\BookingProd\BookingApp.accdb;";
  }
}
