using MySqlConnector;

//TODO
//test with real data later
//make the logo a reality
//make the login page look okay

namespace Rentalin;
public partial class FormLogin : Form
{
    public FormLogin()
    {
        //the window
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 540);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Text = "Login";

        Image logoImage = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "images", "logo.png"));
                        
        int panelWidth = Math.Max(400, logoImage.Width);
        int top = logoImage.Height + 40;            

        var inputtables = new Panel {
            BorderStyle = BorderStyle.None,          
            Size = new Size(panelWidth, top + 200),
            Anchor = AnchorStyles.None
        };
        inputtables.Location = new Point(
            (this.ClientSize.Width - inputtables.Width) / 2,
            (this.ClientSize.Height - inputtables.Height) / 2
        );

        int w = inputtables.ClientSize.Width;
        int fieldW = 360;
        int fieldX = (w - fieldW) / 2;

        PictureBox logo = new PictureBox {
            Image = logoImage,
            SizeMode = PictureBoxSizeMode.Normal,
            Size = logoImage.Size
        };
        logo.Location = new Point((w - logo.Width) / 2, 0);

        Label usernameLabel = new Label {
            Text = "Username",
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, top),
            Size = new Size(w, 25)
        };
        TextBox usernameBox = new TextBox {
            TextAlign = HorizontalAlignment.Center,
            Location = new Point(fieldX, top + 30),
            Width = fieldW
        };

        Label passwordLabel = new Label {
            Text = "Password",
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, top + 70),
            Size = new Size(w, 25)
        };
        TextBox passwordBox = new TextBox {
            TextAlign = HorizontalAlignment.Center,
            UseSystemPasswordChar = true,
            Location = new Point(fieldX, top + 100),
            Width = fieldW
        };

        Button loginButton = new Button {
            Text = "Login",
            Size = new Size(120, 35)
        };
        loginButton.Location = new Point((w - loginButton.Width) / 2, top + 155);

        loginButton.Click += (s, e) => {
            if (usernameBox.Text == "" || passwordBox.Text == "")
            {
                MessageBox.Show("Lengkapi data terlebih dahulu", "Gagal");
                return;
            }
            try
            {
                using var conn = Db.Open();
                using var cmd = new MySqlCommand("SELECT password FROM users WHERE username = @u LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@u", usernameBox.Text.Trim());

                var hash = cmd.ExecuteScalar() as string;

                if (hash == null || !BCrypt.Net.BCrypt.Verify(passwordBox.Text, hash))
                {
                    MessageBox.Show("Salah username atau password", "Gagal");
                    return;
                }

                var dashboard = new DashboardKasir();
                dashboard.FormClosed += (s2, e2) => this.Close();
                dashboard.Show();
                this.Hide();
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Gagal meraih database:\n" + ex.Message);
            }
        };

        inputtables.Controls.AddRange(new Control[] {
            logo, usernameLabel, usernameBox, passwordLabel, passwordBox, loginButton
        });

        this.Controls.Add(inputtables);
    }
}