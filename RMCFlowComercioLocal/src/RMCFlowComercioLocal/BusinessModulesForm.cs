using System;
using System.Drawing;
using System.Windows.Forms;
using RMCFlowComercioLocal.Infrastructure;

namespace RMCFlowComercioLocal
{
    internal sealed class BusinessModulesForm : Form
    {
        private readonly LocalDatabase _database;
        private readonly CheckBox _services = new CheckBox();
        private readonly TextBox _activity = new TextBox();

        public BusinessModulesForm(LocalDatabase database)
        {
            _database = database;
            Text = "Módulos da empresa";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(560, 340);
            MinimumSize = new Size(560, 340);
            Build();
        }

        private void Build()
        {
            Controls.Add(new Label
            {
                Text = "Escolha os recursos que fazem parte da operação da empresa.",
                Location = new Point(24, 24),
                Size = new Size(500, 28),
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            });

            Controls.Add(new Label { Text = "Atividade principal (opcional)", Location = new Point(28, 62), AutoSize = true });
            _activity.Location = new Point(28, 84);
            _activity.Size = new Size(480, 25);
            _activity.Text = _database.Get("business.activity") ?? string.Empty;
            Controls.Add(_activity);

            _services.Text = "A empresa presta serviços e utiliza ordens de serviço";
            _services.Checked = _database.Get("modules.services") == "1";
            _services.Location = new Point(28, 126);
            _services.Size = new Size(490, 28);
            Controls.Add(_services);

            Controls.Add(new Label
            {
                Text = "Ao ativar, o sistema exibirá os módulos de serviços, agendamentos, ordens de serviço e garantias. Reinicie o sistema para aplicar a alteração.",
                Location = new Point(28, 162),
                Size = new Size(480, 58),
                ForeColor = Color.DimGray
            });

            var save = new Button
            {
                Text = "Salvar",
                Location = new Point(390, 240),
                Size = new Size(120, 34),
                BackColor = Color.FromArgb(24, 189, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            save.Click += Save;
            Controls.Add(save);
        }

        private void Save(object sender, EventArgs e)
        {
            _database.Set("business.activity", _activity.Text.Trim());
            _database.Set("modules.services", _services.Checked ? "1" : "0");
            MessageBox.Show("Configuração salva. Feche e reabra o RMC Flow Comércio Local para atualizar os módulos.", "Módulos da empresa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
    }
}
