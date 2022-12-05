using SignalRChat.Models;
using System;

namespace SignalRChat
{
    public partial class Login : System.Web.UI.Page
    {
        //Class Object
        ConnClass ConnC = new ConnClass();
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ErrorMessage.Visible = false;
            }
        }

        protected void btnSignIn_Click(object sender, EventArgs e)
        {
            string Query = "select * from tbl_Users where Email='" + txtEmail.Value + "' and Password='" + txtPassword.Value + "'";
            if (ConnC.IsExist(Query))
            {
                string UserName = ConnC.GetColumnVal(Query, "UserName");
                string UserId = ConnC.GetColumnVal(Query, "ID");
                Session["UserId"] = UserId;
                Session["UserName"] = UserName;
                Session["Email"] = txtEmail.Value;
                Query = "update tbl_Users set Is_Online = 1 where Email='" + txtEmail.Value + "' and Password='" + txtPassword.Value + "'";
                ConnC.ExecuteQuery(Query);
                Response.Redirect("Chat.aspx");
            }
            else
            {
                ErrorMessage.Visible = true;
                ErrorMessage.InnerHtml = "Invalid Email or Password!!";
            }
        }
    }
}