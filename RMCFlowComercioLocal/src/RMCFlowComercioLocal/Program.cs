using System;
using System.Windows.Forms;

namespace RMCFlowComercioLocal
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var database = new Infrastructure.LocalDatabase();
#if !DEBUG
            if (!database.IsActivated()) { Application.Run(new MainForm(database)); return; }
#endif
            if (!database.HasUsers())
            {
                using (var setup = new FirstUserForm(database)) { if (setup.ShowDialog() != DialogResult.OK) return; }
            }
            using (var login = new LoginForm(database))
            {
                if (login.ShowDialog() == DialogResult.OK) { database.BeginSession(login.User); Application.Run(new AppForm(database, login.User)); }
            }
        }
    }
}
