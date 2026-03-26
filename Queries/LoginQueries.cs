using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookingAppV2.Queries
{
    public class LoginQueries
    {
        public static string LoginUser()
        {
            string query = @"Select * from UsersTable where user_name =@user_name and pass_word = @pass_word";
            return query;
        }

    }
}
