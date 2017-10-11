using System;
using System.Windows.Forms;

namespace Blackbaud.Addin.TokenAuthentication.Tester
{
    public partial class ValidateTokenForm : Form
    {
        public ValidateTokenForm()
        {
            InitializeComponent();
        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            try
            {
                var token = txtToken.Text;
                var applicationId = new Guid(txtApplicationId.Text);

                var uit = await UserIdentityToken.ParseAsync(token, applicationId);

                MessageBox.Show("The token was valid, UserId = " + uit.UserId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while validating the user identity token:  {ex.Message}");
            }
        }
    }
}