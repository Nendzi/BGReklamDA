using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace forgesample.Models
{
    public class UserModel
    {
        string UserId { get; set; }
        public string UserName { get; set; }
        public string EncriptedPassword { get; set; }
        public string ForgeClient { get; set; }
        public string ForgeSecret { get; set; }
    }
}
