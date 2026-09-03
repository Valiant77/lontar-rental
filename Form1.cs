namespace Rentalin;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();

        //Label
        Label title = new Label();
        title.Text = "Hello Bro";
        title.Location = new Point(50, 50);
        title.AutoSize = true;
        this.Controls.Add(title);

        Button exit = new Button();
        exit.Text = "QUIT!";
        exit.Location = new Point(450, 300);
    }
}
