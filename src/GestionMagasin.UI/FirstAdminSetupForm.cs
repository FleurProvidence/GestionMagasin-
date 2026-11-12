using GestionMagasin.Services.Security;

namespace GestionMagasin.UI;

public sealed class FirstAdminSetupForm : Form
{
    private readonly AuthService _auth;

    public FirstAdminSetupForm(AuthService auth)
    {
        _auth = auth;
        Text = "Premier compte administrateur";
        Width = 520;
        Height = 260;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var lblNom = new Label { Text = "Nom", Location = new Point(20, 20), Width = 120 };
        var txtNom = new TextBox { Location = new Point(150, 20), Width = 320 };

        var lblIdentifiant = new Label { Text = "Identifiant", Location = new Point(20, 60), Width = 120 };
        var txtIdentifiant = new TextBox { Location = new Point(150, 60), Width = 320 };

        var lblPassword = new Label { Text = "Mot de passe", Location = new Point(20, 100), Width = 120 };
        var txtPassword = new TextBox { Location = new Point(150, 100), Width = 320, UseSystemPasswordChar = true };

        var btnOk = new Button
        {
            Text = "Créer le compte",
            Location = new Point(310, 170),
            Width = 160,
            DialogResult = DialogResult.OK
        };

        btnOk.Click += (_, _) =>
        {
            try
            {
                _auth.CreateFirstAdmin(txtNom.Text, txtIdentifiant.Text, txtPassword.Text);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        Controls.AddRange(new Control[]
        {
            lblNom, txtNom,
            lblIdentifiant, txtIdentifiant,
            lblPassword, txtPassword,
            btnOk
        });
    }
}
