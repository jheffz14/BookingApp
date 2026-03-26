using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookingAppV2.Helpers
{
    public class GetUserByName
    {
        public static string GetUserName() {
            string query = @"Select * from UsersTable where user_name=@user_name";
            return query;
        }

    }
}
