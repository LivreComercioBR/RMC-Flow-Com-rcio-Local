using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using RMCFlowComercioLocal.Infrastructure;

namespace RMCFlowComercioLocal
{
    internal sealed class MainForm : Form
    {
        private readonly LocalDatabase _database;
        private readonly TextBox _legalName = new TextBox();
        private readonly TextBox _tradeName = new TextBox();
        private readonly TextBox _document = new TextBox();
        private readonly TextBox _adminName = new TextBox();
        private readonly TextBox _adminEmail = new TextBox();
        private readonly TextBox _requestCode = new TextBox();
        private readonly TextBox _signedLicense = new TextBox();
        private readonly Label _status = new Label();

        public MainForm(LocalDatabase database)
        {
            _database = database;
            Text = "RMC Flow Comércio Local"; StartPosition = FormStartPosition.CenterScreen; Size = new Size(920, 760); MinimumSize = new Size(760, 620);
            BackColor = Color.FromArgb(7, 20, 44); ForeColor = Color.White; BuildInterface();
        }

        private void BuildInterface()
        {
            Controls.Add(new Label { Text = "RMC Flow Comércio Local", Font = new Font("Segoe UI", 23, FontStyle.Bold), ForeColor = Color.FromArgb(78, 224, 232), AutoSize = true, Location = new Point(34, 24) });
            Controls.Add(new Label { Text = "Ativação permanente — comércios em geral", AutoSize = true, Location = new Point(37, 68), ForeColor = Color.FromArgb(201, 226, 244) });
            var form = new TableLayoutPanel { Location = new Point(34, 98), Size = new Size(820, 208), ColumnCount = 2, RowCount = 3 };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            form.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F)); form.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F)); form.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));
            AddField(form, "Razão social *", _legalName, 0, 0); AddField(form, "Nome fantasia *", _tradeName, 1, 0); AddField(form, "CPF/CNPJ", _document, 0, 1); AddField(form, "Nome do administrador *", _adminName, 1, 1); AddField(form, "E-mail do administrador *", _adminEmail, 0, 2); AddField(form, "Código de solicitação", _requestCode, 1, 2); _requestCode.ReadOnly = true;
            Controls.Add(form);
            var generate = Button("1. Gerar código de solicitação", 34, 322, 245); generate.Click += GenerateRequestCode; Controls.Add(generate);
            Controls.Add(new Label { Text = "2. Cole abaixo a licença assinada recebida da RMC Sites", AutoSize = true, Location = new Point(34, 382), ForeColor = Color.FromArgb(218, 236, 250) });
            _signedLicense.Location = new Point(34, 409); _signedLicense.Size = new Size(820, 126); _signedLicense.Multiline = true; _signedLicense.ScrollBars = ScrollBars.Vertical; _signedLicense.Font = new Font("Consolas", 9); Controls.Add(_signedLicense);
            var activate = Button("3. Validar e ativar licença", 34, 553, 225); activate.Click += Activate; Controls.Add(activate);
            _status.Location = new Point(34, 610); _status.Size = new Size(820, 70); _status.ForeColor = Color.FromArgb(201, 226, 244); _status.Text = "A ativação pode ser concluída offline: gere o código, envie à RMC Sites e cole a licença assinada recebida."; Controls.Add(_status);
        }

        private static Button Button(string text, int x, int y, int width) { return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 42), BackColor = Color.FromArgb(24, 189, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; }
        private static void AddField(TableLayoutPanel panel, string title, TextBox textBox, int column, int row) { var group = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) }; var label = new Label { Text = title, Dock = DockStyle.Top, Height = 22, ForeColor = Color.FromArgb(218, 236, 250) }; textBox.Dock = DockStyle.Top; textBox.Height = 30; textBox.Font = new Font("Segoe UI", 10); group.Controls.Add(textBox); group.Controls.Add(label); panel.Controls.Add(group, column, row); }

        private void GenerateRequestCode(object sender, EventArgs e)
        {
            _requestCode.Text = LicenseService.CreateRequestCode(_document.Text);
            _requestCode.Focus(); _requestCode.SelectAll();
            try { Clipboard.SetText(_requestCode.Text); } catch { }
            _status.ForeColor = Color.FromArgb(201, 226, 244);
            _status.Text = "Código gerado e copiado. Confirme-o no campo acima e cole-o no Painel de Licenças da RMC Sites.";
        }

        private void Activate(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_legalName.Text) || string.IsNullOrWhiteSpace(_tradeName.Text) || string.IsNullOrWhiteSpace(_adminName.Text) || string.IsNullOrWhiteSpace(_adminEmail.Text)) { _status.Text = "Preencha os campos obrigatórios da empresa e do administrador."; return; }
            if (string.IsNullOrWhiteSpace(_requestCode.Text)) { _status.Text = "Primeiro gere o código de solicitação deste computador."; return; }
            var publicKey = Path.Combine(Application.StartupPath, "keys", "license-public.pem"); string result;
            if (!LicenseService.TryValidateSignedLicense(_signedLicense.Text, _requestCode.Text, publicKey, out result)) { _status.Text = result; return; }
            _database.Set("company_legal_name", _legalName.Text.Trim()); _database.Set("company_trade_name", _tradeName.Text.Trim()); _database.Set("license_request_code", _requestCode.Text); _database.Set("license_code", result); _database.Set("license_status", "activated");
            _status.ForeColor = Color.FromArgb(102, 241, 177); _status.Text = "Licença " + result + " validada e ativada neste computador.";
            Hide(); using (var setup = new FirstUserForm(_database)) { if (setup.ShowDialog() != DialogResult.OK) { Close(); return; } } using (var login = new LoginForm(_database)) { if (login.ShowDialog() == DialogResult.OK) using (var app = new AppForm(_database, login.User)) app.ShowDialog(); } Close();
        }
    }
}
