using SignalRChat.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalRChat
{
    public partial class Chat : System.Web.UI.Page
    {
        public string UserName = "admin";
        public int UserId = 0;
        public string UserImage = "images/dummy.png";
        protected string UploadFolderPath = "~/Uploads/";
        ConnClass ConnC = new ConnClass();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserName"] != null)
            {
                UserName = Session["UserName"].ToString();
                UserId = Convert.ToInt32(Session["UserId"]);
                GetUserImage(UserName);
            }
            else
                Response.Redirect("Login.aspx");

            this.Header.DataBind();
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            string Query = "update tbl_Users set Is_Online = 0 where ID=" + UserId;
            ConnC.ExecuteQuery(Query);
            Session.Clear();
            Session.Abandon();
            Response.Redirect("Login.aspx");
        }

        public void GetUserImage(string Username)
        {
            if (Username != null)
            {
                string query = "select Photo from tbl_Users where UserName='" + Username + "'";

                string ImageName = ConnC.GetColumnVal(query, "Photo");
                if (!string.IsNullOrEmpty(ImageName))
                    UserImage = "images/DP/" + ImageName;
                if (!System.IO.File.Exists(UserImage))
                {
                    UserImage = "images/dummy.png";
                }
            }
        }

        [System.Web.Services.WebMethod]
        public static List<UserBE> GetAllUsers()
        {
            ConnClass ConnC = new ConnClass();

            var Users = ConnC.GetList("select * from tbl_Users");
            List<UserBE> AllUsers = new List<UserBE>();

            for (int i = 0; i < Users.Rows.Count; i++)
            {
                AllUsers.Add(new UserBE
                {
                    UserId = Convert.ToInt32(Users.Rows[i]["ID"]),
                    UserName = Convert.ToString(Users.Rows[i]["UserName"]),
                });
            }

            return AllUsers;
        }

        [System.Web.Services.WebMethod]
        public static GroupBE GetGroup(int GroupId)
        {
            ConnClass ConnC = new ConnClass();

            GroupBE result = new GroupBE();

            string Query = "select * from tbl_Group where ID='" + GroupId + "' AND COALESCE(IsDeleted,0)=0";
            List<int> SelectedUsers = new List<int>();

            if (ConnC.IsExist(Query))
            {
                Groups group = new Groups();
                group.ID = Convert.ToInt32(ConnC.GetColumnVal(Query, "ID"));
                group.CreatedBy = Convert.ToInt32(ConnC.GetColumnVal(Query, "CreatedBy"));
                group.GroupName = Convert.ToString(ConnC.GetColumnVal(Query, "GroupName"));
                group.UserIds = Convert.ToString(ConnC.GetColumnVal(Query, "UserIds"));
                result.Group = group;

                if (!string.IsNullOrWhiteSpace(group.UserIds))
                    SelectedUsers = group.UserIds.Split(',').Select(Int32.Parse).ToList();
            }

            var Users = ConnC.GetList("select * from tbl_Users");
            List<UserBE> AllUsers = new List<UserBE>();

            for (int i = 0; i < Users.Rows.Count; i++)
            {
                AllUsers.Add(new UserBE
                {
                    UserId = Convert.ToInt32(Users.Rows[i]["ID"]),
                    UserName = Convert.ToString(Users.Rows[i]["UserName"]),
                    Selected = SelectedUsers.Where(x => x == Convert.ToInt32(Users.Rows[i]["ID"])).Any()
                });
            }

            result.AllUsers = AllUsers;

            return result;
        }

        [System.Web.Services.WebMethod]
        public static int SaveGroup(Groups model)
        {
            ConnClass ConnC = new ConnClass();
            int result = 0;
            try
            {
                if (model.ID == 0)
                {
                    string Query = "insert into tbl_Group(GroupName,UserIds,CreatedBy,IsDeleted,CreatedDate)Values('" + model.GroupName + "','" + model.UserIds + "','" + model.CreatedBy + "',0,GETDATE())";
                    string ExistQ = "select * from tbl_Group where GroupName='" + model.GroupName + "' AND COALESCE(IsDeleted,0)=0";

                    if (!ConnC.IsExist(ExistQ))
                    {
                        ConnC.ExecuteQuery(Query);
                    }
                    else
                    {
                        result = 1;
                    }
                }
                else
                {
                    string Query = "update tbl_Group set GroupName='" + model.GroupName + "', UserIds='" + model.UserIds + "' WHERE ID=" + model.ID;
                    ConnC.ExecuteQuery(Query);
                }
            }
            catch (Exception ex)
            {
                result = -1;
            }

            return result;
        }

        protected void FileUploadComplete(object sender, EventArgs e)
        {
            string filename = System.IO.Path.GetFileName(AsyncFileUpload1.FileName);
            AsyncFileUpload1.SaveAs(Server.MapPath(this.UploadFolderPath) + filename);
        }

        protected void EditFileUploadComplete(object sender, EventArgs e)
        {
            string filename = System.IO.Path.GetFileName(AsyncFileUpload2.FileName);
            AsyncFileUpload2.SaveAs(Server.MapPath(this.UploadFolderPath) + filename);
        }
    }
}