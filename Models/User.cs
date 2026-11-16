using System;
using System.ComponentModel;

namespace scholarship_Application.Models
{
    public class User
    {
        public int UserId { get; set; }

        // navigation property ulit, naka false 'yung browsable attribute kasi hindi 'to dapat nakikita sa datagridview
        [Browsable(false)]
        public int RoleId { get; set; }
        public string RoleName { get; set; }

        public string Username { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        [Browsable(false)]
        public string Password { get; set; }

        [Browsable(false)]
        public UserRole Role
        {
            get { return (UserRole)RoleId; }
            set { RoleId = (int)value; }
        }
    }
}
