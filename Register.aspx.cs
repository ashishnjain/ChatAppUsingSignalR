using SignalRChat.Models;
using System;
using System.Web.UI;

namespace SignalRChat
{
    public partial class Register : System.Web.UI.Page
    {
        ConnClass ConnC = new ConnClass();
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        protected void btnRegister_ServerClick(object sender, EventArgs e)
        {
            string Query = "insert into tbl_Users(UserName,Email,Password)Values('" + txtName.Value + "','" + txtEmail.Value + "','" + txtPassword.Value + "')";
            string ExistQ = "select * from tbl_Users where Email='" + txtEmail.Value + "'";
            if (!ConnC.IsExist(ExistQ))
            {
                if (ConnC.ExecuteQuery(Query))
                {
                    ScriptManager.RegisterStartupScript(this, GetType(), "Message", "alert('Congratulations!! You have successfully registered..');", true);
                    string UserId = ConnC.GetColumnVal(ExistQ, "ID");
                    Session["UserId"] = UserId;
                    Session["UserName"] = txtName.Value;
                    Session["Email"] = txtEmail.Value;
                    Query = "update tbl_Users set Is_Online = 1 where Email='" + txtEmail.Value + "' and Password='" + txtPassword.Value + "'";
                    ConnC.ExecuteQuery(Query);
                    Response.Redirect("Chat.aspx");
                }
            }
            else
            {
                ScriptManager.RegisterStartupScript(this, GetType(), "Message", "alert('Email is already Exists!! Please Try Different Email..');", true);
            }
        }
    }
}