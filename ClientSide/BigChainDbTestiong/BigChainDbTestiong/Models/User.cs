using System;
using System.Collections.Generic;
using System.Text;

namespace BigChainDbTestiong.Models
{
    public class User
    {
        public string userId { get; set; }
        //public string PHN { get; set; }
        public string keyId { get; set; } // DEBUG: might need to be private
        public string fpId { get; set; }
        public string fpData { get; set; } // demo: HEX, 440: img

        public User()
        {
            var random = new Random();
            var userId = random.Next(40, 10000);
        }
    }
}
