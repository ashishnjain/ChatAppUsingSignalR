using SignalRChat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace SignalRChat.Controllers
{
    [RoutePrefix("api/Chat")]
    public class ChatController : ApiController
    {
        ConnClass ConnC = new ConnClass();

        [HttpGet]
        [Route("Login")]
        public ResultBE Login(string Email, string Password)
        {
            ResultBE user = new ResultBE();
            try
            {
                string Query = "select * from tbl_Users where Email='" + Email + "' and Password='" + Password + "'";
                if (ConnC.IsExist(Query))
                {
                    user.UserName = ConnC.GetColumnVal(Query, "UserName");
                    user.UserId = Convert.ToInt32(ConnC.GetColumnVal(Query, "ID"));
                    user.Email = Email;
                    user.ResponseCode = "200";
                    user.Message = "Login successfully";
                    user.ResponseStatus = "OK";
                    Query = "update tbl_Users set Is_Online = 1 where Email='" + Email + "' and Password='" + Password + "'";
                    ConnC.ExecuteQuery(Query);
                }
                else
                {
                    user.ResponseCode = "401";
                    user.Message = "Invalid Email or Password!!";
                    user.ResponseStatus = "Unauthorized";
                }
            }
            catch (Exception ex)
            {
                user.ResponseCode = "400";
                user.Message = ex.Message.ToString();
                user.ResponseStatus = "Bad Request";
            }
            return user;
        }

        [HttpGet]
        [Route("Logout")]
        public HttpResponseMessage Logout(int UserId)
        {
            try
            {
                string Query = "update tbl_Users set Is_Online = 0 where ID=" + UserId;
                ConnC.ExecuteQuery(Query);

                return Request.CreateResponse(HttpStatusCode.OK, "Logout Successfully");
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message.ToString());
            }
        }

        [HttpPost]
        [Route("Registration")]
        public ResultBE Registration(Registration model)
        {
            ResultBE user = new ResultBE();
            try
            {
                string Query = "insert into tbl_Users(UserName,Email,Password)Values('" + model.UserName + "','" + model.Email + "','" + model.Password + "')";
                string ExistQ = "select * from tbl_Users where Email='" + model.Email + "'";

                if (!ConnC.IsExist(ExistQ))
                {
                    if (ConnC.ExecuteQuery(Query))
                    {
                        user.UserId = Convert.ToInt32(ConnC.GetColumnVal(ExistQ, "ID"));
                        user.UserName = model.UserName;
                        user.Email = model.Email;
                        user.ResponseCode = "200";
                        user.Message = "You have successfully Registered.!";
                        user.ResponseStatus = "OK";
                        Query = "update tbl_Users set Is_Online = 1 where Email='" + model.Email + "' and Password='" + model.Password + "'";
                        ConnC.ExecuteQuery(Query);
                    }
                }
                else
                {
                    user.ResponseCode = "401";
                    user.Message = "Email is already Exists!! Please Try Different Email..";
                    user.ResponseStatus = "Unauthorized";
                }
            }
            catch (Exception ex)
            {
                user.ResponseCode = "400";
                user.Message = ex.Message.ToString();
                user.ResponseStatus = "Bad Request";
            }
            return user;
        }

        [HttpPost]
        [Route("UploadFile")]
        public HttpResponseMessage UploadFile(FileUploadBE file)
        {
            try
            {
                var filePath = HttpContext.Current.Server.MapPath("~/Uploads/" + file.FileName);
                if (!Directory.Exists(HttpContext.Current.Server.MapPath("~/Uploads")))
                {
                    Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~/Uploads"));
                }

                if (System.IO.File.Exists(filePath))
                {
                    int count = 1;
                    string fileNameOnly = Path.GetFileNameWithoutExtension(filePath);
                    string extension = Path.GetExtension(filePath);
                    string newfileName = filePath;
                    while (System.IO.File.Exists(newfileName))
                    {
                        string tempFileName = string.Format("{0}({1})", fileNameOnly, count++);
                        newfileName = Path.Combine("~/Uploads/", tempFileName + extension);
                        filePath = newfileName;
                    }
                }
                System.IO.File.WriteAllBytes(filePath, file.FileByte);

                return Request.CreateResponse(HttpStatusCode.Created, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex.Message.ToString());
            }
        }

        [HttpGet]
        [Route("AllUsers")]
        public List<UserBE> AllUsers()
        {
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

        [HttpPost]
        [Route("AddGroup")]
        public int AddGroup(Groups model)
        {
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

        [HttpGet]
        [Route("GetGroup")]
        public GroupBE GetGroup(int GroupId)
        {
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
    }
}