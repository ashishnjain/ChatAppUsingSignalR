using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SignalRChat.Models
{
    public class Users
    {
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int IsRead { get; set; }
        public string UserImage { get; set; }
        public string LoginTime { get; set; }
        public bool IsOnline { get; set; }
        public string RecentMsg { get; set; }
        public DateTime? RecentMsgTime { get; set; }
        public bool IsPin { get; set; }
    }

    public class ResultBE
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public string ResponseCode { get; set; }
        public string Message { get; set; }
        public string ResponseStatus { get; set; }
    }

    public class Registration
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class Groups
    {
        public int ID { get; set; }
        public string GroupName { get; set; }
        public string UserIds { get; set; }
        public int CreatedBy { get; set; }
        public string RecentMsg { get; set; }
        public DateTime? RecentMsgTime { get; set; }
        public string UserName { get; set; }
        public bool IsPin { get; set; }
    }

    public class GroupBE
    {
        public Groups Group { get; set; }
        public List<UserBE> AllUsers { get; set; }
    }

    public class UserBE
    {
        public string UserName { get; set; }
        public int UserId { get; set; }
        public bool Selected { get; set; }
    }
}