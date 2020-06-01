using System;
using System.Collections.Generic;
using System.Text;

namespace BigChainDbTestiong.Models
{
    public class UserListAsset
    {

        public int randomID { get; set; }
        public string data { get; set; }
        public string type { get; set; }

        public UserListAsset()
        {
            type = "User List";
            var random = new Random();
            randomID = random.Next(40, 10000);
        }

    }

    public class UserListMetadata
    {
        public Dictionary<string, object> accessList { get; set; }

        public UserListMetadata()
        {
            accessList = new Dictionary<string, object>(); // user_id: string, user_data: object 
        }
    }
}
