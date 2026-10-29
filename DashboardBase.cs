using System.ComponentModel;
namespace Rentalin;

public abstract class DashboardBase : Form
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LoggedOut { get; protected set; }
}

public class DashboardOwner : StubDashboard
{
    public DashboardOwner(string username) : base("Owner", username) { }
}

//placeholder, replace later
public class StubDashboard : DashboardBase
{
    protected StubDashboard(string role, string username)
    {
        AutoScaleMode = AutoScaleMode.Font;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        WindowState = FormWindowState.Maximized;
        Text = $"Dashboard {role} | {username}";
        BackColor = Theme.Background;
        Font = new Font("Segoe UI", 10f);

        var body = new Label {
            Dock = DockStyle.Fill,
            Text = $"Dashboard {role}\nTBA",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 28f),
            ForeColor = Theme.TextMuted
        };

        var bar = new Panel {
            Dock = DockStyle.Top,
            Height = 80,
            BackColor = Theme.Card,
            Padding = new Padding(20)
        };
        var logout = new Button { Text = "Logout", Dock = DockStyle.Right, Width = 100 };
        Theme.StyleDanger(logout);
        logout.Click += (s, e) => { LoggedOut = true; Close(); };
        bar.Controls.Add(logout);

        Controls.Add(body);
        Controls.Add(bar);
    }
}