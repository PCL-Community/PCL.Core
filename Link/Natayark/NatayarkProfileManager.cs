using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCL.Core.Link.Natayark
{
    public static class NatayarkProfileManager
    {
        public class NaidUser
        {
            public Int32 Id { get; set; }
            public string Email { get; set; }
            public string Username { get; set; }
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            /// <summary>
            /// Natayark ID 状态，1 为正常
            /// </summary>
            public int Status { get; set; }
            public bool IsRealname { get; set; }
            public string LastIp { get; set; }

        }

        public static NaidUser NaidProfile;
    }
}
