using Microsoft.AspNet.SignalR;
using SignalRChat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SignalRChat
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ChatHub : Hub
    {
        static List<Users> ConnectedUsers = new List<Users>();
        ConnClass ConnC = new ConnClass();

        public void Connect(string userName, int userId)
        {
            var id = Context.ConnectionId;
            var UsersAndGroups = ConnC.GetAllUsers(userId);
            var Users = UsersAndGroups.Tables[0];
            var MyGroup = UsersAndGroups.Tables[1];

            List<Users> AllUsers = new List<Users>();

            for (int i = 0; i < Users.Rows.Count; i++)
            {
                string UserImg = GetUserImage(Convert.ToString(Users.Rows[i]["UserName"]));
                string logintime = DateTime.Now.ToString();
                var RecentMsgTime = new Nullable<DateTime>();

                if (!string.IsNullOrEmpty(Convert.ToString(Users.Rows[i]["RecentTime"])))
                {
                    RecentMsgTime = Convert.ToDateTime(Convert.ToString(Users.Rows[i]["RecentTime"]));
                }

                AllUsers.Add(new Users
                {
                    ConnectionId = userId == Convert.ToInt32(Users.Rows[i]["ID"]) ? id : string.Empty,
                    UserId = Convert.ToInt32(Users.Rows[i]["ID"]),
                    UserName = Convert.ToString(Users.Rows[i]["UserName"]),
                    UserImage = UserImg,
                    LoginTime = logintime,
                    RecentMsgTime = !string.IsNullOrEmpty(Convert.ToString(Users.Rows[i]["RecentTime"])) ? RecentMsgTime : null,
                    IsRead = Convert.ToInt32(Users.Rows[i]["IsRead"]),
                    RecentMsg = Convert.ToString(Users.Rows[i]["Message"]),
                    IsOnline = ConnectedUsers.Where(x => x.UserId == Convert.ToInt32(Users.Rows[i]["ID"])).Any(),
                    IsPin = !string.IsNullOrEmpty(Users.Rows[i]["IsPin"].ToString()) ? Convert.ToBoolean(Users.Rows[i]["IsPin"]) : false
                });
            }

            List<Groups> AllGroups = new List<Groups>();

            for (int i = 0; i < MyGroup.Rows.Count; i++)
            {
                var RecentMsgTime = new Nullable<DateTime>();

                if (!string.IsNullOrEmpty(Convert.ToString(Users.Rows[i]["RecentTime"])))
                {
                    RecentMsgTime = Convert.ToDateTime(Convert.ToString(Users.Rows[i]["RecentTime"]));
                }

                AllGroups.Add(new Groups()
                {
                    ID = Convert.ToInt32(MyGroup.Rows[i]["ID"]),
                    GroupName = Convert.ToString(MyGroup.Rows[i]["GroupName"]),
                    CreatedBy = Convert.ToInt32(MyGroup.Rows[i]["CreatedBy"]),
                    RecentMsg = Convert.ToString(MyGroup.Rows[i]["Message"]),
                    RecentMsgTime = !string.IsNullOrEmpty(Convert.ToString(MyGroup.Rows[i]["RecentTime"])) ? RecentMsgTime : null,
                    UserName = Convert.ToString(MyGroup.Rows[i]["UserName"]),
                    IsPin = !string.IsNullOrEmpty(Users.Rows[i]["IsPin"].ToString()) ? Convert.ToBoolean(Users.Rows[i]["IsPin"]) : false
                });

                Groups.Add(Context.ConnectionId, Convert.ToString(MyGroup.Rows[i]["GroupName"]));
            }

            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                foreach (var item in ConnectedUsers.Where(x => x.UserId == userId).ToList()) { ConnectedUsers.Remove(item); }

                string UserImg = GetUserImage(userName);
                string logintime = DateTime.Now.ToString();

                ConnectedUsers.Add(new Users
                {
                    ConnectionId = id,
                    UserId = userId,
                    UserName = userName,
                    UserImage = UserImg,
                    LoginTime = logintime
                });
                // send to caller
                Clients.Caller.onConnected(userId, userName, AllUsers, AllGroups);

                // send to all except caller client
                Clients.AllExcept(id).onNewUserConnected(userId, userName, UserImg, logintime);
            }
            GetNotification(userId, 1);
        }

        public void ReConnect(string userName, int userId)
        {
            var id = Context.ConnectionId;

            if (ConnectedUsers.Count(x => x.ConnectionId == id) == 0)
            {
                foreach (var item in ConnectedUsers.Where(x => x.UserId == userId).ToList()) { ConnectedUsers.Remove(item); }

                string UserImg = GetUserImage(userName);
                string logintime = DateTime.Now.ToString();

                ConnectedUsers.Add(new Users
                {
                    ConnectionId = id,
                    UserId = userId,
                    UserName = userName,
                    UserImage = UserImg,
                    LoginTime = logintime
                });

                Clients.AllExcept(id).onNewUserConnected(id, userName, UserImg, logintime);
            }
        }

        public string GetUserImage(string username)
        {
            string RetimgName = "/images/dummy.png";
            try
            {
                string query = "select Photo from tbl_Users where UserName='" + username + "'";
                string ImageName = ConnC.GetColumnVal(query, "Photo");

                if (ImageName != "")
                    RetimgName = "/images/DP/" + ImageName;
                if (!System.IO.File.Exists(RetimgName))
                {
                    RetimgName = "/images/dummy.png";
                }
            }
            catch (Exception ex)
            { }
            return RetimgName;
        }

        public override System.Threading.Tasks.Task OnDisconnected(bool stopCalled)
        {
            var item = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            if (item != null)
            {
                ConnectedUsers.Remove(item);

                var id = Context.ConnectionId;
                Clients.All.onUserDisconnected(item.UserId, item.UserName);

            }
            return base.OnDisconnected(stopCalled);
        }

        public void SendPrivateMessage(int toUserId, int fromUserId, string message)
        {
            var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == fromUserId);

            string CurrentDateTime = DateTime.Now.ToString();
            string UserImg = GetUserImage(fromUser.UserName);

            // SAVE MSG TO DB
            string Query = "insert into tbl_Chat(SenderId,ReceiverId,Message,DateTime)Values(" + fromUser.UserId + "," + toUserId + ",N'" + message + "', GETDATE())";
            int MessageId = ConnC.ExecuteChatQuery(Query);

            // send to caller user
            Clients.Caller.sendPrivateMessage(toUserId, fromUser.UserName, message, UserImg, CurrentDateTime, MessageId);

            Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
            string Notification = tagRegex.IsMatch(message) ? fromUser.UserName + " shared file with you." : "New message from " + fromUser.UserName;

            if (toUser != null && fromUser != null && fromUser.UserId != toUser.UserId)
            {
                // send to 
                Clients.Client(toUser.ConnectionId).sendPrivateMessage(fromUser.UserId, fromUser.UserName, message, UserImg, CurrentDateTime, MessageId);
                Clients.Client(toUser.ConnectionId).sendNotification(fromUser.UserId, fromUser.UserName, Notification, UserImg, CurrentDateTime);
            }

            if (toUserId != fromUserId)
            {
                Query = "insert into tbl_Notification(Notification,UserId,Is_Read,Datetime)Values('" + Notification + "'," + toUserId + ",0, GETDATE())";
                ConnC.ExecuteQuery(Query);
            }
            else
            {
                ConnC.ExecuteQuery("update tbl_Chat set IsRead=1 where SenderId=" + toUserId + " and ReceiverId=" + fromUserId);
            }
        }

        public void GetPrivateMessage(int toUserId, int fromUserId)
        {
            var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);
            ConnC.ExecuteQuery("update tbl_Chat set IsRead=1 where SenderId=" + toUserId + " and ReceiverId=" + fromUserId);

            var Messages = ConnC.GetList("select * from tbl_Chat where ((SenderId=" + toUserId + " and ReceiverId=" + fromUserId + ") OR " +
                "(SenderId=" + fromUserId + " and ReceiverId=" + toUserId + ")) and COALESCE(IsDeleted,0)=0");

            List<Messages> AllMessages = new List<Messages>();

            for (int i = 0; i < Messages.Rows.Count; i++)
            {
                string Query = "select * from tbl_Users where ID=" + Convert.ToInt32(Messages.Rows[i]["SenderId"]);
                string userName = string.Empty;
                string UserImg = string.Empty;
                if (ConnC.IsExist(Query))
                {
                    userName = ConnC.GetColumnVal(Query, "UserName");
                    UserImg = GetUserImage(userName);
                }

                AllMessages.Add(
                    new Messages
                    {
                        MessageId = Convert.ToInt32(Messages.Rows[i]["ID"]),
                        Message = Convert.ToString(Messages.Rows[i]["Message"]),
                        UserName = userName,
                        UserImage = UserImg,
                        Time = Messages.Rows[i]["DateTime"].ToString(),
                        IsRead = !string.IsNullOrEmpty(Messages.Rows[i]["IsRead"].ToString()) ? Convert.ToBoolean(Messages.Rows[i]["IsRead"]) : false,
                        IsEdited = !string.IsNullOrEmpty(Messages.Rows[i]["ModifiedDate"].ToString()) ? true : false
                    });
            }

            Clients.Caller.GetPrivateMessage(AllMessages);

            if (toUser != null)
            {
                Clients.Client(toUser.ConnectionId).ReadMessage(fromUserId);
            }
        }

        public void ReadMessage(int toUserId, int fromUserId)
        {
            ConnC.ExecuteQuery("update tbl_Chat set IsRead=1 where SenderId=" + toUserId + " and ReceiverId=" + fromUserId);
            var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);
            Clients.Client(toUser.ConnectionId).ReadMessage(fromUserId);
        }

        public void ReadNotification(int NotiId, int userId)
        {
            ConnC.ExecuteQuery("update tbl_Notification set Is_Read=1 where ID=" + NotiId);
            GetNotification(userId, 2);
        }

        public void GetNotification(int userId, int flag)
        {
            var Notification = ConnC.GetList("select * from tbl_Notification where UserId=" + userId);

            List<dynamic> AllNotification = new List<dynamic>();

            for (int i = 0; i < Notification.Rows.Count; i++)
            {
                AllNotification.Add(
                    new
                    {
                        ID = Convert.ToInt32(Notification.Rows[i]["ID"]),
                        Notification = Notification.Rows[i]["Notification"].ToString(),
                        Is_Read = Convert.ToBoolean(Notification.Rows[i]["Is_Read"])
                    });
            }

            Clients.Caller.GetNotification(AllNotification.OrderByDescending(x => x.ID), flag);
        }

        public void DeleteMessage(int toUserId, int fromUserId, int MessageId)
        {
            ConnC.ExecuteQuery("update tbl_Chat set IsDeleted=1, ModifiedDate=GETDATE() where ID=" + MessageId);

            var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == fromUserId);

            if (toUser != null && fromUser != null && fromUser.UserId != toUser.UserId)
            {
                // send to 
                Clients.Client(toUser.ConnectionId).sendDeletedMessage(MessageId);
            }
        }

        public void EditMessage(int toUserId, int fromUserId, string Message, int MessageId)
        {
            ConnC.ExecuteQuery("update tbl_Chat set Message=N'" + Message + "', ModifiedDate=GETDATE() where ID=" + MessageId);

            var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == fromUserId);

            string CurrentDateTime = DateTime.Now.ToString();
            string UserImg = GetUserImage(fromUser.UserName);

            Clients.Caller.sendEditedMessage(toUserId, fromUser.UserName, Message, UserImg, CurrentDateTime, MessageId);
            if (toUser != null && fromUser != null && fromUser.UserId != toUser.UserId)
            {
                // send to 
                Clients.Client(toUser.ConnectionId).sendEditedMessage(fromUser.UserId, fromUser.UserName, Message, UserImg, CurrentDateTime, MessageId);
            }
        }

        public void GetGroups(int fromUserId)
        {
            var MyGroup = ConnC.GetList("SELECT * FROM tbl_Group WHERE (UserIds = '" + fromUserId + "' OR UserIds LIKE '" + fromUserId + "' + ',%' OR UserIds LIKE '%,' + '" + fromUserId + "' + ',%' OR UserIds LIKE '%,' + '" + fromUserId + "') AND COALESCE(IsDeleted,0)=0");
            List<Groups> AllGroups = new List<Groups>();

            for (int i = 0; i < MyGroup.Rows.Count; i++)
            {
                AllGroups.Add(new Groups()
                {
                    ID = Convert.ToInt32(MyGroup.Rows[i]["ID"]),
                    GroupName = Convert.ToString(MyGroup.Rows[i]["GroupName"]),
                    CreatedBy = Convert.ToInt32(MyGroup.Rows[i]["CreatedBy"])
                });

                Groups.Add(Context.ConnectionId, Convert.ToString(MyGroup.Rows[i]["GroupName"]));
            }

            Clients.Caller.GetGroups(AllGroups);
        }

        public void AddToGroup(string GroupName, string UserIds)
        {
            var Ids = UserIds.Split(',').Select(Int32.Parse).ToList();

            var ConnectedGroupUsers = ConnectedUsers.Where(x => Ids.Contains(x.UserId)).ToList();

            foreach (var item in ConnectedGroupUsers)
            {
                Groups.Add(item.ConnectionId, GroupName);
            }

            string Query = "select * from tbl_Group where GroupName='" + GroupName + "' AND COALESCE(IsDeleted,0)=0";
            if (ConnC.IsExist(Query))
            {
                var GroupId = Convert.ToInt32(ConnC.GetColumnVal(Query, "ID"));
                var CreatedBy = Convert.ToInt32(ConnC.GetColumnVal(Query, "CreatedBy"));
                Clients.Group(GroupName).AddToGroup(GroupId, GroupName, CreatedBy);

                // send to notification group user
                string Notification = "You are added in #" + GroupName;
                Clients.Group(GroupName).sendGroupNotification(Notification);
            }
        }

        public void GetGroupMessage(int groupId)
        {
            var Messages = ConnC.GetList("select * from tbl_Chat where GroupId=" + groupId + " and COALESCE(IsDeleted,0)=0");

            List<Messages> AllMessages = new List<Messages>();

            for (int i = 0; i < Messages.Rows.Count; i++)
            {
                string Query = "select * from tbl_Users where ID=" + Convert.ToInt32(Messages.Rows[i]["SenderId"]);
                string userName = string.Empty;
                string UserImg = string.Empty;
                if (ConnC.IsExist(Query))
                {
                    userName = ConnC.GetColumnVal(Query, "UserName");
                    UserImg = GetUserImage(userName);
                }

                AllMessages.Add(
                    new Messages
                    {
                        MessageId = Convert.ToInt32(Messages.Rows[i]["ID"]),
                        Message = Convert.ToString(Messages.Rows[i]["Message"]),
                        UserName = userName,
                        UserImage = UserImg,
                        Time = Messages.Rows[i]["DateTime"].ToString(),
                        IsEdited = !string.IsNullOrEmpty(Messages.Rows[i]["ModifiedDate"].ToString()) ? true : false
                    });
            }

            Clients.Caller.GetPrivateMessage(AllMessages);
        }

        public void SendGroupMessage(int groupId, string groupName, int fromUserId, string message)
        {
            var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == fromUserId);

            string CurrentDateTime = DateTime.Now.ToString();
            string UserImg = GetUserImage(fromUser.UserName);

            // SAVE MSG TO DB
            string Query = "insert into tbl_Chat(SenderId,GroupId,Message,DateTime)Values(" + fromUser.UserId + "," + groupId + ",N'" + message + "', GETDATE())";
            int MessageId = ConnC.ExecuteChatQuery(Query);

            // send to group user
            Clients.Group(groupName).sendGroupMessage(groupId, fromUser.UserName, message, UserImg, CurrentDateTime, MessageId);

            // send to notification group user
            Regex tagRegex = new Regex(@"<\s*([^ >]+)[^>]*>.*?<\s*/\s*\1\s*>");
            string Notification = tagRegex.IsMatch(message) ? fromUser.UserName + " shared file in " + groupName : "New message from " + fromUser.UserName + " in " + groupName;
            Clients.Group(groupName).sendGroupNotification(Notification);
        }

        public void EditGroupMessage(int groupId, string groupName, int fromUserId, string Message, int MessageId)
        {
            ConnC.ExecuteQuery("update tbl_Chat set Message=N'" + Message + "', ModifiedDate=GETDATE() where ID=" + MessageId);

            var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == fromUserId);

            string CurrentDateTime = DateTime.Now.ToString();
            string UserImg = GetUserImage(fromUser.UserName);

            Clients.Group(groupName).sendEditedGroupMessage(fromUser.UserId, fromUser.UserName, Message, UserImg, CurrentDateTime, MessageId);
        }

        public void DeleteGroupMessage(int groupId, string groupName, int fromUserId, int MessageId)
        {
            ConnC.ExecuteQuery("update tbl_Chat set IsDeleted=1, ModifiedDate=GETDATE() where ID=" + MessageId);

            // send to group users
            Clients.Group(groupName).sendDeletedMessage(MessageId);
        }

        public void DeleteGroup(int groupId)
        {
            ConnC.ExecuteQuery("update tbl_Group set IsDeleted=1 where ID=" + groupId);

            string Query = "select * from tbl_Group where ID='" + groupId + "'";

            if (ConnC.IsExist(Query))
            {
                string GroupName = Convert.ToString(ConnC.GetColumnVal(Query, "GroupName"));
                Clients.Group(GroupName).sendDeletedGroup(groupId);
            }
        }

        public void EditGroup(string GroupName, string UserIds)
        {
            var Ids = UserIds.Split(',').Select(Int32.Parse).ToList();

            var ConnectedGroupUsers = ConnectedUsers.Where(x => Ids.Contains(x.UserId)).ToList();

            foreach (var item in ConnectedGroupUsers)
            {
                Groups.Add(item.ConnectionId, GroupName);
            }

            string Query = "select * from tbl_Group where GroupName='" + GroupName + "' AND COALESCE(IsDeleted,0)=0";
            if (ConnC.IsExist(Query))
            {
                var GroupId = Convert.ToInt32(ConnC.GetColumnVal(Query, "ID"));
                var CreatedBy = Convert.ToInt32(ConnC.GetColumnVal(Query, "CreatedBy"));
                Clients.Group(GroupName).sendEditedGroup(GroupId, GroupName, CreatedBy);
            }
        }

        public void PrivateRoomSearch(int toUserId, int fromUserId, string SearchString)
        {
            var Messages = ConnC.GetList("select * from tbl_Chat where ((SenderId=" + toUserId + " and ReceiverId=" + fromUserId + ") OR " +
                "(SenderId=" + fromUserId + " and ReceiverId=" + toUserId + ")) and COALESCE(IsDeleted,0)=0 and Message like '%" + SearchString + "%'");

            List<Messages> AllMessages = new List<Messages>();

            for (int i = 0; i < Messages.Rows.Count; i++)
            {
                string Query = "select * from tbl_Users where ID=" + Convert.ToInt32(Messages.Rows[i]["SenderId"]);
                string userName = string.Empty;
                string UserImg = string.Empty;
                if (ConnC.IsExist(Query))
                {
                    userName = ConnC.GetColumnVal(Query, "UserName");
                    UserImg = GetUserImage(userName);
                }

                AllMessages.Add(
                    new Messages
                    {
                        MessageId = Convert.ToInt32(Messages.Rows[i]["ID"]),
                        Message = Convert.ToString(Messages.Rows[i]["Message"]),
                        UserName = userName,
                        UserImage = UserImg,
                        Time = Messages.Rows[i]["DateTime"].ToString(),
                        IsEdited = !string.IsNullOrEmpty(Messages.Rows[i]["ModifiedDate"].ToString()) ? true : false
                    });
            }

            Clients.Caller.GetPrivateMessage(AllMessages);
        }

        public void GroupSearch(int groupId, string SearchString)
        {
            var Messages = ConnC.GetList("select * from tbl_Chat where GroupId=" + groupId + " and COALESCE(IsDeleted,0)=0 and Message like '%" + SearchString + "%'");

            List<Messages> AllMessages = new List<Messages>();

            for (int i = 0; i < Messages.Rows.Count; i++)
            {
                string Query = "select * from tbl_Users where ID=" + Convert.ToInt32(Messages.Rows[i]["SenderId"]);
                string userName = string.Empty;
                string UserImg = string.Empty;
                if (ConnC.IsExist(Query))
                {
                    userName = ConnC.GetColumnVal(Query, "UserName");
                    UserImg = GetUserImage(userName);
                }

                AllMessages.Add(
                    new Messages
                    {
                        MessageId = Convert.ToInt32(Messages.Rows[i]["ID"]),
                        Message = Convert.ToString(Messages.Rows[i]["Message"]),
                        UserName = userName,
                        UserImage = UserImg,
                        Time = Messages.Rows[i]["DateTime"].ToString()
                    });
            }

            Clients.Caller.GetPrivateMessage(AllMessages);
        }

        public void GetUsers(int selection, int userId)
        {
            List<Users> Users = new List<Users>();
            var id = Context.ConnectionId;

            var AllUsers = ConnC.GetAllUsers(userId).Tables[0];
            for (int i = 0; i < AllUsers.Rows.Count; i++)
            {
                string UserImg = GetUserImage(Convert.ToString(AllUsers.Rows[i]["UserName"]));
                string logintime = DateTime.Now.ToString();
                var RecentMsgTime = new Nullable<DateTime>();

                if (!string.IsNullOrEmpty(Convert.ToString(AllUsers.Rows[i]["RecentTime"])))
                {
                    RecentMsgTime = Convert.ToDateTime(Convert.ToString(AllUsers.Rows[i]["RecentTime"]));
                }

                Users.Add(new Users
                {
                    ConnectionId = userId == Convert.ToInt32(AllUsers.Rows[i]["ID"]) ? id : string.Empty,
                    UserId = Convert.ToInt32(AllUsers.Rows[i]["ID"]),
                    UserName = Convert.ToString(AllUsers.Rows[i]["UserName"]),
                    UserImage = UserImg,
                    LoginTime = logintime,
                    RecentMsgTime = !string.IsNullOrEmpty(Convert.ToString(AllUsers.Rows[i]["RecentTime"])) ? RecentMsgTime : null,
                    IsRead = Convert.ToInt32(AllUsers.Rows[i]["IsRead"]),
                    RecentMsg = Convert.ToString(AllUsers.Rows[i]["Message"]),
                    IsOnline = ConnectedUsers.Where(x => x.UserId == Convert.ToInt32(AllUsers.Rows[i]["ID"])).Any()
                });
            }

            Clients.Caller.getUsers(selection == 2 ? Users.OrderByDescending(x => x.RecentMsgTime) : (selection == 3 ? Users.Where(x => x.IsOnline == true) : Users));
        }

        public void PinUnpinUser(int UserId, int PinPerson_Id, bool IsPined, bool IsGroup)
        {
            bool result = ConnC.PinUnpinUser(UserId, PinPerson_Id, IsPined, IsGroup);
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}