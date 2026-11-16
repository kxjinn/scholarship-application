using scholarship_Application.Models;
using scholarship_Application.Enums;

namespace scholarship_Application.Forms;

public partial class LoginForm : Form
{
    public LoginForm()
    {
        InitializeComponent();
    }

    private void LoginForm_Load(object sender, EventArgs e)
    {
        txtUsername.SelectAll();
        txtUsername.Focus();
    }

    private void chkShow_CheckedChanged(object sender, EventArgs e)
    {
        txtPassword.UseSystemPasswordChar = !chkShow.Checked;
    }

    private void btnSignIn_Click(object sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text.Trim();
        if (username.Length == 0)
        {
            lblError.Text = "Type a username to continue.";
            txtUsername.Focus();
            return;
        }

        User? user = scholarship_Application.Data.AppRepository.SignIn(username, password);

        if (user is null)
        {
            lblError.Text = "Wrong username or password.";
            txtPassword.Clear();
            txtPassword.Focus();
            return;
        }

        scholarship_Application.Data.AppSession.Username = user.Username;
        scholarship_Application.Data.AppSession.FullName = user.FullName;
        scholarship_Application.Data.AppSession.StudentNumber = user.StudentNumber;
        scholarship_Application.Data.AppSession.IsAdmin = user.Role == Roles.Admin;

        DashboardBase shell = user.Role == Roles.Admin
            ? (DashboardBase)new AdminDashboard(this)
            : new StudentDashboard(this);

        shell.FormClosed += (_, _) =>
        {
            if (!IsDisposed && !Visible)
            {
                Application.Exit();
            }
        };

        Hide();
        shell.Show();

        txtPassword.Clear();
        lblError.Text = "";
    }

    public void Reset()
    {
        Show();
        txtPassword.Clear();
        txtUsername.SelectAll();
        txtUsername.Focus();
    }

    
}
