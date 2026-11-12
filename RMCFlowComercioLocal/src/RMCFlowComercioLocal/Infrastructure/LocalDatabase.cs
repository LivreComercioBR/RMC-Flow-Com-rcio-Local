using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;

namespace RMCFlowComercioLocal.Infrastructure
{
    internal sealed class SystemUser
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    internal sealed class LocalDatabase
    {
        private readonly string _databasePath;
        private SystemUser _sessionUser;
        public void BeginSession(SystemUser user) { _sessionUser = user ?? throw new ArgumentNullException("user"); }
        private void RequirePermission(string key)
        {
            if (_sessionUser == null) return; // Installation and signed recovery run before login.
            if (!HasPermission(_sessionUser.Id, _sessionUser.Role, key))
                throw new InvalidOperationException("Seu usuário não tem permissão para esta operação.");
        }

        public LocalDatabase()
        {
#if DEBUG
            var productDataDirectory = "RMC Flow Com\u00e9rcio Local - Desenvolvimento";
#else
            var productDataDirectory = "RMC Flow Com\u00e9rcio Local";
#endif
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RMC Sites", productDataDirectory);
            Directory.CreateDirectory(directory);
            _databasePath = Path.Combine(directory, "rmc-flow-comercio-local.db");
            Initialize();
        }

        private void Initialize()
        {
            if (!File.Exists(_databasePath)) SQLiteConnection.CreateFile(_databasePath);
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(@"
CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS licenses (id INTEGER PRIMARY KEY, activation_code TEXT, request_code TEXT, status TEXT NOT NULL, activated_at TEXT, entitlement_json TEXT);
CREATE TABLE IF NOT EXISTS clients (id INTEGER PRIMARY KEY, name TEXT NOT NULL, document TEXT, phone TEXT, email TEXT);
CREATE TABLE IF NOT EXISTS vehicles (id INTEGER PRIMARY KEY, client_id INTEGER NOT NULL, type TEXT NOT NULL, plate TEXT, manufacturer TEXT, model TEXT NOT NULL, year_model TEXT);
CREATE TABLE IF NOT EXISTS appointments (id INTEGER PRIMARY KEY, client_id INTEGER NOT NULL, vehicle_id INTEGER NOT NULL, scheduled_at TEXT NOT NULL, service_description TEXT NOT NULL, notes TEXT, status TEXT NOT NULL DEFAULT 'Agendado', work_order_id INTEGER, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS work_orders (id INTEGER PRIMARY KEY, number TEXT NOT NULL UNIQUE, client_id INTEGER NOT NULL, vehicle_id INTEGER NOT NULL, status TEXT NOT NULL, opened_at TEXT NOT NULL, expected_at TEXT, mileage TEXT, fuel_level TEXT, complaint TEXT NOT NULL, diagnosis TEXT, notes TEXT, quote_id INTEGER, quote_number TEXT, approval_history TEXT, finalized_at TEXT, cancelled_at TEXT, warranty_days INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS work_order_items (id INTEGER PRIMARY KEY, work_order_id INTEGER NOT NULL, product_id INTEGER, item_type TEXT NOT NULL, description TEXT NOT NULL, quantity REAL NOT NULL, unit_price REAL NOT NULL, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS quotes (id INTEGER PRIMARY KEY, number TEXT NOT NULL UNIQUE, client_id INTEGER NOT NULL, vehicle_id INTEGER NOT NULL, status TEXT NOT NULL DEFAULT 'Em aberto', valid_until TEXT, complaint TEXT, notes TEXT, converted_work_order_id INTEGER, created_at TEXT NOT NULL, approved_at TEXT, approval_method TEXT, approval_notes TEXT, rejected_at TEXT, rejection_reason TEXT);
CREATE TABLE IF NOT EXISTS quote_items (id INTEGER PRIMARY KEY, quote_id INTEGER NOT NULL, product_id INTEGER, item_type TEXT NOT NULL, description TEXT NOT NULL, quantity REAL NOT NULL, unit_price REAL NOT NULL, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS products (id INTEGER PRIMARY KEY, code TEXT, barcode TEXT, name TEXT NOT NULL, category TEXT, unit TEXT, sale_price REAL NOT NULL DEFAULT 0, stock_quantity REAL NOT NULL DEFAULT 0, minimum_stock REAL NOT NULL DEFAULT 0, shelf_number TEXT, shelf_location TEXT, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS product_categories (id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS services (id INTEGER PRIMARY KEY, code TEXT, name TEXT NOT NULL, category TEXT, estimated_duration TEXT, sale_price REAL NOT NULL DEFAULT 0, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS service_categories (name TEXT PRIMARY KEY COLLATE NOCASE);
INSERT OR IGNORE INTO service_categories(name) SELECT DISTINCT trim(category) FROM services WHERE trim(COALESCE(category,''))<>'';
CREATE TABLE IF NOT EXISTS stock_movements (id INTEGER PRIMARY KEY, product_id INTEGER NOT NULL, work_order_id INTEGER, work_order_item_id INTEGER, movement_type TEXT NOT NULL, quantity REAL NOT NULL, previous_quantity REAL NOT NULL, resulting_quantity REAL NOT NULL, reason TEXT, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS sales (id INTEGER PRIMARY KEY, number TEXT NOT NULL UNIQUE, client_id INTEGER, status TEXT NOT NULL, subtotal REAL NOT NULL, discount REAL NOT NULL DEFAULT 0, total REAL NOT NULL, payment_method TEXT, created_at TEXT NOT NULL, cancelled_at TEXT, cancellation_reason TEXT);
CREATE TABLE IF NOT EXISTS sale_items (id INTEGER PRIMARY KEY, sale_id INTEGER NOT NULL, product_id INTEGER NOT NULL, description TEXT NOT NULL, quantity REAL NOT NULL, unit_price REAL NOT NULL, total REAL NOT NULL);
CREATE TABLE IF NOT EXISTS cash_sessions (id INTEGER PRIMARY KEY, opened_at TEXT NOT NULL, opening_balance REAL NOT NULL, status TEXT NOT NULL, closed_at TEXT, counted_balance REAL, difference REAL);
CREATE TABLE IF NOT EXISTS cash_movements (id INTEGER PRIMARY KEY, cash_session_id INTEGER NOT NULL, movement_type TEXT NOT NULL, payment_method TEXT, amount REAL NOT NULL, description TEXT, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS financial_entries (id INTEGER PRIMARY KEY, entry_type TEXT NOT NULL, party_name TEXT, category TEXT, description TEXT NOT NULL, document_number TEXT, amount REAL NOT NULL, due_date TEXT, paid_at TEXT, payment_method TEXT, status TEXT NOT NULL, origin TEXT, sale_id INTEGER, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS financial_categories (id INTEGER PRIMARY KEY, name TEXT NOT NULL UNIQUE, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS suppliers (id INTEGER PRIMARY KEY, name TEXT NOT NULL, document TEXT, phone TEXT, email TEXT, contact_name TEXT, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS debtors (id INTEGER PRIMARY KEY, name TEXT NOT NULL, document TEXT, phone TEXT, email TEXT, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS purchases (id INTEGER PRIMARY KEY, number TEXT NOT NULL UNIQUE, supplier_id INTEGER NOT NULL, supplier_name TEXT NOT NULL, invoice_number TEXT, purchased_at TEXT NOT NULL, due_date TEXT, payment_method TEXT, total REAL NOT NULL, payable_generated INTEGER NOT NULL DEFAULT 0, status TEXT NOT NULL DEFAULT 'Finalizada', cancellation_reason TEXT, cancelled_at TEXT, created_at TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS purchase_items (id INTEGER PRIMARY KEY, purchase_id INTEGER NOT NULL, product_id INTEGER NOT NULL, description TEXT NOT NULL, quantity REAL NOT NULL, unit_cost REAL NOT NULL, total REAL NOT NULL);
CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT NOT NULL, email TEXT NOT NULL UNIQUE, role TEXT NOT NULL, password_hash TEXT NOT NULL, active INTEGER NOT NULL DEFAULT 1);
CREATE TABLE IF NOT EXISTS user_permissions (user_id INTEGER NOT NULL, permission_key TEXT NOT NULL, allowed INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(user_id,permission_key));
CREATE TABLE IF NOT EXISTS audit_logs (id INTEGER PRIMARY KEY, action TEXT NOT NULL, details_json TEXT, created_at TEXT NOT NULL);", connection)) command.ExecuteNonQuery();
                EnsureColumn(connection, "products", "stock_quantity", "REAL NOT NULL DEFAULT 0");
                EnsureColumn(connection, "products", "minimum_stock", "REAL NOT NULL DEFAULT 0");
                EnsureColumn(connection, "products", "shelf_number", "TEXT");
                EnsureColumn(connection, "products", "shelf_location", "TEXT");
                EnsureColumn(connection, "work_order_items", "product_id", "INTEGER");
                EnsureColumn(connection, "sales", "cash_session_id", "INTEGER");
                EnsureColumn(connection, "financial_entries", "sale_id", "INTEGER");
                EnsureColumn(connection, "financial_entries", "supplier_id", "INTEGER");
                EnsureColumn(connection, "financial_entries", "debtor_id", "INTEGER");
                EnsureColumn(connection, "financial_entries", "purchase_id", "INTEGER");
                EnsureColumn(connection, "financial_entries", "work_order_id", "INTEGER");
                EnsureColumn(connection, "purchases", "status", "TEXT NOT NULL DEFAULT 'Finalizada'");
                EnsureColumn(connection, "purchases", "cancellation_reason", "TEXT");
                EnsureColumn(connection, "purchases", "cancelled_at", "TEXT");
                EnsureColumn(connection, "quotes", "approval_method", "TEXT");
                EnsureColumn(connection, "quotes", "approval_notes", "TEXT");
                EnsureColumn(connection, "work_orders", "quote_id", "INTEGER");
                EnsureColumn(connection, "work_orders", "quote_number", "TEXT");
                EnsureColumn(connection, "work_orders", "approval_history", "TEXT");
                EnsureColumn(connection, "work_orders", "warranty_days", "INTEGER NOT NULL DEFAULT 0");
                using (var categories = new SQLiteCommand("INSERT OR IGNORE INTO financial_categories (name,active) VALUES ('Vendas',1),('Serviços',1),('Compras',1),('Aluguel',1),('Energia',1),('Água',1),('Internet',1),('Salários',1),('Impostos',1),('Marketing',1),('Manutenção',1),('Outros',1);", connection)) categories.ExecuteNonQuery();
                using (var categories = new SQLiteCommand("INSERT OR IGNORE INTO product_categories (name,active) VALUES ('Acessórios',1),('Alimentos',1),('Bebidas',1),('Eletrônicos',1),('Higiene e limpeza',1),('Peças',1),('Vestuário',1),('Outros',1);", connection)) categories.ExecuteNonQuery();
            }
            LinkLegacyWorkOrderItems();
            RepairIncorrectCancellationReversals();
            SynchronizeCompletedWorkOrders();
        }

        private static void EnsureColumn(SQLiteConnection connection, string table, string column, string declaration)
        {
            using (var check = new SQLiteCommand("PRAGMA table_info(" + table + ")", connection))
            using (var reader = check.ExecuteReader()) { while (reader.Read()) if (string.Equals(Convert.ToString(reader["name"]), column, StringComparison.OrdinalIgnoreCase)) return; }
            using (var alter = new SQLiteCommand("ALTER TABLE " + table + " ADD COLUMN " + column + " " + declaration, connection)) alter.ExecuteNonQuery();
        }

        private void LinkLegacyWorkOrderItems()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(@"UPDATE work_order_items
SET product_id = (SELECT products.id FROM products WHERE lower(trim(products.name)) = lower(trim(work_order_items.description)) LIMIT 1)
WHERE item_type='Peça' AND product_id IS NULL
  AND EXISTS (SELECT 1 FROM products WHERE lower(trim(products.name)) = lower(trim(work_order_items.description)));", connection))
            { connection.Open(); command.ExecuteNonQuery(); }
        }

        private void SynchronizeCompletedWorkOrders()
        {
            var orders = Query("SELECT id, status FROM work_orders WHERE status IN ('Serviço concluído', 'Aguardando pagamento', 'Pronta para retirada', 'Finalizada', 'Cancelada')");
            foreach (DataRow order in orders.Rows) SynchronizeWorkOrderStock(Convert.ToInt64(order["id"]), Convert.ToString(order["status"]));
        }

        private void RepairIncorrectCancellationReversals()
        {
            var incorrect = Query("SELECT id, product_id, quantity FROM stock_movements WHERE movement_type='Estorno de OS' AND quantity < 0");
            foreach (DataRow movement in incorrect.Rows)
            {
                var correction = -2 * Convert.ToDecimal(movement["quantity"]);
                MoveStock(Convert.ToInt64(movement["product_id"]), correction, "Correção de estorno", "Correção automática de estorno registrado com sinal incorreto", null, null);
                using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var update = new SQLiteCommand("UPDATE stock_movements SET quantity=ABS(quantity) WHERE id=@id", connection)) { connection.Open(); update.Parameters.AddWithValue("@id", Convert.ToInt64(movement["id"])); update.ExecuteNonQuery(); }
            }
        }

        public void Set(string key, string value)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)", connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@key", key); command.Parameters.AddWithValue("@value", value); command.ExecuteNonQuery();
            }
        }

        public string Get(string key)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT value FROM settings WHERE key = @key", connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@key", key); return command.ExecuteScalar() as string;
            }
        }

        public bool IsActivated()
        {
            return string.Equals(Get("license_status"), "activated", StringComparison.OrdinalIgnoreCase);
        }

        public bool HasUsers()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT COUNT(1) FROM users", connection)) { connection.Open(); return Convert.ToInt32(command.ExecuteScalar()) > 0; }
        }

        public void CreateUser(string name, string email, string role, string password)
        { RequirePermission("settings.users"); 
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) throw new InvalidOperationException("Informe nome, e-mail e senha.");
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("INSERT INTO users (name,email,role,password_hash,active) VALUES (@name,@email,@role,@hash,1)", connection)) { connection.Open(); command.Parameters.AddWithValue("@name", name.Trim()); command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant()); command.Parameters.AddWithValue("@role", role); command.Parameters.AddWithValue("@hash", HashPassword(password)); command.ExecuteNonQuery(); }
            var user=Query("SELECT id FROM users WHERE lower(email)='"+email.Trim().ToLowerInvariant().Replace("'","''")+"'");
            if(user.Rows.Count>0) InitializeUserPermissions(Convert.ToInt64(user.Rows[0][0]),role);
            AddAudit("user_created", email);
        }

        public static readonly string[] PermissionKeys = { "dashboard.view", "clients.view", "clients.create", "clients.edit", "clients.delete", "catalog.view", "catalog.create", "catalog.edit", "catalog.delete", "stock.view", "stock.adjust", "sales.view", "sales.create", "sales.cancel", "cash.view", "cash.open", "cash.close", "cash.movement", "finance.view", "finance.manage", "reports.view", "purchases.view", "purchases.manage", "suppliers.view", "suppliers.manage", "service.view", "service.manage", "quotes.view", "quotes.manage", "settings.view", "settings.company", "settings.modules", "settings.users", "settings.backup", "settings.audit" };
        public static readonly string[] PermissionLabels = { "Painel: visualizar", "Clientes: visualizar", "Clientes: cadastrar", "Clientes: editar", "Clientes: excluir", "Catálogo: visualizar", "Catálogo: cadastrar", "Catálogo: editar", "Catálogo: excluir", "Estoque: visualizar", "Estoque: ajustar quantidade", "Vendas: visualizar", "Vendas: realizar venda", "Vendas: cancelar", "Caixa: visualizar", "Caixa: abrir", "Caixa: fechar", "Caixa: lançar suprimento/sangria", "Financeiro: visualizar", "Financeiro: incluir e liquidar", "Relatórios: visualizar", "Compras: visualizar", "Compras: registrar/cancelar", "Fornecedores: visualizar", "Fornecedores: gerenciar", "Serviços e OS: visualizar", "Serviços e OS: gerenciar", "Orçamentos: visualizar", "Orçamentos: gerenciar", "Configurações: acessar", "Empresa: alterar dados", "Módulos: alterar", "Usuários: gerenciar", "Backup: criar/restaurar", "Auditoria: visualizar" };
        private void InitializeUserPermissions(long userId,string role)
        {
            foreach(var key in PermissionKeys) SetUserPermission(userId,key,role=="Administrador" || (role=="Atendente" && (key.StartsWith("dashboard.")||key.StartsWith("clients.")||key.StartsWith("catalog.")||key.StartsWith("stock.")||key.StartsWith("sales.")||key.StartsWith("cash.")||key=="reports.view")) || (role=="Técnico" && (key.StartsWith("dashboard.")||key.StartsWith("clients.view")||key.StartsWith("service.")||key.StartsWith("quotes.view"))));
        }
        public bool HasPermission(long userId,string role,string key)
        {
            if(role=="Administrador") return true;
            using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var q=new SQLiteCommand("SELECT allowed FROM user_permissions WHERE user_id=@id AND permission_key=@key",c)) { c.Open();q.Parameters.AddWithValue("@id",userId);q.Parameters.AddWithValue("@key",key);var value=q.ExecuteScalar();return value!=null && Convert.ToInt32(value)==1; }
        }
        public void SetUserPermission(long userId,string key,bool allowed)
        { RequirePermission("settings.users"); 
            using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var q=new SQLiteCommand("INSERT OR REPLACE INTO user_permissions(user_id,permission_key,allowed) VALUES(@id,@key,@allowed)",c)) { c.Open();q.Parameters.AddWithValue("@id",userId);q.Parameters.AddWithValue("@key",key);q.Parameters.AddWithValue("@allowed",allowed?1:0);q.ExecuteNonQuery(); }
        }
        public bool[] GetUserPermissions(long userId)
        {
            var values=new bool[PermissionKeys.Length]; using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var q=new SQLiteCommand("SELECT permission_key,allowed FROM user_permissions WHERE user_id=@id",c)) { c.Open();q.Parameters.AddWithValue("@id",userId);using(var r=q.ExecuteReader()) while(r.Read()){var i=Array.IndexOf(PermissionKeys,Convert.ToString(r[0]));if(i>=0)values[i]=Convert.ToInt32(r[1])==1;} } return values;
        }

        public SystemUser Authenticate(string email, string password)
        {
            var normalized=(email ?? string.Empty).Trim().ToLowerInvariant();
            SystemUser user = null;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT id,name,email,role,password_hash,active FROM users WHERE lower(email)=@email", connection))
            {
                connection.Open();
                command.Parameters.AddWithValue("@email", normalized);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read() && Convert.ToInt32(reader[5]) == 1 && VerifyPassword(password, Convert.ToString(reader[4])))
                    {
                        user = new SystemUser { Id=Convert.ToInt64(reader[0]), Name=Convert.ToString(reader[1]), Email=Convert.ToString(reader[2]), Role=Convert.ToString(reader[3]) };
                    }
                }
            }

            // O leitor e a conexão acima precisam ser fechados antes que a auditoria
            // abra uma nova conexão de escrita no mesmo arquivo SQLite.
            AddAudit(user == null ? "login_failed" : "login_success", user == null ? normalized : user.Id.ToString());
            return user;
        }

        public DataTable GetUsers()
        {
            return Query("SELECT id AS 'Código',name AS 'Nome',email AS 'E-mail',role AS 'Perfil',CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM users ORDER BY name;");
        }

        public DataTable GetAuditLogs()
        {
            return Query(@"SELECT id AS 'Código', substr(created_at,1,19) AS 'Data e hora',
CASE action
 WHEN 'login_success' THEN 'Login realizado'
 WHEN 'login_failed' THEN 'Tentativa de login sem sucesso'
 WHEN 'user_created' THEN 'Usuário criado'
 WHEN 'user_status' THEN 'Status de usuário alterado'
 WHEN 'user_password_changed' THEN 'Senha alterada'
 WHEN 'password_recovery_requested' THEN 'Recuperação de senha solicitada'
 WHEN 'password_recovery_backup' THEN 'Backup de segurança para recuperação'
 WHEN 'administrator_recovered_signed' THEN 'Administrador recuperado por autorização RMC'
 WHEN 'database_backup' THEN 'Backup criado'
 WHEN 'database_restored' THEN 'Backup restaurado'
 WHEN 'appointment_saved' THEN 'Agendamento salvo'
 WHEN 'appointment_confirmed' THEN 'Agendamento confirmado'
 WHEN 'appointment_cancelled' THEN 'Agendamento cancelado'
 WHEN 'appointment_converted' THEN 'Agendamento convertido em OS'
 WHEN 'work_order_saved' THEN 'Ordem de Serviço salva'
 WHEN 'work_order_status' THEN 'Status da OS alterado'
 WHEN 'work_order_received' THEN 'Pagamento da OS registrado'
 WHEN 'work_order_item_saved' THEN 'Item adicionado à OS'
 WHEN 'work_order_item_deleted' THEN 'Item removido da OS'
 WHEN 'stock_moved' THEN 'Movimentação de estoque'
 WHEN 'quote_saved' THEN 'Orçamento salvo'
 WHEN 'quote_item_saved' THEN 'Item adicionado ao orçamento'
 WHEN 'quote_item_deleted' THEN 'Item removido do orçamento'
 WHEN 'quote_approved' THEN 'Orçamento aprovado'
 WHEN 'quote_rejected' THEN 'Orçamento reprovado'
 WHEN 'quote_converted' THEN 'Orçamento convertido em OS'
 WHEN 'purchase_saved' THEN 'Compra registrada'
 WHEN 'purchase_cancelled' THEN 'Compra cancelada / devolvida'
 WHEN 'supplier_saved' THEN 'Fornecedor salvo'
 WHEN 'supplier_deleted' THEN 'Fornecedor excluído'
 WHEN 'debtor_saved' THEN 'Devedor salvo'
 WHEN 'debtor_deleted' THEN 'Devedor excluído'
 WHEN 'financial_entry_saved' THEN 'Conta financeira salva'
 WHEN 'financial_entry_paid' THEN 'Conta financeira baixada'
 WHEN 'financial_entry_cancelled' THEN 'Conta financeira cancelada'
 WHEN 'financial_category_saved' THEN 'Categoria financeira salva'
 WHEN 'financial_category_deleted' THEN 'Categoria financeira excluída'
 WHEN 'product_category_saved' THEN 'Categoria de produto salva'
 WHEN 'cash_opened' THEN 'Caixa aberto'
 WHEN 'cash_closed' THEN 'Caixa fechado'
 WHEN 'cash_movement' THEN 'Movimentação de caixa'
 WHEN 'sale_saved' THEN 'Venda realizada'
 WHEN 'client_saved' THEN 'Cliente salvo'
 WHEN 'client_deleted' THEN 'Cliente excluído'
 WHEN 'vehicle_saved' THEN 'Equipamento salvo'
 WHEN 'vehicle_deleted' THEN 'Equipamento excluído'
 WHEN 'product_saved' THEN 'Produto salvo'
 WHEN 'service_saved' THEN 'Serviço salvo'
 ELSE action END AS 'Ação',
CASE
 WHEN action='appointment_confirmed' AND details_json GLOB '[0-9]*' THEN 'Agendamento nº ' || details_json || ' confirmado'
 WHEN action='appointment_cancelled' AND details_json GLOB '[0-9]*' THEN 'Agendamento nº ' || details_json || ' cancelado'
 WHEN action='appointment_converted' THEN 'Agendamento nº ' || details_json
 WHEN action='work_order_status' THEN 'OS nº ' || details_json
 WHEN action='quote_approved' THEN 'Orçamento nº ' || details_json
 WHEN action='quote_converted' THEN 'Orçamento nº ' || replace(details_json,'->',' gerou a OS')
 WHEN action='quote_rejected' THEN 'Orçamento nº ' || details_json
 WHEN action='quote_saved' THEN 'Orçamento ' || details_json
 WHEN action='purchase_saved' THEN 'Compra ' || details_json
 WHEN action='purchase_cancelled' THEN 'Compra ' || details_json
 ELSE COALESCE(details_json,'') END AS 'Detalhes'
FROM audit_logs ORDER BY id DESC LIMIT 1000;");
        }

        public void SetUserActive(long id, bool active)
        { RequirePermission("settings.users"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE users SET active=@active WHERE id=@id", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", id); command.Parameters.AddWithValue("@active", active ? 1 : 0); command.ExecuteNonQuery(); }
            AddAudit("user_status", id + ": " + active);
        }

        public void ChangeUserPassword(long id, string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6) throw new InvalidOperationException("Use uma senha com pelo menos 6 caracteres.");
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE users SET password_hash=@hash WHERE id=@id", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", id); command.Parameters.AddWithValue("@hash", HashPassword(password)); if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("Usuário não encontrado."); }
            AddAudit("user_password_changed", id.ToString());
        }

        public void RecoverAdministratorAccess(string licenseCode, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(licenseCode) || !string.Equals(Get("license_code"), licenseCode.Trim(), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Código da licença inválido.");
            ResetAdministratorPassword(email, password, "administrator_recovered");
        }

        public string CreatePasswordRecoveryRequest()
        {
            if (!IsActivated()) throw new InvalidOperationException("Esta instalação não está ativada.");
            var requestCode=Get("license_request_code");
            if (string.IsNullOrWhiteSpace(requestCode)) throw new InvalidOperationException("Código de solicitação da licença não encontrado.");
            var bytes=new byte[12]; using(var random=RandomNumberGenerator.Create()) random.GetBytes(bytes);
            var nonce=BitConverter.ToString(bytes).Replace("-", string.Empty);
            Set("password_recovery_nonce",nonce); Set("password_recovery_requested_at",DateTime.UtcNow.ToString("o"));
            AddAudit("password_recovery_requested",requestCode);
            return "RMCREC1|"+requestCode+"|"+nonce;
        }

        public void CompletePasswordRecovery(string authorization, string email, string password, string publicKeyPath)
        {
            string result;
            if(!LicenseService.TryValidatePasswordRecovery(authorization,Get("license_code"),Get("license_request_code"),Get("password_recovery_nonce"),publicKeyPath,out result)) throw new InvalidOperationException(result);
            CreateRecoverySafetyBackup();
            ResetAdministratorPassword(email,password,"administrator_recovered_signed");
            Set("password_recovery_nonce",string.Empty); Set("password_recovery_used_at",DateTime.UtcNow.ToString("o"));
        }

        private void CreateRecoverySafetyBackup()
        {
            var directory=Path.Combine(Path.GetDirectoryName(_databasePath),"recovery-backups"); Directory.CreateDirectory(directory);
            var target=Path.Combine(directory,"antes-da-recuperacao-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".db");
            File.Copy(_databasePath,target,false); AddAudit("password_recovery_backup",target);
        }

        private void ResetAdministratorPassword(string email, string password, string auditAction)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Informe o e-mail do administrador.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8) throw new InvalidOperationException("Use uma nova senha com pelo menos 8 caracteres.");
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE users SET password_hash=@hash, active=1 WHERE lower(email)=@email AND role='Administrador'", connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@hash", HashPassword(password)); command.Parameters.AddWithValue("@email", email.Trim().ToLowerInvariant());
                if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("Administrador não encontrado para este e-mail.");
            }
            AddAudit(auditAction, email.Trim().ToLowerInvariant());
        }

        private static string HashPassword(string password)
        {
            var salt = new byte[16]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt); using (var derive = new Rfc2898DeriveBytes(password, salt, 120000)) { var hash = derive.GetBytes(32); return "120000." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash); }
        }

        private static bool VerifyPassword(string password, string stored)
        {
            try { var parts = (stored ?? string.Empty).Split('.'); if (parts.Length != 3) return false; var iterations=Convert.ToInt32(parts[0]);var salt=Convert.FromBase64String(parts[1]);var expected=Convert.FromBase64String(parts[2]);using(var derive=new Rfc2898DeriveBytes(password ?? string.Empty,salt,iterations)){var actual=derive.GetBytes(expected.Length);if(actual.Length!=expected.Length)return false;var result=0;for(var i=0;i<actual.Length;i++)result|=actual[i]^expected[i];return result==0;}} catch { return false; }
        }

        public string CreateBackup(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Informe a pasta de destino do backup.");
            Directory.CreateDirectory(directory);
            var fileName = "rmc-flow-comercio-local-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db";
            var target = Path.Combine(directory, fileName);
            File.Copy(_databasePath, target, false);
            AddAudit("database_backup", target);
            return target;
        }

        public string RestoreBackup(string backupPath)
        {
            if (string.IsNullOrWhiteSpace(backupPath) || !File.Exists(backupPath)) throw new FileNotFoundException("Arquivo de backup não encontrado.");
            if (string.Equals(Path.GetFullPath(backupPath), Path.GetFullPath(_databasePath), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Escolha um arquivo de backup, não a base em uso.");
            using (var source = new SQLiteConnection("Data Source=" + backupPath + ";Version=3;Read Only=True;")) using (var check = new SQLiteCommand("SELECT COUNT(1) FROM sqlite_master WHERE type='table' AND name='work_orders'", source)) { source.Open(); if (Convert.ToInt32(check.ExecuteScalar()) != 1) throw new InvalidOperationException("O arquivo escolhido não é um backup válido do RMC Flow Comércio Local."); }
            var safetyCopy = Path.Combine(Path.GetDirectoryName(_databasePath), "antes-da-restauracao-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".db");
            File.Copy(_databasePath, safetyCopy, false);
            File.Copy(backupPath, _databasePath, true);
            AddAudit("database_restored", backupPath);
            return safetyCopy;
        }

        public DataTable GetClients()
        {
            return Query("SELECT id AS 'Código', name AS 'Nome', document AS 'CPF/CNPJ', phone AS 'Telefone', email AS 'E-mail' FROM clients ORDER BY name;");
        }

        public DataTable GetVehicles()
        {
            return Query(@"SELECT vehicles.id AS 'Código', clients.name AS 'Cliente', vehicles.type AS 'Tipo', vehicles.plate AS 'Identificador', vehicles.manufacturer AS 'Marca', vehicles.model AS 'Modelo', vehicles.year_model AS 'Versão'
FROM vehicles INNER JOIN clients ON clients.id = vehicles.client_id ORDER BY clients.name, vehicles.model;");
        }

        public DataTable FindVehicles(string search)
        {
            var value=(search??string.Empty).Replace("'","''");
            return Query("SELECT vehicles.id AS 'Código', clients.name AS 'Cliente', vehicles.type AS 'Tipo', vehicles.plate AS 'Identificador', vehicles.manufacturer AS 'Marca', vehicles.model AS 'Modelo', vehicles.year_model AS 'Versão' FROM vehicles INNER JOIN clients ON clients.id=vehicles.client_id WHERE clients.name LIKE '%"+value+"%' OR COALESCE(vehicles.plate,'') LIKE '%"+value+"%' OR COALESCE(vehicles.manufacturer,'') LIKE '%"+value+"%' OR vehicles.model LIKE '%"+value+"%' OR vehicles.type LIKE '%"+value+"%' ORDER BY clients.name,vehicles.model;");
        }

        public DataTable GetVehicleHistory(long vehicleId)
        {
            return Query(@"SELECT work_orders.id AS 'Código', work_orders.number AS 'OS', substr(work_orders.opened_at,1,16) AS 'Entrada', work_orders.status AS 'Status', work_orders.complaint AS 'Problema relatado', COALESCE((SELECT GROUP_CONCAT(item_type || ': ' || description, char(10)) FROM work_order_items WHERE work_order_id=work_orders.id),'Sem itens lançados') AS 'Serviços e peças', printf('R$ %.2f',COALESCE((SELECT SUM(quantity*unit_price) FROM work_order_items WHERE work_order_id=work_orders.id),0)) AS 'Total'
FROM work_orders WHERE vehicle_id=" + vehicleId + " ORDER BY work_orders.id DESC;");
        }

        public DataTable GetVehicleHistoryHeader(long vehicleId)
        {
            return Query(@"SELECT clients.name AS client_name, vehicles.type, COALESCE(vehicles.manufacturer,'') AS manufacturer, vehicles.model, COALESCE(vehicles.year_model,'') AS year_model, COALESCE(vehicles.plate,'') AS plate
FROM vehicles INNER JOIN clients ON clients.id=vehicles.client_id WHERE vehicles.id=" + vehicleId + ";");
        }

        public DataTable GetClientLookup()
        {
            return Query("SELECT id, name FROM clients ORDER BY name;");
        }
        public DataTable GetSaleClientLookup() { return Query("SELECT 0 AS id, 'Consumidor não identificado' AS name UNION ALL SELECT id, name FROM clients ORDER BY name;"); }
        public DataTable FindClients(string search) { var value=(search??string.Empty).Replace("'", "''"); return Query("SELECT id AS 'Código', name AS 'Nome', document AS 'CPF/CNPJ', phone AS 'Telefone', email AS 'E-mail' FROM clients WHERE name LIKE '%"+value+"%' OR COALESCE(document,'') LIKE '%"+value+"%' OR COALESCE(phone,'') LIKE '%"+value+"%' OR COALESCE(email,'') LIKE '%"+value+"%' ORDER BY name;"); }

        public DataTable GetVehicleLookup()
        {
            return Query(@"SELECT vehicles.id, clients.name || ' — ' || vehicles.type || ' ' || COALESCE(vehicles.manufacturer, '') || ' ' || vehicles.model || CASE WHEN COALESCE(vehicles.plate, '') = '' THEN '' ELSE ' (' || vehicles.plate || ')' END AS display_name
FROM vehicles INNER JOIN clients ON clients.id = vehicles.client_id ORDER BY clients.name, vehicles.model;");
        }

        public bool VehicleBelongsToClient(long vehicleId, long clientId)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COUNT(1) FROM vehicles WHERE id=@vehicle_id AND client_id=@client_id", connection))
            { connection.Open(); command.Parameters.AddWithValue("@vehicle_id", vehicleId); command.Parameters.AddWithValue("@client_id", clientId); return Convert.ToInt64(command.ExecuteScalar()) == 1; }
        }

        public DataTable GetWorkOrders()
        {
            return FindWorkOrders("Todos", string.Empty);
        }

        public DataTable FindWorkOrders(string status, string search)
        {
            var value=(search??string.Empty).Replace("'","''");var statusClause=string.IsNullOrWhiteSpace(status)||status=="Todos"?string.Empty:" AND work_orders.status='"+status.Replace("'","''")+"'";
            return Query(@"SELECT work_orders.id AS 'Código', work_orders.number AS 'OS', clients.name AS 'Cliente', vehicles.type || ' ' || COALESCE(vehicles.manufacturer, '') || ' ' || vehicles.model AS 'Equipamento', work_orders.status AS 'Status', CASE WHEN EXISTS(SELECT 1 FROM financial_entries WHERE work_order_id=work_orders.id AND status='Pago') THEN 'Quitada' ELSE 'Pendente' END AS 'Pagamento', COALESCE(work_orders.quote_number,'') AS 'Origem', substr(work_orders.opened_at, 1, 10) AS 'Entrada', COALESCE(work_orders.expected_at, '') AS 'Previsão', work_orders.client_id AS client_id, work_orders.vehicle_id AS vehicle_id, work_orders.mileage AS estado_entrada, work_orders.fuel_level AS acessorios_recebidos, work_orders.complaint AS reclamacao, work_orders.diagnosis AS diagnostico, work_orders.notes AS observacoes, COALESCE(work_orders.approval_history,'') AS historico_aprovacao
FROM work_orders INNER JOIN clients ON clients.id=work_orders.client_id INNER JOIN vehicles ON vehicles.id=work_orders.vehicle_id WHERE (work_orders.number LIKE '%"+value+"%' OR clients.name LIKE '%"+value+"%' OR vehicles.model LIKE '%"+value+"%' OR COALESCE(vehicles.plate,'') LIKE '%"+value+"%')"+statusClause+" ORDER BY work_orders.id DESC;");
        }

        public DataTable GetAppointments()
        {
            return Query(@"SELECT appointments.id AS 'Código', appointments.scheduled_at AS 'Data e hora', clients.name AS 'Cliente', vehicles.type || ' ' || COALESCE(vehicles.manufacturer, '') || ' ' || vehicles.model AS 'Equipamento', appointments.service_description AS 'Serviço previsto', appointments.status AS 'Status', COALESCE(work_orders.number,'') AS 'OS gerada', COALESCE(appointments.notes,'') AS 'Observações'
FROM appointments INNER JOIN clients ON clients.id=appointments.client_id INNER JOIN vehicles ON vehicles.id=appointments.vehicle_id LEFT JOIN work_orders ON work_orders.id=appointments.work_order_id ORDER BY appointments.scheduled_at, appointments.id DESC;");
        }

        public DataTable GetAppointmentForEdit(long appointmentId)
        {
            return Query("SELECT id, client_id, vehicle_id, scheduled_at, service_description, notes, status, work_order_id FROM appointments WHERE id=" + appointmentId + ";");
        }

        public DataTable GetAppointmentDocument(long appointmentId)
        {
            return Query(@"SELECT appointments.id, appointments.scheduled_at, appointments.service_description, appointments.status, clients.name AS client_name, clients.phone AS client_phone, vehicles.type AS vehicle_type, COALESCE(vehicles.manufacturer,'') AS manufacturer, vehicles.model, COALESCE(vehicles.plate,'') AS plate
FROM appointments INNER JOIN clients ON clients.id=appointments.client_id INNER JOIN vehicles ON vehicles.id=appointments.vehicle_id WHERE appointments.id=" + appointmentId + ";");
        }

        public void SaveAppointment(long? id, long clientId, long vehicleId, DateTime scheduledAt, string serviceDescription, string notes, string status)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(@"INSERT INTO appointments (id,client_id,vehicle_id,scheduled_at,service_description,notes,status,created_at) VALUES (@id,@client,@vehicle,@scheduled,@service,@notes,@status,@created)
ON CONFLICT(id) DO UPDATE SET client_id=@client,vehicle_id=@vehicle,scheduled_at=@scheduled,service_description=@service,notes=@notes,status=@status;", connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@client", clientId); command.Parameters.AddWithValue("@vehicle", vehicleId); command.Parameters.AddWithValue("@scheduled", scheduledAt.ToString("yyyy-MM-dd HH:mm:ss")); command.Parameters.AddWithValue("@service", serviceDescription); command.Parameters.AddWithValue("@notes", notes); command.Parameters.AddWithValue("@status", status); command.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); command.ExecuteNonQuery();
            }
            AddAudit("appointment_saved", serviceDescription);
        }

        public void CancelAppointment(long appointmentId)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE appointments SET status='Cancelado' WHERE id=@id AND work_order_id IS NULL", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", appointmentId); if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("Este agendamento já foi convertido em OS e não pode ser cancelado."); }
            AddAudit("appointment_cancelled", appointmentId.ToString());
        }

        public void ConfirmAppointment(long appointmentId)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE appointments SET status='Confirmado' WHERE id=@id AND status='Agendado' AND work_order_id IS NULL", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", appointmentId); if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("Somente agendamentos em aberto podem ser confirmados."); }
            AddAudit("appointment_confirmed", appointmentId.ToString());
        }

        public string ConvertAppointmentToWorkOrder(long appointmentId)
        { RequirePermission("service.manage"); 
            long clientId, vehicleId; string scheduledAt, service, notes; using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT client_id,vehicle_id,scheduled_at,service_description,COALESCE(notes,'') FROM appointments WHERE id=@id AND status IN ('Agendado','Confirmado') AND work_order_id IS NULL", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", appointmentId); using (var reader = command.ExecuteReader()) { if (!reader.Read()) throw new InvalidOperationException("Somente agendamentos ativos, ainda sem OS, podem ser convertidos."); clientId=Convert.ToInt64(reader[0]); vehicleId=Convert.ToInt64(reader[1]); scheduledAt=Convert.ToString(reader[2]); service=Convert.ToString(reader[3]); notes=Convert.ToString(reader[4]); } }
            var number = NextWorkOrderNumber(); SaveWorkOrder(null, number, clientId, vehicleId, "Aberta", scheduledAt, string.Empty, string.Empty, service, string.Empty, "Gerada a partir do agendamento de " + scheduledAt + (string.IsNullOrWhiteSpace(notes) ? string.Empty : ". Observações: " + notes), 0);
            long workOrderId; using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var find = new SQLiteCommand("SELECT id FROM work_orders WHERE number=@number", connection)) { connection.Open(); find.Parameters.AddWithValue("@number", number); workOrderId=Convert.ToInt64(find.ExecuteScalar()); using (var update = new SQLiteCommand("UPDATE appointments SET status='Convertido',work_order_id=@os WHERE id=@id", connection)) { update.Parameters.AddWithValue("@os", workOrderId); update.Parameters.AddWithValue("@id", appointmentId); update.ExecuteNonQuery(); } }
            AddAudit("appointment_converted", appointmentId + ": " + number); return number;
        }

        public DataTable GetWorkOrderForEdit(long workOrderId)
        {
            return Query("SELECT id, number, client_id, vehicle_id, status, expected_at, mileage, fuel_level, complaint, diagnosis, notes, COALESCE(warranty_days,0) AS warranty_days FROM work_orders WHERE id=" + workOrderId + ";");
        }

        public DataTable GetQuotes()
        {
            return Query(@"SELECT quotes.id AS 'Código', quotes.number AS 'Orçamento', clients.name AS 'Cliente', vehicles.type || ' ' || COALESCE(vehicles.manufacturer, '') || ' ' || vehicles.model AS 'Equipamento', quotes.status AS 'Status', substr(quotes.created_at,1,10) AS 'Emissão', COALESCE(quotes.valid_until,'') AS 'Validade', printf('R$ %.2f', COALESCE((SELECT SUM(quantity * unit_price) FROM quote_items WHERE quote_id=quotes.id),0)) AS 'Total', quotes.client_id AS client_id, quotes.vehicle_id AS vehicle_id, COALESCE(quotes.complaint,'') AS reclamacao, COALESCE(quotes.notes,'') AS observacoes, quotes.converted_work_order_id AS os_convertida FROM quotes INNER JOIN clients ON clients.id=quotes.client_id INNER JOIN vehicles ON vehicles.id=quotes.vehicle_id ORDER BY quotes.id DESC;");
        }

        public DataTable GetQuoteItems(long quoteId)
        {
            return Query("SELECT id AS 'Código', item_type AS 'Tipo', description AS 'Descrição', printf('%.2f', quantity) AS 'Qtd.', printf('R$ %.2f', unit_price) AS 'Valor unitário', printf('R$ %.2f', quantity * unit_price) AS 'Total' FROM quote_items WHERE quote_id=" + quoteId + " ORDER BY id;");
        }

        public decimal GetQuoteSubtotal(long quoteId)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(SUM(quantity * unit_price), 0) FROM quote_items WHERE quote_id=@id", connection))
            { connection.Open(); command.Parameters.AddWithValue("@id", quoteId); return Convert.ToDecimal(command.ExecuteScalar()); }
        }

        public long GetQuoteId(string number)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT id FROM quotes WHERE number=@number", connection)) { connection.Open(); command.Parameters.AddWithValue("@number", number); return Convert.ToInt64(command.ExecuteScalar()); }
        }

        public DataTable GetQuoteDocument(long quoteId)
        {
            return Query(@"SELECT quotes.number,quotes.status,quotes.created_at,quotes.valid_until,COALESCE(quotes.complaint,'') AS complaint,COALESCE(quotes.notes,'') AS notes,COALESCE(quotes.approved_at,'') AS approved_at,COALESCE(quotes.approval_method,'') AS approval_method,COALESCE(quotes.approval_notes,'') AS approval_notes,clients.name AS client_name,clients.document AS client_document,clients.phone AS client_phone,clients.email AS client_email,vehicles.type AS vehicle_type,vehicles.plate,vehicles.manufacturer,vehicles.model,vehicles.year_model FROM quotes INNER JOIN clients ON clients.id=quotes.client_id INNER JOIN vehicles ON vehicles.id=quotes.vehicle_id WHERE quotes.id=" + quoteId + ";");
        }

        public DataTable GetVehicleLookupByClient(long clientId)
        {
            return Query(@"SELECT vehicles.id, vehicles.type || ' — ' || COALESCE(vehicles.manufacturer, '') || ' ' || vehicles.model || CASE WHEN COALESCE(vehicles.plate, '') = '' THEN '' ELSE ' (' || vehicles.plate || ')' END AS display_name FROM vehicles WHERE vehicles.client_id=" + clientId + " ORDER BY vehicles.model;");
        }

        public string NextQuoteNumber()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM quotes", connection))
            { connection.Open(); return "ORC-" + DateTime.Now.Year + "-" + Convert.ToInt64(command.ExecuteScalar()).ToString("000000"); }
        }

        public void SaveQuote(long? id, string number, long clientId, long vehicleId, string validUntil, string complaint, string notes)
        { RequirePermission("quotes.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(@"INSERT INTO quotes (id,number,client_id,vehicle_id,status,valid_until,complaint,notes,created_at) VALUES (@id,@number,@client,@vehicle,'Em aberto',@valid,@complaint,@notes,@created)
ON CONFLICT(id) DO UPDATE SET client_id=@client,vehicle_id=@vehicle,valid_until=@valid,complaint=@complaint,notes=@notes;", connection))
            { connection.Open(); command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@number", number); command.Parameters.AddWithValue("@client", clientId); command.Parameters.AddWithValue("@vehicle", vehicleId); command.Parameters.AddWithValue("@valid", validUntil); command.Parameters.AddWithValue("@complaint", complaint); command.Parameters.AddWithValue("@notes", notes); command.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); command.ExecuteNonQuery(); }
            AddAudit("quote_saved", number);
        }

        public void SaveQuoteItem(long quoteId, long? productId, string itemType, string description, decimal quantity, decimal unitPrice)
        { RequirePermission("quotes.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO quote_items (quote_id,product_id,item_type,description,quantity,unit_price,created_at) VALUES (@quote,@product,@type,@description,@quantity,@price,@created)", connection))
            { connection.Open(); command.Parameters.AddWithValue("@quote", quoteId); command.Parameters.AddWithValue("@product", productId.HasValue ? (object)productId.Value : DBNull.Value); command.Parameters.AddWithValue("@type", itemType); command.Parameters.AddWithValue("@description", description); command.Parameters.AddWithValue("@quantity", quantity); command.Parameters.AddWithValue("@price", unitPrice); command.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); command.ExecuteNonQuery(); }
            AddAudit("quote_item_saved", description);
        }

        public void DeleteQuoteItem(long id) { RequirePermission("quotes.manage");  Execute("DELETE FROM quote_items WHERE id=@id;", id); AddAudit("quote_item_deleted", id.ToString()); }

        public void ApproveQuote(long quoteId, string method, string notes)
        { RequirePermission("quotes.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE quotes SET status='Aprovado', approved_at=@approved, approval_method=@method, approval_notes=@notes WHERE id=@id AND status='Em aberto';", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", quoteId); command.Parameters.AddWithValue("@approved", DateTime.Now.ToString("dd/MM/yyyy HH:mm")); command.Parameters.AddWithValue("@method", method); command.Parameters.AddWithValue("@notes", notes); command.ExecuteNonQuery(); } AddAudit("quote_approved", quoteId + ": " + method);
        }

        public void RejectQuote(long quoteId, string reason)
        { RequirePermission("quotes.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE quotes SET status='Reprovado', rejected_at=@rejected, rejection_reason=@reason WHERE id=@id AND status IN ('Em aberto','Aprovado');", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", quoteId); command.Parameters.AddWithValue("@rejected", DateTime.UtcNow.ToString("o")); command.Parameters.AddWithValue("@reason", reason); command.ExecuteNonQuery(); } AddAudit("quote_rejected", quoteId.ToString());
        }

        public string ConvertQuoteToWorkOrder(long quoteId)
        { RequirePermission("quotes.manage"); 
            string convertedNumber = null;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            {
                connection.Open(); using (var tx = connection.BeginTransaction())
                {
                    long clientId, vehicleId; string status, complaint, notes, existingOs, quoteNumber, approval; using (var quote = new SQLiteCommand("SELECT client_id,vehicle_id,status,COALESCE(complaint,''),COALESCE(notes,''),converted_work_order_id,COALESCE(approval_method,''),COALESCE(approved_at,''),COALESCE(approval_notes,''),number FROM quotes WHERE id=@id", connection, tx)) { quote.Parameters.AddWithValue("@id", quoteId); using (var r = quote.ExecuteReader()) { if (!r.Read()) throw new InvalidOperationException("Orçamento não encontrado."); clientId=Convert.ToInt64(r[0]); vehicleId=Convert.ToInt64(r[1]); status=Convert.ToString(r[2]); complaint=Convert.ToString(r[3]); notes=Convert.ToString(r[4]); existingOs=Convert.ToString(r[5]); approval="Aprovação do orçamento: "+Convert.ToString(r[6])+" em "+Convert.ToString(r[7])+(string.IsNullOrWhiteSpace(Convert.ToString(r[8]))?"":". Observação: "+Convert.ToString(r[8])); quoteNumber=Convert.ToString(r[9]); notes+=(string.IsNullOrWhiteSpace(notes)?"":"\r\n")+approval; } }
                    if (status != "Aprovado") throw new InvalidOperationException("Apenas orçamento aprovado pode ser convertido em OS."); if (!string.IsNullOrWhiteSpace(existingOs)) throw new InvalidOperationException("Este orçamento já foi convertido em OS.");
                    var number = "OS-" + DateTime.Now.Year + "-"; using (var next = new SQLiteCommand("SELECT COALESCE(MAX(id),0)+1 FROM work_orders", connection, tx)) number += Convert.ToInt64(next.ExecuteScalar()).ToString("000000");
                    long osId; using (var insert = new SQLiteCommand("INSERT INTO work_orders (number,client_id,vehicle_id,status,opened_at,expected_at,mileage,fuel_level,complaint,diagnosis,notes,quote_id,quote_number,approval_history) VALUES (@number,@client,@vehicle,'Aprovada',@opened,'','','',@complaint,'',@notes,@quote,@quote_number,@approval); SELECT last_insert_rowid();", connection, tx)) { insert.Parameters.AddWithValue("@number",number);insert.Parameters.AddWithValue("@client",clientId);insert.Parameters.AddWithValue("@vehicle",vehicleId);insert.Parameters.AddWithValue("@opened",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));insert.Parameters.AddWithValue("@complaint",string.IsNullOrWhiteSpace(complaint)?"Convertida do orçamento":complaint);insert.Parameters.AddWithValue("@notes",notes);insert.Parameters.AddWithValue("@quote",quoteId);insert.Parameters.AddWithValue("@quote_number",quoteNumber);insert.Parameters.AddWithValue("@approval",approval);osId=Convert.ToInt64(insert.ExecuteScalar()); }
                    using (var copy = new SQLiteCommand("INSERT INTO work_order_items (work_order_id,product_id,item_type,description,quantity,unit_price,created_at) SELECT @os,product_id,item_type,description,quantity,unit_price,@created FROM quote_items WHERE quote_id=@quote", connection, tx)) { copy.Parameters.AddWithValue("@os",osId);copy.Parameters.AddWithValue("@quote",quoteId);copy.Parameters.AddWithValue("@created",DateTime.UtcNow.ToString("o"));copy.ExecuteNonQuery(); }
                    using (var update = new SQLiteCommand("UPDATE quotes SET status='Convertido',converted_work_order_id=@os WHERE id=@id", connection, tx)) { update.Parameters.AddWithValue("@os",osId);update.Parameters.AddWithValue("@id",quoteId);update.ExecuteNonQuery(); }
                    tx.Commit();
                    // A auditoria é feita após a transação/conexão serem liberadas.
                    // Ver retorno abaixo.
                    convertedNumber = number;
                }
            }
            AddAudit("quote_converted", quoteId + " -> " + convertedNumber);
            return convertedNumber;
        }

        public DataTable GetWorkOrderItems(long workOrderId)
        {
            return Query("SELECT id AS 'Código', item_type AS 'Tipo', description AS 'Descrição', printf('%.2f', quantity) AS 'Qtd.', printf('R$ %.2f', unit_price) AS 'Valor unitário', printf('R$ %.2f', quantity * unit_price) AS 'Total' FROM work_order_items WHERE work_order_id=" + workOrderId + " ORDER BY id;");
        }

        public DataTable GetWorkOrderDocument(long workOrderId)
        {
            return Query(@"SELECT work_orders.number, work_orders.status, work_orders.opened_at, work_orders.expected_at, work_orders.mileage, work_orders.fuel_level, work_orders.complaint, work_orders.diagnosis, work_orders.notes, COALESCE(work_orders.warranty_days,0) AS warranty_days, COALESCE(work_orders.finalized_at,'') AS finalized_at, COALESCE(work_orders.quote_number,'') AS quote_number, COALESCE(work_orders.approval_history,'') AS approval_history, COALESCE((SELECT payment_method FROM financial_entries WHERE work_order_id=work_orders.id AND status='Pago' ORDER BY id DESC LIMIT 1),'') AS payment_method, COALESCE((SELECT paid_at FROM financial_entries WHERE work_order_id=work_orders.id AND status='Pago' ORDER BY id DESC LIMIT 1),'') AS paid_at, clients.name AS client_name, clients.document AS client_document, clients.phone AS client_phone, clients.email AS client_email, vehicles.type AS vehicle_type, vehicles.plate, vehicles.manufacturer, vehicles.model, vehicles.year_model
FROM work_orders INNER JOIN clients ON clients.id=work_orders.client_id INNER JOIN vehicles ON vehicles.id=work_orders.vehicle_id WHERE work_orders.id=" + workOrderId + ";");
        }

        public DataTable GetWarrantyOrders()
        {
            return Query(@"SELECT work_orders.id AS 'Código', work_orders.number AS 'OS', clients.name AS 'Cliente', vehicles.type || ' ' || COALESCE(vehicles.manufacturer,'') || ' ' || vehicles.model AS 'Equipamento', work_orders.finalized_at AS 'Finalizada em', work_orders.warranty_days AS 'Garantia (dias)', date(work_orders.finalized_at, '+' || work_orders.warranty_days || ' days') AS 'Válida até', CASE WHEN date(work_orders.finalized_at, '+' || work_orders.warranty_days || ' days') >= date('now','localtime') THEN 'Em garantia' ELSE 'Vencida' END AS 'Situação'
FROM work_orders INNER JOIN clients ON clients.id=work_orders.client_id INNER JOIN vehicles ON vehicles.id=work_orders.vehicle_id WHERE work_orders.status='Finalizada' AND COALESCE(work_orders.warranty_days,0)>0 ORDER BY date(work_orders.finalized_at, '+' || work_orders.warranty_days || ' days') ASC;");
        }

        public DataTable GetProducts()
        {
            return Query("SELECT id AS 'Código', code AS 'Código interno', barcode AS 'Código de barras', name AS 'Produto / peça', category AS 'Categoria', unit AS 'Unidade', COALESCE(shelf_number,'') AS 'Prateleira', COALESCE(shelf_location,'') AS 'Localização', printf('R$ %.2f', sale_price) AS 'Preço de venda', CASE WHEN stock_quantity=CAST(stock_quantity AS INTEGER) THEN CAST(stock_quantity AS INTEGER) ELSE printf('%.2f', stock_quantity) END AS 'Em estoque', CASE WHEN minimum_stock=CAST(minimum_stock AS INTEGER) THEN CAST(minimum_stock AS INTEGER) ELSE printf('%.2f', minimum_stock) END AS 'Estoque mínimo', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM products ORDER BY name;");
        }

        public DataTable GetServices()
        {
            return Query("SELECT id AS 'Código', code AS 'Código interno', name AS 'Serviço', category AS 'Categoria', estimated_duration AS 'Duração estimada', printf('R$ %.2f', sale_price) AS 'Preço padrão', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM services ORDER BY name;");
        }

        public DataTable FindProductsForList(string search)
        {
            var value=(search??string.Empty).Replace("'","''");return Query("SELECT id AS 'Código', code AS 'Código interno', barcode AS 'Código de barras', name AS 'Produto / peça', category AS 'Categoria', unit AS 'Unidade', COALESCE(shelf_number,'') AS 'Prateleira', COALESCE(shelf_location,'') AS 'Localização', printf('R$ %.2f', sale_price) AS 'Preço de venda', CASE WHEN stock_quantity=CAST(stock_quantity AS INTEGER) THEN CAST(stock_quantity AS INTEGER) ELSE printf('%.2f', stock_quantity) END AS 'Em estoque', CASE WHEN minimum_stock=CAST(minimum_stock AS INTEGER) THEN CAST(minimum_stock AS INTEGER) ELSE printf('%.2f', minimum_stock) END AS 'Estoque mínimo', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM products WHERE COALESCE(code,'') LIKE '%"+value+"%' OR COALESCE(barcode,'') LIKE '%"+value+"%' OR name LIKE '%"+value+"%' OR COALESCE(category,'') LIKE '%"+value+"%' OR COALESCE(shelf_number,'') LIKE '%"+value+"%' OR COALESCE(shelf_location,'') LIKE '%"+value+"%' ORDER BY name;");
        }

        public DataTable FindServicesForList(string search)
        {
            var value=(search??string.Empty).Replace("'","''");return Query("SELECT id AS 'Código', code AS 'Código interno', name AS 'Serviço', category AS 'Categoria', estimated_duration AS 'Duração estimada', printf('R$ %.2f', sale_price) AS 'Preço padrão', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM services WHERE COALESCE(code,'') LIKE '%"+value+"%' OR name LIKE '%"+value+"%' OR COALESCE(category,'') LIKE '%"+value+"%' ORDER BY name;");
        }

        public DataTable GetProductLookup() { return Query("SELECT id, (COALESCE(code || ' — ', '') || name || ' — R$ ' || printf('%.2f', sale_price) || ' | Estoque: ' || printf('%.2f', stock_quantity) || CASE WHEN COALESCE(shelf_number,'')='' THEN '' ELSE ' | Prat.: ' || shelf_number || CASE WHEN COALESCE(shelf_location,'')='' THEN '' ELSE ' / ' || shelf_location END END) AS display_name, name, sale_price FROM products WHERE active=1 ORDER BY name;"); }
        public DataTable FindProductLookup(string search) { var value=(search??string.Empty).Replace("'", "''"); return Query("SELECT id, (COALESCE(code || ' — ', '') || name || ' — R$ ' || printf('%.2f', sale_price) || ' | Estoque: ' || printf('%.2f', stock_quantity) || CASE WHEN COALESCE(shelf_number,'')='' THEN '' ELSE ' | Prat.: ' || shelf_number || CASE WHEN COALESCE(shelf_location,'')='' THEN '' ELSE ' / ' || shelf_location END END) AS display_name, name, sale_price FROM products WHERE active=1 AND (name LIKE '%"+value+"%' OR COALESCE(code,'') LIKE '%"+value+"%' OR COALESCE(barcode,'') LIKE '%"+value+"%' OR COALESCE(shelf_number,'') LIKE '%"+value+"%' OR COALESCE(shelf_location,'') LIKE '%"+value+"%') ORDER BY name;"); }
        public DataTable FindProducts(string search)
        {
            var value = (search ?? string.Empty).Replace("'", "''");
            return Query("SELECT id, code, barcode, name, sale_price, stock_quantity FROM products WHERE active=1 AND (barcode='" + value + "' OR code='" + value + "' OR name LIKE '%" + value + "%') ORDER BY CASE WHEN barcode='" + value + "' OR code='" + value + "' THEN 0 ELSE 1 END, name;");
        }
        public DataTable GetProductCategories() { return Query("SELECT id AS 'Código', name AS 'Categoria', CASE WHEN active=1 THEN 'Ativa' ELSE 'Inativa' END AS 'Status' FROM product_categories ORDER BY name;"); }
        public DataTable GetProductCategoryLookup() { return Query("SELECT id, name AS display_name, name FROM product_categories WHERE active=1 ORDER BY name;"); }
        public void SaveProductCategory(long? id,string name,bool active) { RequirePermission("catalog.edit");  using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var command=new SQLiteCommand("INSERT INTO product_categories (id,name,active) VALUES (@id,@name,@active) ON CONFLICT(id) DO UPDATE SET name=@name,active=@active;",connection)) { connection.Open();command.Parameters.AddWithValue("@id",id.HasValue?(object)id.Value:DBNull.Value);command.Parameters.AddWithValue("@name",name);command.Parameters.AddWithValue("@active",active?1:0);command.ExecuteNonQuery(); } AddAudit("product_category_saved",name); }
        public DataTable GetServiceCategories(){return Query("SELECT name FROM service_categories ORDER BY name COLLATE NOCASE");}
        public void SaveServiceCategory(string name)
        {
            RequirePermission("service.manage");if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Informe o nome da categoria.");
            using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;"))using(var q=new SQLiteCommand("INSERT OR IGNORE INTO service_categories(name) VALUES(@name)",c)){c.Open();q.Parameters.AddWithValue("@name",name.Trim());q.ExecuteNonQuery();}
        }
        public DataTable GetReportTopServices(DateTime start,DateTime end)
        {
            RequirePermission("reports.view");
            using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;"))using(var q=new SQLiteCommand(@"SELECT i.description AS 'Serviço',SUM(i.quantity) AS 'Quantidade',COUNT(DISTINCT w.id) AS 'Ordens',ROUND(SUM(i.quantity*i.unit_price),2) AS 'Valor' FROM work_order_items i JOIN work_orders w ON w.id=i.work_order_id WHERE w.status='Finalizada' AND i.item_type='Serviço' AND datetime(w.finalized_at)>=@start AND datetime(w.finalized_at)<@end GROUP BY i.description ORDER BY SUM(i.quantity) DESC,i.description",c))
            {c.Open();q.Parameters.AddWithValue("@start",start.Date.ToString("yyyy-MM-dd HH:mm:ss"));q.Parameters.AddWithValue("@end",end.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));var table=new DataTable();using(var adapter=new SQLiteDataAdapter(q))adapter.Fill(table);return table;}
        }
        public DataTable GetServiceLookup() { return Query("SELECT id, (COALESCE(code || ' — ', '') || name || ' — R$ ' || printf('%.2f', sale_price)) AS display_name, name, sale_price FROM services WHERE active=1 ORDER BY name;"); }
        public DataTable FindServiceLookup(string search) { var value=(search??string.Empty).Replace("'", "''"); return Query("SELECT id, (COALESCE(code || ' — ', '') || name || ' — R$ ' || printf('%.2f', sale_price)) AS display_name, name, sale_price FROM services WHERE active=1 AND (name LIKE '%"+value+"%' OR COALESCE(code,'') LIKE '%"+value+"%') ORDER BY name;"); }

        public void SaveProduct(long? id, string code, string barcode, string name, string category, string unit, string shelfNumber, string shelfLocation, decimal salePrice, decimal minimumStock, bool active, decimal[] pricing = null, decimal initialStock = 0)
        { RequirePermission(id.HasValue ? "catalog.edit" : "catalog.create"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO products (id, code, barcode, name, category, unit, shelf_number, shelf_location, sale_price, minimum_stock, active) VALUES (@id,@code,@barcode,@name,@category,@unit,@shelf_number,@shelf_location,@sale_price,@minimum_stock,@active) ON CONFLICT(id) DO UPDATE SET code=@code,barcode=@barcode,name=@name,category=@category,unit=@unit,shelf_number=@shelf_number,shelf_location=@shelf_location,sale_price=@sale_price,minimum_stock=@minimum_stock,active=@active;", connection))
            { connection.Open(); using(var tx=connection.BeginTransaction()) { command.Transaction=tx; command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@code", code); command.Parameters.AddWithValue("@barcode", barcode); command.Parameters.AddWithValue("@name", name); command.Parameters.AddWithValue("@category", category); command.Parameters.AddWithValue("@unit", unit); command.Parameters.AddWithValue("@shelf_number", shelfNumber); command.Parameters.AddWithValue("@shelf_location", shelfLocation); command.Parameters.AddWithValue("@sale_price", salePrice); command.Parameters.AddWithValue("@minimum_stock", minimumStock); command.Parameters.AddWithValue("@active", active ? 1 : 0); command.ExecuteNonQuery();
                if(!id.HasValue && initialStock>0) { var productId=connection.LastInsertRowId; using(var stock=new SQLiteCommand("UPDATE products SET stock_quantity=@q WHERE id=@id",connection,tx)){stock.Parameters.AddWithValue("@q",initialStock);stock.Parameters.AddWithValue("@id",productId);stock.ExecuteNonQuery();} using(var movement=new SQLiteCommand("INSERT INTO stock_movements(product_id,movement_type,quantity,previous_quantity,resulting_quantity,reason,created_at) VALUES(@id,'Saldo inicial',@q,0,@q,'Quantidade informada no cadastro do produto',@date)",connection,tx)){movement.Parameters.AddWithValue("@id",productId);movement.Parameters.AddWithValue("@q",initialStock);movement.Parameters.AddWithValue("@date",DateTime.UtcNow.ToString("o"));movement.ExecuteNonQuery();} }
                if(pricing!=null) {
                    if(pricing.Length!=9)throw new ArgumentException("Composition invalide.");
                    var productId=id ?? connection.LastInsertRowId;
                    for(int i=0;i<9;i++) using(var q=new SQLiteCommand("INSERT OR REPLACE INTO settings(key,value) VALUES(@k,@v)",connection,tx)) {
                        if(pricing[i]<0)throw new ArgumentException("Valores negativos não são permitidos.");
                        q.Parameters.AddWithValue("@k","pricing."+productId+"."+i);q.Parameters.AddWithValue("@v",pricing[i].ToString(System.Globalization.CultureInfo.InvariantCulture));q.ExecuteNonQuery();
                    }
                }
                tx.Commit(); } }
            AddAudit("product_saved", name);
        }

        public long SaveService(long? id, string code, string name, string category, string duration, decimal salePrice, bool active, decimal[] pricing = null)
        { RequirePermission("service.manage"); 
            long savedId;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO services (id, code, name, category, estimated_duration, sale_price, active) VALUES (@id,@code,@name,@category,@duration,@sale_price,@active) ON CONFLICT(id) DO UPDATE SET code=@code,name=@name,category=@category,estimated_duration=@duration,sale_price=@sale_price,active=@active;", connection))
            { connection.Open(); using(var tx=connection.BeginTransaction()) { command.Transaction=tx; command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@code", code); command.Parameters.AddWithValue("@name", name); command.Parameters.AddWithValue("@category", category); command.Parameters.AddWithValue("@duration", duration); command.Parameters.AddWithValue("@sale_price", salePrice); command.Parameters.AddWithValue("@active", active ? 1 : 0); command.ExecuteNonQuery(); savedId=id ?? connection.LastInsertRowId;
                if(pricing!=null) {
                    if(pricing.Length!=9)throw new ArgumentException("Composição inválida.");
                    var serviceId=savedId;
                    for(int i=0;i<9;i++)using(var q=new SQLiteCommand("INSERT OR REPLACE INTO settings(key,value) VALUES(@k,@v)",connection,tx)) {
                        if(pricing[i]<0)throw new ArgumentException("Valores negativos não são permitidos.");
                        q.Parameters.AddWithValue("@k","service.pricing."+serviceId+"."+i);q.Parameters.AddWithValue("@v",pricing[i].ToString(System.Globalization.CultureInfo.InvariantCulture));q.ExecuteNonQuery();
                    }
                }
                tx.Commit(); } }
            AddAudit("service_saved", name); return savedId;
        }

        public void DeleteProduct(long id) { RequirePermission("catalog.delete");  Execute("DELETE FROM products WHERE id=@id;", id); AddAudit("product_deleted", id.ToString()); }
        public void DeleteService(long id) { RequirePermission("service.manage");  Execute("DELETE FROM services WHERE id=@id;", id); AddAudit("service_deleted", id.ToString()); }

        public decimal GetWorkOrderSubtotal(long workOrderId)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(SUM(quantity * unit_price), 0) FROM work_order_items WHERE work_order_id=@id", connection))
            { connection.Open(); command.Parameters.AddWithValue("@id", workOrderId); return Convert.ToDecimal(command.ExecuteScalar()); }
        }

        public void SaveWorkOrderItem(long workOrderId, long? productId, string itemType, string description, decimal quantity, decimal unitPrice)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO work_order_items (work_order_id, product_id, item_type, description, quantity, unit_price, created_at) VALUES (@work_order_id, @product_id, @item_type, @description, @quantity, @unit_price, @created_at)", connection))
            { connection.Open(); command.Parameters.AddWithValue("@work_order_id", workOrderId); command.Parameters.AddWithValue("@product_id", productId.HasValue ? (object)productId.Value : DBNull.Value); command.Parameters.AddWithValue("@item_type", itemType); command.Parameters.AddWithValue("@description", description); command.Parameters.AddWithValue("@quantity", quantity); command.Parameters.AddWithValue("@unit_price", unitPrice); command.Parameters.AddWithValue("@created_at", DateTime.UtcNow.ToString("o")); command.ExecuteNonQuery(); }
            var order = Query("SELECT status FROM work_orders WHERE id=" + workOrderId);
            var orderStatus = order.Rows.Count == 0 ? string.Empty : Convert.ToString(order.Rows[0]["status"]);
            SynchronizeWorkOrderStock(workOrderId,orderStatus);
            AddAudit("work_order_item_saved", description);
        }

        public void DeleteWorkOrderItem(long id)
        {
            RequirePermission("service.manage");
            ReverseWorkOrderItemStock(id, "Estorno de item da OS");
            Execute("DELETE FROM work_order_items WHERE id=@id;", id);
            AddAudit("work_order_item_deleted", id.ToString());
        }

        public DataTable GetStockMovements()
        {
            return Query("SELECT products.name AS 'Produto', stock_movements.movement_type AS 'Tipo', CASE WHEN stock_movements.quantity=CAST(stock_movements.quantity AS INTEGER) THEN CAST(stock_movements.quantity AS INTEGER) ELSE printf('%.2f', stock_movements.quantity) END AS 'Quantidade', CASE WHEN stock_movements.previous_quantity=CAST(stock_movements.previous_quantity AS INTEGER) THEN CAST(stock_movements.previous_quantity AS INTEGER) ELSE printf('%.2f', stock_movements.previous_quantity) END AS 'Saldo anterior', CASE WHEN stock_movements.resulting_quantity=CAST(stock_movements.resulting_quantity AS INTEGER) THEN CAST(stock_movements.resulting_quantity AS INTEGER) ELSE printf('%.2f', stock_movements.resulting_quantity) END AS 'Saldo final', COALESCE(work_orders.number, '') AS 'OS', stock_movements.reason AS 'Motivo', strftime('%d/%m/%Y %H:%M',replace(substr(stock_movements.created_at,1,19),'T',' ')) AS 'Data' FROM stock_movements INNER JOIN products ON products.id=stock_movements.product_id LEFT JOIN work_orders ON work_orders.id=stock_movements.work_order_id ORDER BY stock_movements.id DESC;");
        }

        public DataTable GetStockProductLookup()
        {
            return Query("SELECT id, (CASE WHEN COALESCE(code,'')='' THEN '' ELSE code || ' - ' END || name) AS display_name FROM products WHERE active=1 ORDER BY name;");
        }

        public DataTable FindStockProductLookup(string search)
        {
            var value=(search??string.Empty).Replace("'","''");
            return Query("SELECT id, (CASE WHEN COALESCE(code,'')='' THEN '' ELSE code || ' - ' END || name) AS display_name FROM products WHERE active=1 AND (name LIKE '%"+value+"%' OR COALESCE(code,'') LIKE '%"+value+"%' OR COALESCE(barcode,'') LIKE '%"+value+"%') ORDER BY name;");
        }

        public int GetLowStockCount() { using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT COUNT(1) FROM products WHERE active=1 AND stock_quantity <= minimum_stock", connection)) { connection.Open(); return Convert.ToInt32(command.ExecuteScalar()); } }

        public string NextSaleNumber()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT COALESCE(MAX(id),0)+1 FROM sales", connection)) { connection.Open(); return "VD-" + DateTime.Now.Year + "-" + Convert.ToInt64(command.ExecuteScalar()).ToString("000000"); }
        }

        public DataTable GetOpenCash() { return Query("SELECT * FROM cash_sessions WHERE status='Aberto' ORDER BY id DESC LIMIT 1;"); }
        public DataTable GetCashMovements(long cashId) { return Query("SELECT id AS 'Código', movement_type AS 'Tipo', COALESCE(payment_method,'') AS 'Pagamento', printf('R$ %.2f', amount) AS 'Valor', description AS 'Descrição', substr(created_at,1,19) AS 'Data' FROM cash_movements WHERE cash_session_id=" + cashId + " ORDER BY id DESC;"); }
        public long OpenCash(decimal openingBalance)
        { RequirePermission("cash.open"); 
            long id;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO cash_sessions (opened_at,opening_balance,status) VALUES (@opened,@balance,'Aberto'); SELECT last_insert_rowid();",connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@opened",DateTime.UtcNow.ToString("o")); command.Parameters.AddWithValue("@balance",openingBalance);
                id=Convert.ToInt64(command.ExecuteScalar());
            }
            AddAudit("cash_opened",id.ToString());
            return id;
        }
        public void AddCashMovement(long cashId, string type, string payment, decimal amount, string description) { using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("INSERT INTO cash_movements (cash_session_id,movement_type,payment_method,amount,description,created_at) VALUES (@cash,@type,@payment,@amount,@description,@created)",connection)) { connection.Open(); command.Parameters.AddWithValue("@cash",cashId);command.Parameters.AddWithValue("@type",type);command.Parameters.AddWithValue("@payment",payment);command.Parameters.AddWithValue("@amount",amount);command.Parameters.AddWithValue("@description",description);command.Parameters.AddWithValue("@created",DateTime.UtcNow.ToString("o"));command.ExecuteNonQuery(); } AddAudit("cash_movement",type); }
        public decimal GetCashExpected(long cashId) { using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT opening_balance + COALESCE((SELECT SUM(amount) FROM cash_movements WHERE cash_session_id=@id AND payment_method='Dinheiro'),0) FROM cash_sessions WHERE id=@id",connection)) { connection.Open(); command.Parameters.AddWithValue("@id",cashId); return Convert.ToDecimal(command.ExecuteScalar()); } }
        public void CloseCash(long cashId, decimal counted) { RequirePermission("cash.close");  var expected=GetCashExpected(cashId); using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE cash_sessions SET status='Fechado',closed_at=@closed,counted_balance=@counted,difference=@difference WHERE id=@id",connection)) { connection.Open();command.Parameters.AddWithValue("@closed",DateTime.UtcNow.ToString("o"));command.Parameters.AddWithValue("@counted",counted);command.Parameters.AddWithValue("@difference",counted-expected);command.Parameters.AddWithValue("@id",cashId);command.ExecuteNonQuery(); } AddAudit("cash_closed",cashId.ToString()); }

        public DataTable GetFinancialEntries()
        {
            return FindFinancialEntries("Todos", string.Empty);
        }

        public DataTable FindFinancialEntries(string statusFilter, string search)
        {
            var value = (search ?? string.Empty).Replace("'", "''");
            var clause = string.Empty;
            if (statusFilter == "Vencido") clause = " AND status='Pendente' AND date(due_date) < date('now','localtime')";
            else if (statusFilter == "Pendente" || statusFilter == "Pago" || statusFilter == "Cancelado") clause = " AND status='" + statusFilter + "'";
            return Query("SELECT id AS 'Código', entry_type AS 'Tipo', COALESCE(party_name,'') AS 'Cliente / fornecedor', COALESCE(due_date,'') AS 'Vencimento', CASE WHEN status='Pendente' AND date(due_date) < date('now','localtime') THEN 'Vencido' ELSE status END AS 'Status', printf('R$ %.2f', amount) AS 'Valor', COALESCE(category,'') AS 'Categoria', description AS 'Descrição', COALESCE(document_number,'') AS 'Documento', COALESCE(payment_method,'') AS 'Pagamento', COALESCE(origin,'Manual') AS 'Origem' FROM financial_entries WHERE (COALESCE(party_name,'') LIKE '%" + value + "%' OR COALESCE(description,'') LIKE '%" + value + "%' OR COALESCE(document_number,'') LIKE '%" + value + "%' OR COALESCE(category,'') LIKE '%" + value + "%')" + clause + " ORDER BY CASE WHEN status='Pendente' THEN 0 ELSE 1 END, due_date, id DESC;");
        }

        public DataTable FindFinancialEntriesByType(string type, string statusFilter, string search, DateTime? start, DateTime? end)
        {
            var value = (search ?? string.Empty).Replace("'", "''");
            var clause = string.Empty;
            if (statusFilter == "Vencido") clause += " AND status='Pendente' AND date(due_date) < date('now','localtime')";
            else if (statusFilter == "Pendente" || statusFilter == "Pago" || statusFilter == "Cancelado") clause += " AND status='" + statusFilter + "'";
            if (start.HasValue) clause += " AND date(due_date)>='" + start.Value.ToString("yyyy-MM-dd") + "'";
            if (end.HasValue) clause += " AND date(due_date)<='" + end.Value.ToString("yyyy-MM-dd") + "'";
            return Query("SELECT id AS 'Código', entry_type AS 'Tipo', COALESCE(party_name,'') AS 'Cliente / fornecedor', COALESCE(due_date,'') AS 'Vencimento', CASE WHEN status='Pendente' AND date(due_date) < date('now','localtime') THEN 'Vencido' ELSE status END AS 'Status', printf('R$ %.2f', amount) AS 'Valor', COALESCE(category,'') AS 'Categoria', description AS 'Descrição', COALESCE(document_number,'') AS 'Documento', COALESCE(payment_method,'') AS 'Pagamento', COALESCE(origin,'Manual') AS 'Origem' FROM financial_entries WHERE entry_type='" + type + "' AND (COALESCE(party_name,'') LIKE '%" + value + "%' OR COALESCE(description,'') LIKE '%" + value + "%' OR COALESCE(document_number,'') LIKE '%" + value + "%' OR COALESCE(category,'') LIKE '%" + value + "%')" + clause + " ORDER BY CASE WHEN status='Pendente' THEN 0 ELSE 1 END, due_date, id DESC;");
        }

        public DataTable GetFinancialAccountSummary(string type, DateTime? start, DateTime? end)
        {
            var period = string.Empty; if (start.HasValue) period += " AND date(due_date)>='" + start.Value.ToString("yyyy-MM-dd") + "'"; if (end.HasValue) period += " AND date(due_date)<='" + end.Value.ToString("yyyy-MM-dd") + "'";
            return Query("SELECT COALESCE(SUM(CASE WHEN status='Pendente' THEN amount ELSE 0 END),0) AS pending, COALESCE(SUM(CASE WHEN status='Pendente' AND date(due_date)<date('now','localtime') THEN amount ELSE 0 END),0) AS overdue, COALESCE(SUM(CASE WHEN status='Pago' THEN amount ELSE 0 END),0) AS paid, COALESCE(SUM(CASE WHEN status='Cancelado' THEN amount ELSE 0 END),0) AS cancelled FROM financial_entries WHERE entry_type='" + type + "'" + period + ";");
        }

        public DataTable GetFinancialAlerts(int days)
        {
            if (days < 1) days = 1;
            return Query("SELECT id AS 'Código', entry_type AS 'Tipo', COALESCE(party_name,'') AS 'Cliente / fornecedor', COALESCE(due_date,'') AS 'Vencimento', CASE WHEN date(due_date)<date('now','localtime') THEN 'Vencida' WHEN date(due_date)=date('now','localtime') THEN 'Vence hoje' ELSE 'Vence em ' || CAST(julianday(date(due_date))-julianday(date('now','localtime')) AS INTEGER) || ' dia(s)' END AS 'Alerta', printf('R$ %.2f',amount) AS 'Valor', description AS 'Descrição', COALESCE(document_number,'') AS 'Documento', COALESCE(payment_method,'') AS 'Pagamento' FROM financial_entries WHERE status='Pendente' AND due_date IS NOT NULL AND date(due_date)<=date('now','localtime','+" + days + " days') ORDER BY CASE WHEN date(due_date)<date('now','localtime') THEN 0 WHEN date(due_date)=date('now','localtime') THEN 1 ELSE 2 END, due_date, entry_type;");
        }

        public DataTable GetFinancialFlow()
        {
            return Query("SELECT substr(COALESCE(paid_at,created_at),1,10) AS 'Data', entry_type AS 'Tipo', description AS 'Descrição', COALESCE(party_name,'') AS 'Cliente / fornecedor', printf('R$ %.2f', amount) AS 'Valor', CASE WHEN entry_type='Receber' THEN printf('R$ %.2f', amount) ELSE printf('-R$ %.2f', amount) END AS 'Impacto' FROM financial_entries WHERE status='Pago' ORDER BY COALESCE(paid_at,created_at) DESC, id DESC;");
        }

        public DataTable GetReportSummary(DateTime start, DateTime end)
        {
            var from = start.ToString("yyyy-MM-dd"); var to = end.ToString("yyyy-MM-dd");
            return Query(@"SELECT
COALESCE((SELECT SUM(amount) FROM financial_entries WHERE entry_type='Receber' AND status='Pago' AND substr(paid_at,1,10)>='" + from + @"' AND substr(paid_at,1,10)<='" + to + @"'),0) AS received,
COALESCE((SELECT SUM(amount) FROM financial_entries WHERE entry_type='Pagar' AND status='Pago' AND substr(paid_at,1,10)>='" + from + @"' AND substr(paid_at,1,10)<='" + to + @"'),0) AS paid,
COALESCE((SELECT SUM(amount) FROM financial_entries WHERE entry_type='Receber' AND status='Pendente'),0) AS pending_receive,
COALESCE((SELECT SUM(amount) FROM financial_entries WHERE entry_type='Pagar' AND status='Pendente'),0) AS pending_pay,
COALESCE((SELECT COUNT(1) FROM sales WHERE status='Finalizada' AND substr(created_at,1,10)>='" + from + @"' AND substr(created_at,1,10)<='" + to + @"'),0) AS sales_count,
COALESCE((SELECT COUNT(1) FROM work_orders WHERE substr(opened_at,1,10)>='" + from + @"' AND substr(opened_at,1,10)<='" + to + @"'),0) AS orders_count;");
        }

        public DataTable GetReportSalesByPayment(DateTime start, DateTime end)
        {
            var from = start.ToString("yyyy-MM-dd"); var to = end.ToString("yyyy-MM-dd");
            return Query("SELECT COALESCE(payment_method,'Não informado') AS 'Pagamento', COUNT(1) AS 'Vendas', printf('R$ %.2f', SUM(total)) AS 'Total' FROM sales WHERE status='Finalizada' AND substr(created_at,1,10)>='" + from + "' AND substr(created_at,1,10)<='" + to + "' GROUP BY payment_method ORDER BY SUM(total) DESC;");
        }

        public decimal[] GetServicePricing(long id)
        {
            var values=new decimal[9];for(int i=0;i<9;i++)decimal.TryParse(Get("service.pricing."+id+"."+i),System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out values[i]);return values;
        }
        public decimal[] GetPricing(long productId)
        {
            var result=new decimal[9];
            for(int i=0;i<result.Length;i++) decimal.TryParse(Get("pricing."+productId+"."+i),System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.InvariantCulture,out result[i]);
            return result;
        }
        public void SavePricing(long productId,decimal[] values,decimal price)
        {
            RequirePermission("catalog.edit");
            if(values==null||values.Length!=9||price<0)throw new ArgumentException("Valores inválidos.");
            using(var c=new SQLiteConnection("Data Source="+_databasePath+";Version=3;"))
            {
                c.Open();using(var tx=c.BeginTransaction())
                {
                    for(int i=0;i<values.Length;i++)
                    using(var q=new SQLiteCommand("INSERT OR REPLACE INTO settings(key,value) VALUES(@key,@value)",c,tx))
                    {
                        if(values[i]<0)throw new ArgumentException("Valores negativos não são permitidos.");
                        q.Parameters.AddWithValue("@key","pricing."+productId+"."+i);q.Parameters.AddWithValue("@value",values[i].ToString(System.Globalization.CultureInfo.InvariantCulture));q.ExecuteNonQuery();
                    }
                    using(var q=new SQLiteCommand("UPDATE products SET sale_price=@price WHERE id=@id",c,tx))
                    {q.Parameters.AddWithValue("@price",price);q.Parameters.AddWithValue("@id",productId);if(q.ExecuteNonQuery()!=1)throw new InvalidOperationException("Produto não encontrado.");}
                    tx.Commit();
                }
            }
            AddAudit("product_pricing_saved",productId.ToString());
        }

        public DataTable GetReportTopProducts(DateTime start, DateTime end)
        {
            RequirePermission("reports.view");
            if(end.Date < start.Date) throw new ArgumentException("Período inválido.");
            using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;"))
            using(var command=new SQLiteCommand(@"SELECT i.product_id AS 'Código',
COALESCE(p.name,MAX(i.description)) AS 'Produto',
SUM(i.quantity) AS 'Quantidade',
COUNT(DISTINCT s.id) AS 'Vendas',
ROUND(SUM(i.total),2) AS 'Valor bruto',
ROUND(SUM(CASE WHEN s.subtotal>0 THEN i.total*s.discount/s.subtotal ELSE 0 END),2) AS 'Descontos',
ROUND(SUM(CASE WHEN s.subtotal>0 THEN i.total*(1-s.discount/s.subtotal) ELSE i.total END),2) AS 'Valor líquido'
FROM sale_items i JOIN sales s ON s.id=i.sale_id LEFT JOIN products p ON p.id=i.product_id
WHERE s.status='Finalizada' AND datetime(s.created_at,'localtime')>=@start AND datetime(s.created_at,'localtime')<@end
GROUP BY i.product_id ORDER BY SUM(i.quantity) DESC,SUM(i.total) DESC,i.product_id",connection))
            {
                command.Parameters.AddWithValue("@start",start.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("@end",end.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));
                connection.Open();var table=new DataTable();using(var adapter=new SQLiteDataAdapter(command))adapter.Fill(table);return table;
            }
        }

        public DataTable GetReportLowStock()
        {
            return Query("SELECT name AS 'Produto / peça', COALESCE(category,'') AS 'Categoria', CASE WHEN stock_quantity=CAST(stock_quantity AS INTEGER) THEN CAST(stock_quantity AS INTEGER) ELSE printf('%.2f',stock_quantity) END AS 'Em estoque', CASE WHEN minimum_stock=CAST(minimum_stock AS INTEGER) THEN CAST(minimum_stock AS INTEGER) ELSE printf('%.2f',minimum_stock) END AS 'Mínimo' FROM products WHERE active=1 AND stock_quantity<=minimum_stock ORDER BY name;");
        }

        public DataTable GetFinancialDailyMovements(DateTime day)
        {
            var date = day.ToString("yyyy-MM-dd");
            return Query(@"SELECT substr(paid_at,1,10) AS 'Data', entry_type AS 'Tipo', description AS 'Descrição', COALESCE(party_name,'') AS 'Cliente / fornecedor', COALESCE(payment_method,'') AS 'Pagamento', amount AS raw_impact, printf('R$ %.2f', amount) AS 'Valor', CASE WHEN entry_type='Receber' THEN printf('R$ %.2f', amount) ELSE printf('-R$ %.2f', amount) END AS 'Impacto', 'Financeiro' AS 'Origem'
FROM financial_entries WHERE status='Pago' AND substr(paid_at,1,10)='" + date + @"'
UNION ALL
SELECT substr(created_at,1,10) AS 'Data', movement_type AS 'Tipo', COALESCE(description,'') AS 'Descrição', '' AS 'Cliente / fornecedor', 'Dinheiro' AS 'Pagamento', amount AS raw_impact, printf('R$ %.2f', abs(amount)) AS 'Valor', CASE WHEN amount >= 0 THEN printf('R$ %.2f', amount) ELSE printf('-R$ %.2f', abs(amount)) END AS 'Impacto', 'Caixa' AS 'Origem'
FROM cash_movements WHERE substr(created_at,1,10)='" + date + @"' AND movement_type IN ('Suprimento','Sangria')
ORDER BY 9, 2, 3;");
        }

        public decimal GetFinancialPaidTotal(string type)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(SUM(amount),0) FROM financial_entries WHERE entry_type=@type AND status='Pago'", connection))
            { connection.Open(); command.Parameters.AddWithValue("@type", type); return Convert.ToDecimal(command.ExecuteScalar()); }
        }

        public decimal GetFinancialTotal(string type, string status)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(SUM(amount),0) FROM financial_entries WHERE entry_type=@type AND status=@status", connection))
            { connection.Open(); command.Parameters.AddWithValue("@type", type); command.Parameters.AddWithValue("@status", status); return Convert.ToDecimal(command.ExecuteScalar()); }
        }

        public decimal GetFinancialOverdueTotal(string type)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(SUM(amount),0) FROM financial_entries WHERE entry_type=@type AND status='Pendente' AND date(due_date) < date('now','localtime')", connection))
            { connection.Open(); command.Parameters.AddWithValue("@type", type); return Convert.ToDecimal(command.ExecuteScalar()); }
        }

        public DataTable GetFinancialCategories() { return Query("SELECT id AS 'Código', name AS 'Categoria', CASE WHEN active=1 THEN 'Ativa' ELSE 'Inativa' END AS 'Status' FROM financial_categories ORDER BY name;"); }
        public DataTable GetFinancialCategoryLookup() { return Query("SELECT id, name AS display_name, name FROM financial_categories WHERE active=1 ORDER BY name;"); }
        public void SaveFinancialCategory(long? id, string name, bool active) { RequirePermission("finance.manage");  using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var command=new SQLiteCommand("INSERT INTO financial_categories (id,name,active) VALUES (@id,@name,@active) ON CONFLICT(id) DO UPDATE SET name=@name,active=@active;",connection)) { connection.Open();command.Parameters.AddWithValue("@id",id.HasValue?(object)id.Value:DBNull.Value);command.Parameters.AddWithValue("@name",name);command.Parameters.AddWithValue("@active",active?1:0);command.ExecuteNonQuery(); } AddAudit("financial_category_saved",name); }
        public void DeleteFinancialCategory(long id) { RequirePermission("finance.manage");  Execute("DELETE FROM financial_categories WHERE id=@id;",id); AddAudit("financial_category_deleted",id.ToString()); }

        public DataTable GetSuppliers() { return FindSuppliers(string.Empty); }
        public DataTable GetDebtors() { return FindDebtors(string.Empty); }
        public DataTable FindSuppliers(string search) { var value=(search??string.Empty).Replace("'", "''"); return Query("SELECT id AS 'Código', name AS 'Fornecedor', COALESCE(contact_name,'') AS 'Contato', COALESCE(document,'') AS 'CPF/CNPJ', COALESCE(phone,'') AS 'Telefone', COALESCE(email,'') AS 'E-mail', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM suppliers WHERE name LIKE '%"+value+"%' OR COALESCE(contact_name,'') LIKE '%"+value+"%' OR COALESCE(document,'') LIKE '%"+value+"%' OR COALESCE(phone,'') LIKE '%"+value+"%' OR COALESCE(email,'') LIKE '%"+value+"%' ORDER BY name;"); }
        public DataTable FindDebtors(string search) { var value=(search??string.Empty).Replace("'", "''"); return Query("SELECT id AS 'Código', name AS 'Devedor', COALESCE(document,'') AS 'CPF/CNPJ', COALESCE(phone,'') AS 'Telefone', COALESCE(email,'') AS 'E-mail', CASE WHEN active=1 THEN 'Ativo' ELSE 'Inativo' END AS 'Status' FROM debtors WHERE name LIKE '%"+value+"%' OR COALESCE(document,'') LIKE '%"+value+"%' OR COALESCE(phone,'') LIKE '%"+value+"%' OR COALESCE(email,'') LIKE '%"+value+"%' ORDER BY name;"); }
        public DataTable GetSupplierLookup() { return Query("SELECT id, name AS display_name, name FROM suppliers WHERE active=1 ORDER BY name;"); }
        public DataTable GetDebtorLookup() { return Query("SELECT id, name AS display_name, name FROM debtors WHERE active=1 ORDER BY name;"); }
        public void SaveSupplier(long? id, string name, string document, string phone, string email, string contact, bool active) { RequirePermission("suppliers.manage");  using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var command=new SQLiteCommand("INSERT INTO suppliers (id,name,document,phone,email,contact_name,active) VALUES (@id,@name,@document,@phone,@email,@contact,@active) ON CONFLICT(id) DO UPDATE SET name=@name,document=@document,phone=@phone,email=@email,contact_name=@contact,active=@active;",connection)) { connection.Open(); command.Parameters.AddWithValue("@id",id.HasValue?(object)id.Value:DBNull.Value); command.Parameters.AddWithValue("@name",name); command.Parameters.AddWithValue("@document",document); command.Parameters.AddWithValue("@phone",phone); command.Parameters.AddWithValue("@email",email); command.Parameters.AddWithValue("@contact",contact); command.Parameters.AddWithValue("@active",active?1:0); command.ExecuteNonQuery(); } AddAudit("supplier_saved",name); }
        public void SaveDebtor(long? id, string name, string document, string phone, string email, bool active) { RequirePermission("finance.manage");  using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;")) using(var command=new SQLiteCommand("INSERT INTO debtors (id,name,document,phone,email,active) VALUES (@id,@name,@document,@phone,@email,@active) ON CONFLICT(id) DO UPDATE SET name=@name,document=@document,phone=@phone,email=@email,active=@active;",connection)) { connection.Open(); command.Parameters.AddWithValue("@id",id.HasValue?(object)id.Value:DBNull.Value); command.Parameters.AddWithValue("@name",name); command.Parameters.AddWithValue("@document",document); command.Parameters.AddWithValue("@phone",phone); command.Parameters.AddWithValue("@email",email); command.Parameters.AddWithValue("@active",active?1:0); command.ExecuteNonQuery(); } AddAudit("debtor_saved",name); }
        public void DeleteSupplier(long id) { RequirePermission("suppliers.manage");  Execute("DELETE FROM suppliers WHERE id=@id;",id); AddAudit("supplier_deleted",id.ToString()); }
        public void DeleteDebtor(long id) { RequirePermission("finance.manage");  Execute("DELETE FROM debtors WHERE id=@id;",id); AddAudit("debtor_deleted",id.ToString()); }

        public void SaveFinancialEntry(string type, long partyId, string party, string category, string description, string document, decimal amount, string dueDate, string paymentMethod)
        { RequirePermission("finance.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO financial_entries (entry_type,party_name,supplier_id,debtor_id,category,description,document_number,amount,due_date,payment_method,status,origin,created_at) VALUES (@type,@party,@supplier,@debtor,@category,@description,@document,@amount,@due,@payment,'Pendente','Manual',@created)", connection))
            { connection.Open(); command.Parameters.AddWithValue("@type", type); command.Parameters.AddWithValue("@party", party); command.Parameters.AddWithValue("@supplier", type=="Pagar" ? (object)partyId : DBNull.Value); command.Parameters.AddWithValue("@debtor", type=="Receber" ? (object)partyId : DBNull.Value); command.Parameters.AddWithValue("@category", category); command.Parameters.AddWithValue("@description", description); command.Parameters.AddWithValue("@document", document); command.Parameters.AddWithValue("@amount", amount); command.Parameters.AddWithValue("@due", dueDate); command.Parameters.AddWithValue("@payment", paymentMethod); command.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); command.ExecuteNonQuery(); }
            AddAudit("financial_entry_saved", type + ": " + description);
        }

        public void MarkFinancialEntryPaid(long id, string paymentMethod, DateTime paidAt)
        { RequirePermission("finance.manage"); 
            string type; decimal amount; string description;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            { connection.Open(); using (var read = new SQLiteCommand("SELECT entry_type,amount,description FROM financial_entries WHERE id=@id AND status='Pendente'", connection)) { read.Parameters.AddWithValue("@id", id); using (var reader = read.ExecuteReader()) { if (!reader.Read()) return; type=Convert.ToString(reader["entry_type"]); amount=Convert.ToDecimal(reader["amount"]); description=Convert.ToString(reader["description"]); } } }
            DataTable openCash = null;
            if (paymentMethod == "Dinheiro") { openCash = GetOpenCash(); if (openCash.Rows.Count == 0) throw new InvalidOperationException("Abra o caixa antes de baixar uma conta em dinheiro. Para PIX, cartão, transferência ou boleto, escolha a forma correspondente."); }
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("UPDATE financial_entries SET status='Pago', paid_at=@paid, payment_method=@payment WHERE id=@id AND status='Pendente'", connection))
            { connection.Open(); command.Parameters.AddWithValue("@paid", paidAt.ToString("o")); command.Parameters.AddWithValue("@payment", paymentMethod); command.Parameters.AddWithValue("@id", id); command.ExecuteNonQuery(); }
            if (paymentMethod == "Dinheiro") AddCashMovement(Convert.ToInt64(openCash.Rows[0]["id"]), type == "Receber" ? "Recebimento financeiro" : "Pagamento financeiro", "Dinheiro", type == "Receber" ? amount : -amount, description);
            AddAudit("financial_entry_paid", id.ToString());
        }

        public void CancelFinancialEntry(long id)
        { RequirePermission("finance.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("UPDATE financial_entries SET status='Cancelado' WHERE id=@id AND status='Pendente'", connection))
            { connection.Open(); command.Parameters.AddWithValue("@id", id); command.ExecuteNonQuery(); }
            AddAudit("financial_entry_cancelled", id.ToString());
        }

        public void SaveSale(string number, DataTable items, string paymentMethod, decimal discount, long? clientId)
        { RequirePermission("sales.create"); 
            var open = GetOpenCash(); if (open.Rows.Count == 0) throw new InvalidOperationException("Abra o caixa antes de finalizar uma venda."); var cashId=Convert.ToInt64(open.Rows[0]["id"]);
            decimal subtotal = 0; foreach (DataRow item in items.Rows) subtotal += Convert.ToDecimal(item["quantity"]) * Convert.ToDecimal(item["unit_price"]); var total = Math.Max(0, subtotal - discount); long saleId;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            { connection.Open(); using (var transaction = connection.BeginTransaction()) { using (var sale = new SQLiteCommand("INSERT INTO sales (number,client_id,status,subtotal,discount,total,payment_method,cash_session_id,created_at) VALUES (@number,@client,'Finalizada',@subtotal,@discount,@total,@payment,@cash,@created); SELECT last_insert_rowid();", connection, transaction)) { sale.Parameters.AddWithValue("@number", number); sale.Parameters.AddWithValue("@client",clientId.HasValue?(object)clientId.Value:DBNull.Value); sale.Parameters.AddWithValue("@subtotal", subtotal); sale.Parameters.AddWithValue("@discount", discount); sale.Parameters.AddWithValue("@total", total); sale.Parameters.AddWithValue("@payment", paymentMethod); sale.Parameters.AddWithValue("@cash",cashId); sale.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); saleId = Convert.ToInt64(sale.ExecuteScalar()); }
                    foreach (DataRow item in items.Rows) using (var detail = new SQLiteCommand("INSERT INTO sale_items (sale_id,product_id,description,quantity,unit_price,total) VALUES (@sale,@product,@description,@quantity,@unit,@total)", connection, transaction)) { var quantity = Convert.ToDecimal(item["quantity"]); var price = Convert.ToDecimal(item["unit_price"]); detail.Parameters.AddWithValue("@sale",saleId); detail.Parameters.AddWithValue("@product",Convert.ToInt64(item["product_id"])); detail.Parameters.AddWithValue("@description",Convert.ToString(item["description"])); detail.Parameters.AddWithValue("@quantity",quantity); detail.Parameters.AddWithValue("@unit",price); detail.Parameters.AddWithValue("@total",quantity*price); detail.ExecuteNonQuery(); } transaction.Commit(); }
            }
            foreach (DataRow item in items.Rows) MoveStock(Convert.ToInt64(item["product_id"]), -Convert.ToDecimal(item["quantity"]), "Venda PDV", "Venda " + number, null, null);
            AddCashMovement(cashId,"Venda PDV",paymentMethod,total,"Venda " + number);
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var financial = new SQLiteCommand("INSERT INTO financial_entries (entry_type,description,document_number,amount,due_date,paid_at,payment_method,status,origin,sale_id,created_at) VALUES ('Receber',@description,@document,@amount,@due,@paid,@payment,'Pago','PDV',@sale,@created)", connection)) { connection.Open(); financial.Parameters.AddWithValue("@description", "Venda " + number); financial.Parameters.AddWithValue("@document", number); financial.Parameters.AddWithValue("@amount", total); financial.Parameters.AddWithValue("@due", DateTime.Now.ToString("yyyy-MM-dd")); financial.Parameters.AddWithValue("@paid", DateTime.UtcNow.ToString("o")); financial.Parameters.AddWithValue("@payment", paymentMethod); financial.Parameters.AddWithValue("@sale", saleId); financial.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); financial.ExecuteNonQuery(); }
            AddAudit("sale_saved", number);
        }

        public DataTable GetPurchases()
        {
            return Query("SELECT id AS 'Código', number AS 'Compra', supplier_name AS 'Fornecedor', COALESCE(invoice_number,'') AS 'NF / documento', substr(purchased_at,1,10) AS 'Data', printf('R$ %.2f',total) AS 'Total', CASE WHEN payable_generated=1 THEN 'Sim' ELSE 'Não' END AS 'Conta a pagar', COALESCE(status,'Finalizada') AS 'Status' FROM purchases ORDER BY id DESC;");
        }

        public string NextPurchaseNumber()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("SELECT COALESCE(MAX(id),0)+1 FROM purchases", connection)) { connection.Open(); return "CP-" + DateTime.Now.Year + "-" + Convert.ToInt64(command.ExecuteScalar()).ToString("000000"); }
        }

        public void SavePurchase(string number, long supplierId, string supplierName, string invoiceNumber, DateTime purchasedAt, string paymentMethod, bool generatePayable, DataTable items, DataTable installments)
        { RequirePermission("purchases.manage"); 
            if (items.Rows.Count == 0) throw new InvalidOperationException("Inclua ao menos uma peça na compra."); decimal total = 0; foreach (DataRow item in items.Rows) total += Math.Round(Convert.ToDecimal(item["quantity"]) * Convert.ToDecimal(item["unit_cost"]), 2, MidpointRounding.AwayFromZero); total = Math.Round(total, 2, MidpointRounding.AwayFromZero); long purchaseId;
            if (generatePayable && (installments == null || installments.Rows.Count == 0)) throw new InvalidOperationException("Gere ao menos uma parcela para a conta a pagar.");
            decimal installmentsTotal = 0; DateTime firstDue = purchasedAt.Date; var firstDueAssigned = false;
            if (generatePayable) { foreach (DataRow installment in installments.Rows) { var due = Convert.ToDateTime(installment["due_date"]).Date; if (due < purchasedAt.Date) throw new InvalidOperationException("O vencimento não pode ser anterior à data da compra."); installmentsTotal += Convert.ToDecimal(installment["amount"]); if (!firstDueAssigned || due < firstDue) { firstDue = due; firstDueAssigned = true; } } if (Math.Abs(installmentsTotal - total) > 0.01M) throw new InvalidOperationException("A soma das parcelas deve ser igual ao total da compra."); }
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            { connection.Open(); using (var transaction = connection.BeginTransaction())
                {
                    using (var purchase = new SQLiteCommand("INSERT INTO purchases (number,supplier_id,supplier_name,invoice_number,purchased_at,due_date,payment_method,total,payable_generated,created_at) VALUES (@number,@supplier,@name,@invoice,@date,@due,@payment,@total,@payable,@created); SELECT last_insert_rowid();", connection, transaction))
                    { purchase.Parameters.AddWithValue("@number", number); purchase.Parameters.AddWithValue("@supplier", supplierId); purchase.Parameters.AddWithValue("@name", supplierName); purchase.Parameters.AddWithValue("@invoice", invoiceNumber); purchase.Parameters.AddWithValue("@date", purchasedAt.ToString("yyyy-MM-dd")); purchase.Parameters.AddWithValue("@due", firstDue.ToString("yyyy-MM-dd")); purchase.Parameters.AddWithValue("@payment", paymentMethod); purchase.Parameters.AddWithValue("@total", total); purchase.Parameters.AddWithValue("@payable", generatePayable ? 1 : 0); purchase.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); purchaseId = Convert.ToInt64(purchase.ExecuteScalar()); }
                    foreach (DataRow item in items.Rows) using (var detail = new SQLiteCommand("INSERT INTO purchase_items (purchase_id,product_id,description,quantity,unit_cost,total) VALUES (@purchase,@product,@description,@quantity,@cost,@total)", connection, transaction)) { var quantity=Convert.ToDecimal(item["quantity"]); var cost=Convert.ToDecimal(item["unit_cost"]); detail.Parameters.AddWithValue("@purchase",purchaseId);detail.Parameters.AddWithValue("@product",Convert.ToInt64(item["product_id"]));detail.Parameters.AddWithValue("@description",Convert.ToString(item["description"]));detail.Parameters.AddWithValue("@quantity",quantity);detail.Parameters.AddWithValue("@cost",cost);detail.Parameters.AddWithValue("@total",Math.Round(quantity*cost,2,MidpointRounding.AwayFromZero));detail.ExecuteNonQuery(); }
                    if (generatePayable) foreach (DataRow installment in installments.Rows) using (var payable = new SQLiteCommand("INSERT INTO financial_entries (entry_type,party_name,supplier_id,purchase_id,category,description,document_number,amount,due_date,payment_method,status,origin,created_at) VALUES ('Pagar',@party,@supplier,@purchase,'Compras',@description,@document,@amount,@due,@payment,'Pendente','Compra',@created)", connection, transaction)) { var installmentNumber=Convert.ToInt32(installment["number"]); var due=Convert.ToDateTime(installment["due_date"]); payable.Parameters.AddWithValue("@party",supplierName);payable.Parameters.AddWithValue("@supplier",supplierId);payable.Parameters.AddWithValue("@purchase",purchaseId);payable.Parameters.AddWithValue("@description","Compra "+number+" • Parcela "+installmentNumber+"/"+installments.Rows.Count);payable.Parameters.AddWithValue("@document",string.IsNullOrWhiteSpace(invoiceNumber)?number:invoiceNumber);payable.Parameters.AddWithValue("@amount",Convert.ToDecimal(installment["amount"]));payable.Parameters.AddWithValue("@due",due.ToString("yyyy-MM-dd"));payable.Parameters.AddWithValue("@payment",paymentMethod);payable.Parameters.AddWithValue("@created",DateTime.UtcNow.ToString("o"));payable.ExecuteNonQuery(); }
                    transaction.Commit();
                }
            }
            foreach (DataRow item in items.Rows) MoveStock(Convert.ToInt64(item["product_id"]), Convert.ToDecimal(item["quantity"]), "Compra", "Compra " + number + (string.IsNullOrWhiteSpace(invoiceNumber) ? string.Empty : " • NF " + invoiceNumber), null, null);
            AddAudit("purchase_saved", number);
        }

        public void CancelPurchase(long purchaseId, string reason)
        { RequirePermission("purchases.manage"); 
            string number; DataTable items;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            {
                connection.Open();
                using (var purchase = new SQLiteCommand("SELECT number,COALESCE(status,'Finalizada') AS status FROM purchases WHERE id=@id", connection))
                { purchase.Parameters.AddWithValue("@id", purchaseId); using (var reader = purchase.ExecuteReader()) { if (!reader.Read()) throw new InvalidOperationException("Compra não encontrada."); if (Convert.ToString(reader["status"]) == "Cancelada") throw new InvalidOperationException("Esta compra já foi cancelada."); number = Convert.ToString(reader["number"]); } }
                using (var transaction = connection.BeginTransaction())
                {
                    using (var cancel = new SQLiteCommand("UPDATE purchases SET status='Cancelada',cancellation_reason=@reason,cancelled_at=@cancelled WHERE id=@id", connection, transaction)) { cancel.Parameters.AddWithValue("@reason", reason); cancel.Parameters.AddWithValue("@cancelled", DateTime.UtcNow.ToString("o")); cancel.Parameters.AddWithValue("@id", purchaseId); cancel.ExecuteNonQuery(); }
                    using (var cancelPayables = new SQLiteCommand("UPDATE financial_entries SET status='Cancelado' WHERE purchase_id=@id AND status='Pendente'", connection, transaction)) { cancelPayables.Parameters.AddWithValue("@id", purchaseId); cancelPayables.ExecuteNonQuery(); }
                    transaction.Commit();
                }
                items = Query("SELECT product_id,quantity FROM purchase_items WHERE purchase_id=" + purchaseId);
            }
            foreach (DataRow item in items.Rows) MoveStock(Convert.ToInt64(item["product_id"]), -Convert.ToDecimal(item["quantity"]), "Devolução de compra", "Estorno da compra " + number + ": " + reason, null, null);
            AddAudit("purchase_cancelled", number + ": " + reason);
        }

        public void MoveStock(long productId, decimal quantity, string movementType, string reason, long? workOrderId, long? itemId)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    decimal previous;
                    using (var current = new SQLiteCommand("SELECT stock_quantity FROM products WHERE id=@id", connection, transaction))
                    {
                        current.Parameters.AddWithValue("@id", productId);
                        var currentValue = current.ExecuteScalar();
                        if (currentValue == null || currentValue == DBNull.Value) throw new InvalidOperationException("O produto não foi encontrado no estoque.");
                        previous = Convert.ToDecimal(currentValue);
                    }
                    var result = previous + quantity;
                    using (var update = new SQLiteCommand("UPDATE products SET stock_quantity=@quantity WHERE id=@id", connection, transaction))
                    {
                        update.Parameters.AddWithValue("@quantity", result); update.Parameters.AddWithValue("@id", productId);
                        if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Não foi possível atualizar o saldo do produto.");
                    }
                    using (var insert = new SQLiteCommand("INSERT INTO stock_movements (product_id, work_order_id, work_order_item_id, movement_type, quantity, previous_quantity, resulting_quantity, reason, created_at) VALUES (@product_id,@work_order_id,@item_id,@type,@quantity,@previous,@result,@reason,@created)", connection, transaction))
                    {
                        insert.Parameters.AddWithValue("@product_id", productId); insert.Parameters.AddWithValue("@work_order_id", workOrderId.HasValue ? (object)workOrderId.Value : DBNull.Value); insert.Parameters.AddWithValue("@item_id", itemId.HasValue ? (object)itemId.Value : DBNull.Value); insert.Parameters.AddWithValue("@type", movementType); insert.Parameters.AddWithValue("@quantity", quantity); insert.Parameters.AddWithValue("@previous", previous); insert.Parameters.AddWithValue("@result", result); insert.Parameters.AddWithValue("@reason", reason); insert.Parameters.AddWithValue("@created", DateTime.UtcNow.ToString("o")); insert.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
            }
            AddAudit("stock_moved", movementType + ": " + productId);
        }

        public void MoveStockManually(long productId, decimal quantity, string movementType, string reason)
        {
            RequirePermission("stock.adjust");
            MoveStock(productId, quantity, movementType, reason, null, null);
        }

        public string NextWorkOrderNumber()
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT COALESCE(MAX(id), 0) + 1 FROM work_orders", connection))
            { connection.Open(); var next = Convert.ToInt64(command.ExecuteScalar()); return "OS-" + DateTime.Now.Year + "-" + next.ToString("000000"); }
        }

        public void SaveWorkOrder(long? id, string number, long clientId, long vehicleId, string status, string expectedAt, string mileage, string fuelLevel, string complaint, string diagnosis, string notes, int warrantyDays)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(@"INSERT INTO work_orders (id, number, client_id, vehicle_id, status, opened_at, expected_at, mileage, fuel_level, complaint, diagnosis, notes, warranty_days) VALUES (@id, @number, @client_id, @vehicle_id, @status, @opened_at, @expected_at, @mileage, @fuel_level, @complaint, @diagnosis, @notes, @warranty_days)
ON CONFLICT(id) DO UPDATE SET client_id=@client_id, vehicle_id=@vehicle_id, status=@status, expected_at=@expected_at, mileage=@mileage, fuel_level=@fuel_level, complaint=@complaint, diagnosis=@diagnosis, notes=@notes, warranty_days=@warranty_days;", connection))
            {
                command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@number", number); command.Parameters.AddWithValue("@client_id", clientId); command.Parameters.AddWithValue("@vehicle_id", vehicleId); command.Parameters.AddWithValue("@status", status); command.Parameters.AddWithValue("@opened_at", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); command.Parameters.AddWithValue("@expected_at", expectedAt); command.Parameters.AddWithValue("@mileage", mileage); command.Parameters.AddWithValue("@fuel_level", fuelLevel); command.Parameters.AddWithValue("@complaint", complaint); command.Parameters.AddWithValue("@diagnosis", diagnosis); command.Parameters.AddWithValue("@notes", notes); command.Parameters.AddWithValue("@warranty_days", warrantyDays);
                connection.Open(); command.ExecuteNonQuery();
            }
            if (id.HasValue) SynchronizeWorkOrderStock(id.Value, status);
            AddAudit("work_order_saved", number);
        }

        public void UpdateWorkOrderStatus(long id, string status)
        { RequirePermission("service.manage"); 
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var command = new SQLiteCommand("UPDATE work_orders SET status=@status, finalized_at=CASE WHEN @status='Finalizada' THEN @now ELSE finalized_at END, cancelled_at=CASE WHEN @status='Cancelada' THEN @now ELSE cancelled_at END WHERE id=@id", connection)) { connection.Open(); command.Parameters.AddWithValue("@id", id); command.Parameters.AddWithValue("@status", status); command.Parameters.AddWithValue("@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); command.ExecuteNonQuery(); }
            SynchronizeWorkOrderStock(id, status); AddAudit("work_order_status", id + ": " + status);
        }

        public void ReceiveWorkOrder(long workOrderId, string paymentMethod)
        { RequirePermission("finance.manage"); 
            string number, client; decimal amount; using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var read = new SQLiteCommand("SELECT work_orders.number,clients.name,COALESCE((SELECT SUM(quantity*unit_price) FROM work_order_items WHERE work_order_id=work_orders.id),0) FROM work_orders INNER JOIN clients ON clients.id=work_orders.client_id WHERE work_orders.id=@id", connection)) { connection.Open();read.Parameters.AddWithValue("@id",workOrderId);using(var r=read.ExecuteReader()){if(!r.Read())throw new InvalidOperationException("OS não encontrada.");number=Convert.ToString(r[0]);client=Convert.ToString(r[1]);amount=Convert.ToDecimal(r[2]);}using(var check=new SQLiteCommand("SELECT COUNT(1) FROM financial_entries WHERE work_order_id=@id AND status='Pago'",connection)){check.Parameters.AddWithValue("@id",workOrderId);if(Convert.ToInt32(check.ExecuteScalar())>0)throw new InvalidOperationException("Esta OS já possui recebimento registrado.");}}
            DataTable open=null;if(paymentMethod=="Dinheiro"){open=GetOpenCash();if(open.Rows.Count==0)throw new InvalidOperationException("Abra o caixa antes de receber em dinheiro.");}
            using(var connection=new SQLiteConnection("Data Source="+_databasePath+";Version=3;"))using(var command=new SQLiteCommand("INSERT INTO financial_entries (entry_type,party_name,category,description,document_number,amount,due_date,paid_at,payment_method,status,origin,work_order_id,created_at) VALUES ('Receber',@party,'Serviços',@description,@document,@amount,@due,@paid,@payment,'Pago','OS',@os,@created)",connection)){connection.Open();command.Parameters.AddWithValue("@party",client);command.Parameters.AddWithValue("@description","Recebimento da OS "+number);command.Parameters.AddWithValue("@document",number);command.Parameters.AddWithValue("@amount",amount);command.Parameters.AddWithValue("@due",DateTime.Now.ToString("yyyy-MM-dd"));command.Parameters.AddWithValue("@paid",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));command.Parameters.AddWithValue("@payment",paymentMethod);command.Parameters.AddWithValue("@os",workOrderId);command.Parameters.AddWithValue("@created",DateTime.UtcNow.ToString("o"));command.ExecuteNonQuery();}
            if(paymentMethod=="Dinheiro")AddCashMovement(Convert.ToInt64(open.Rows[0]["id"]),"Recebimento de OS","Dinheiro",amount,"Recebimento da OS "+number);
            UpdateWorkOrderStatus(workOrderId,"Pronta para retirada");
            AddAudit("work_order_received",number+": "+paymentMethod);
        }

        private void SynchronizeWorkOrderStock(long workOrderId, string status)
        {
            var shouldProcess = status == "Serviço concluído" || status == "Aguardando pagamento" || status == "Pronta para retirada" || status == "Finalizada";
            if (status == "Cancelada") { ReverseWorkOrderStock(workOrderId); return; }
            if (!shouldProcess) return;
            var items = Query("SELECT id, product_id, quantity FROM work_order_items WHERE work_order_id=" + workOrderId + " AND item_type='Peça' AND product_id IS NOT NULL");
            foreach (DataRow item in items.Rows) { var itemId = Convert.ToInt64(item["id"]); using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var check = new SQLiteCommand("SELECT COUNT(1) FROM stock_movements WHERE work_order_item_id=@item_id AND movement_type='Uso em OS'", connection)) { connection.Open(); check.Parameters.AddWithValue("@item_id", itemId); if (Convert.ToInt32(check.ExecuteScalar()) == 0) MoveStock(Convert.ToInt64(item["product_id"]), -Convert.ToDecimal(item["quantity"]), "Uso em OS", "Baixa automática ao concluir OS", workOrderId, itemId); } }
        }

        private void ReverseWorkOrderStock(long workOrderId)
        {
            var movements = Query("SELECT product_id, work_order_item_id, quantity FROM stock_movements WHERE work_order_id=" + workOrderId + " AND movement_type='Uso em OS'");
            foreach (DataRow movement in movements.Rows) { var itemId = Convert.ToInt64(movement["work_order_item_id"]); using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;")) using (var check = new SQLiteCommand("SELECT COUNT(1) FROM stock_movements WHERE work_order_item_id=@item_id AND movement_type='Estorno de OS'", connection)) { connection.Open(); check.Parameters.AddWithValue("@item_id", itemId); if (Convert.ToInt32(check.ExecuteScalar()) == 0) MoveStock(Convert.ToInt64(movement["product_id"]), -Convert.ToDecimal(movement["quantity"]), "Estorno de OS", "Estorno automático por cancelamento da OS", workOrderId, itemId); } }
        }

        private void ReverseWorkOrderItemStock(long itemId, string reason)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("SELECT product_id, work_order_id, quantity FROM stock_movements WHERE work_order_item_id=@id AND movement_type='Uso em OS' AND NOT EXISTS (SELECT 1 FROM stock_movements r WHERE r.work_order_item_id=@id AND r.movement_type IN ('Estorno de OS','Estorno de item da OS'))", connection))
            {
                connection.Open(); command.Parameters.AddWithValue("@id", itemId);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var productId = Convert.ToInt64(reader["product_id"]);
                        var workOrderId = Convert.ToInt64(reader["work_order_id"]);
                        var quantity = -Convert.ToDecimal(reader["quantity"]);
                        reader.Close();
                        MoveStock(productId, quantity, "Estorno de item da OS", reason, workOrderId, itemId);
                    }
                }
            }
        }

        public void DeleteWorkOrder(long id)
        {
            RequirePermission("service.manage");
            ReverseWorkOrderStock(id);
            var items = Query("SELECT id FROM work_order_items WHERE work_order_id=" + id);
            foreach (DataRow item in items.Rows) ReverseWorkOrderItemStock(Convert.ToInt64(item["id"]), "Estorno pela exclusão da OS");
            Execute("DELETE FROM work_order_items WHERE work_order_id=@id; DELETE FROM work_orders WHERE id=@id;", id);
            AddAudit("work_order_deleted", id.ToString());
        }

        public void SaveClient(long? id, string name, string document, string phone, string email)
        { RequirePermission(id.HasValue ? "clients.edit" : "clients.create"); 
            Execute(@"INSERT INTO clients (id, name, document, phone, email) VALUES (@id, @name, @document, @phone, @email)
ON CONFLICT(id) DO UPDATE SET name=@name, document=@document, phone=@phone, email=@email;", id, name, document, phone, email);
            AddAudit("client_saved", name);
        }

        public long SaveVehicle(long? id, long clientId, string type, string plate, string manufacturer, string model, string yearModel)
        { RequirePermission("service.manage"); 
            long savedId;
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(@"INSERT INTO vehicles (id, client_id, type, plate, manufacturer, model, year_model) VALUES (@id, @client_id, @type, @plate, @manufacturer, @model, @year_model)
ON CONFLICT(id) DO UPDATE SET client_id=@client_id, type=@type, plate=@plate, manufacturer=@manufacturer, model=@model, year_model=@year_model;", connection))
            {
                command.Parameters.AddWithValue("@id", id.HasValue ? (object)id.Value : DBNull.Value); command.Parameters.AddWithValue("@client_id", clientId); command.Parameters.AddWithValue("@type", type); command.Parameters.AddWithValue("@plate", plate); command.Parameters.AddWithValue("@manufacturer", manufacturer); command.Parameters.AddWithValue("@model", model); command.Parameters.AddWithValue("@year_model", yearModel);
                connection.Open(); command.ExecuteNonQuery(); savedId=id ?? connection.LastInsertRowId;
            }
            AddAudit("vehicle_saved", model); return savedId;
        }

        public void DeleteClient(long id) { RequirePermission("clients.delete");  Execute("DELETE FROM vehicles WHERE client_id=@id; DELETE FROM clients WHERE id=@id;", id); AddAudit("client_deleted", id.ToString()); }
        public void DeleteVehicle(long id) { RequirePermission("service.manage");  Execute("DELETE FROM vehicles WHERE id=@id;", id); AddAudit("vehicle_deleted", id.ToString()); }

        private DataTable Query(string sql)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var adapter = new SQLiteDataAdapter(sql, connection)) { var table = new DataTable(); adapter.Fill(table); return table; }
        }

        private void Execute(string sql, params object[] values)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand(sql, connection))
            {
                connection.Open();
                for (var i = 0; i < values.Length; i++) command.Parameters.AddWithValue(i == 0 ? "@id" : ParameterName(i), values[i] ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private static string ParameterName(int index)
        {
            string[] names = { "@id", "@name", "@document", "@phone", "@email", "@client_id", "@type", "@plate", "@manufacturer", "@model", "@year_model" };
            return index < names.Length ? names[index] : "@p" + index;
        }

        private void AddAudit(string action, string details)
        {
            using (var connection = new SQLiteConnection("Data Source=" + _databasePath + ";Version=3;"))
            using (var command = new SQLiteCommand("INSERT INTO audit_logs (action, details_json, created_at) VALUES (@action, @details, @created_at)", connection))
            { connection.Open(); command.Parameters.AddWithValue("@action", action); command.Parameters.AddWithValue("@details", details); command.Parameters.AddWithValue("@created_at", DateTime.UtcNow.ToString("o")); command.ExecuteNonQuery(); }
        }
    }
}
