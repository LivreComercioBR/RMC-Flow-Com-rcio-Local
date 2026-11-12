using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using RMCFlowComercioLocal.Infrastructure;

namespace RMCFlowComercioLocal
{
    internal sealed class AppForm : Form
    {
        private readonly LocalDatabase _database;
        private readonly SystemUser _user;
        private readonly TabControl _tabs = new TabControl();
        private readonly DataGridView _clients = Grid();
        private readonly DataGridView _vehicles = Grid();
        private readonly TextBox _clientSearch = new TextBox();
        private readonly TextBox _vehicleSearch = new TextBox();
        private readonly DataGridView _appointments = Grid();
        private readonly DataGridView _workOrders = Grid();
        private readonly ComboBox _workOrderStatus = new ComboBox();
        private readonly TextBox _workOrderSearch = new TextBox();
        private readonly DataGridView _quotes = Grid();
        private readonly DataGridView _products = Grid();
        private readonly DataGridView _services = Grid();
        private readonly TextBox _productSearch = new TextBox();
        private readonly TextBox _serviceSearch = new TextBox();
        private readonly DataGridView _stockMovements = Grid();
        private readonly DataGridView _financial = Grid();
        private readonly ComboBox _financialStatus = new ComboBox();
        private readonly TextBox _financialSearch = new TextBox();
        private readonly DataGridView _suppliers = Grid();
        private readonly DataGridView _debtors = Grid();
        private readonly DataGridView _financialCategories = Grid();
        private readonly DataGridView _purchases = Grid();
        private readonly TextBox _supplierSearch = new TextBox();
        private readonly TextBox _debtorSearch = new TextBox();
        private readonly Label _summary = new Label();
        private readonly Label _financialSummary = new Label();
        private readonly Label _dashToday = new Label(); private readonly Label _dashMonth = new Label(); private readonly Label _dashCash = new Label(); private readonly Label _dashReceive = new Label(); private readonly Label _dashPay = new Label(); private readonly Label _dashOrders = new Label(); private readonly Label _dashReady = new Label(); private readonly Label _dashStock = new Label(); private readonly Label _dashAlerts = new Label();
        private bool _dashValuesHidden;

        public AppForm(LocalDatabase database, SystemUser user)
        {
            _database = database; _user=user;
#if DEBUG
            Text = "RMC Flow Com\u00e9rcio Local - DESENVOLVIMENTO";
#else
            Text = "RMC Flow Com\u00e9rcio Local";
#endif
            StartPosition = FormStartPosition.CenterScreen; Size = new Size(1080, 700); MinimumSize = new Size(920, 600);
            BackColor = Color.FromArgb(7, 20, 44); Build(); ApplyPermissions(); RefreshData();
        }

        private void Build()
        {
            var title = new Label { Text = "RMC Flow Comércio Local", Location = new Point(24, 16), AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(78, 224, 232) };
            var subtitle = new Label { Text = "Usuário: " + _user.Name + " • " + _user.Role, Location = new Point(28, 52), AutoSize = true, ForeColor = Color.FromArgb(201, 226, 244) };
            var password=new Button { Text = "Minha senha", Location = new Point(810, 30), Size = new Size(108, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.FromArgb(84,102,180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; password.Click+=(s,e)=>{using(var form=new ChangePasswordForm(_database,_user,true))form.ShowDialog(this);};Controls.Add(password);
            var exit = new Button { Text = "Sair", Location = new Point(928, 30), Size = new Size(92, 32), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.FromArgb(180, 65, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            exit.Click += (s, e) => { if (MessageBox.Show("Deseja sair do RMC Flow Comércio Local?", "Sair", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) Close(); };
            Controls.Add(title); Controls.Add(subtitle); Controls.Add(exit);
            _tabs.Location = new Point(20, 82); _tabs.Size = new Size(1025, 560); _tabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _tabs.TabPages.Add(DashboardPage());
            _tabs.TabPages.Add(ClientsPage());
            _tabs.TabPages.Add(ProductsPage());
            if (ServiceModuleEnabled())
            {
                _tabs.TabPages.Add(VehiclesPage());
                _tabs.TabPages.Add(ServicesPage());
                _tabs.TabPages.Add(AppointmentsPage());
                _tabs.TabPages.Add(WorkOrdersPage());
                _tabs.TabPages.Add(WarrantiesPage());
                _tabs.TabPages.Add(QuotesPage());
            }
            _tabs.TabPages.Add(StockPage());
            _tabs.TabPages.Add(PdvPage());
            _tabs.TabPages.Add(CashPage());
            _tabs.TabPages.Add(FinancialPage());
            _tabs.TabPages.Add(ReportsPage());
            _tabs.TabPages.Add(PurchasesPage());
            _tabs.TabPages.Add(SuppliersPage());
            _tabs.TabPages.Add(DebtorsPage());
            _tabs.TabPages.Add(CategoriesPage());
            _tabs.TabPages.Add(SettingsPage());
            Controls.Add(_tabs);
        }

        private void ApplyPermissions()
        {
            if (_user.Role == "Administrador") return;
            for (var i=_tabs.TabPages.Count-1;i>=0;i--)
            {
                var page=_tabs.TabPages[i]; var key=PermissionForTab(page.Text);
                if(key!=null && !_database.HasPermission(_user.Id,_user.Role,key)) _tabs.TabPages.RemoveAt(i);
                else ApplyActionPermissions(page);
            }
        }
        private static string PermissionForTab(string title)
        {
            if(title.Contains("Painel"))return "dashboard.view"; if(title.Contains("Agenda")||title.Contains("Garantia"))return "service.view"; if(title.Contains("Clientes"))return "clients.view"; if(title.Contains("Servi"))return "service.view"; if(title.Contains("Produtos"))return "catalog.view"; if(title.Contains("Estoque"))return "stock.view"; if(title.Contains("PDV"))return "sales.view"; if(title.Contains("Caixa"))return "cash.view"; if(title.Contains("Financeiro")||title.Contains("Devedor")||title.Contains("Categoria"))return "finance.view"; if(title.Contains("Relat"))return "reports.view"; if(title.Contains("Compra"))return "purchases.view"; if(title.Contains("Fornecedor"))return "suppliers.view"; if(title.Contains("Ordem")||title.Contains("Equipamento")||title.Contains("Agendamento"))return "service.view"; if(title.Contains("Orcamento")||title.Contains("Orçamento"))return "quotes.view"; if(title.Contains("Config"))return "settings.view"; return null;
        }
        private void ApplyActionPermissions(TabPage page)
        {
            foreach(Control control in page.Controls) if(control is Button)
            {
                var button=(Button)control; var label=button.Text.ToLowerInvariant(); string key=null;
                if(label.Contains("novo cliente"))key="clients.create"; else if(label.Contains("editar")&&page.Text.Contains("Cliente"))key="clients.edit"; else if(label.Contains("excluir")&&page.Text.Contains("Cliente"))key="clients.delete";
                else if(page.Text.Contains("Produtos")&&label.Contains("novo produto"))key="catalog.create"; else if(page.Text.Contains("Produtos")&&label.Contains("editar"))key="catalog.edit"; else if(page.Text.Contains("Produtos")&&label.Contains("excluir"))key="catalog.delete";
                else if(page.Text.Contains("Servi")&&(label.Contains("novo")||label.Contains("editar")||label.Contains("excluir")))key="service.manage";
                else if(page.Text.Contains("Orcamento")||page.Text.Contains("Orçamento"))key="quotes.manage";
                else if(page.Text.Contains("Agenda")||page.Text.Contains("Equipamento")||page.Text.Contains("Ordens"))key="service.manage";
                else if(label.Contains("ajust")||label.Contains("moviment"))key="stock.adjust";
                else if(label.Contains("nova venda")||label.Contains("finalizar venda"))key="sales.create"; else if(label.Contains("cancelar venda"))key="sales.cancel";
                else if(label.Contains("abrir caixa"))key="cash.open"; else if(label.Contains("fechar caixa"))key="cash.close"; else if(label.Contains("suprimento")||label.Contains("sangria"))key="cash.movement";
                else if(label.Contains("nova compra")||label.Contains("cancelar / devolver"))key="purchases.manage";
                else if(label.Contains("nova ordem")||label.Contains("nova os")||label.Contains("novo servi"))key="service.manage";
                else if(label.Contains("novo orçamento"))key="quotes.manage";
                if(key==null && PermissionForTab(page.Text)=="finance.view" && label!="buscar")key="finance.manage"; if(key==null && PermissionForTab(page.Text)=="suppliers.view" && label!="buscar")key="suppliers.manage"; if(key!=null)button.Enabled=_database.HasPermission(_user.Id,_user.Role,key);
            }
        }        private bool ServiceModuleEnabled() { return _database.Get("modules.services") == "1"; }
        private void RemoveTab(string name){foreach(TabPage page in _tabs.TabPages)if(page.Text==name){_tabs.TabPages.Remove(page);return;}}

        private TabPage DashboardPage()
        {
            var page = new TabPage("Painel") { BackColor = Color.FromArgb(6, 48, 82), Padding = new Padding(12) };
            var hero = new Panel { Location = new Point(20, 18), Size = new Size(958, 92), BackColor = Color.FromArgb(4, 40, 72), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            hero.Controls.Add(new Label { Text = "Dashboard", Location = new Point(22, 16), AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White });
            hero.Controls.Add(new Label { Text = "Acompanhe os principais indicadores da empresa.", Location = new Point(24, 56), AutoSize = true, Font = new Font("Segoe UI", 11), ForeColor = Color.FromArgb(125, 232, 241) });
            page.Controls.Add(hero);
            AddDashboardCard(page, "Entradas hoje", _dashToday, 20, 126, Color.FromArgb(13,80,145), Color.White); AddDashboardCard(page, "Resultado do mês", _dashMonth, 260, 126, Color.FromArgb(5,130,171), Color.White); AddDashboardCard(page, "Dinheiro no caixa", _dashCash, 500, 126, Color.FromArgb(18,170,186), Color.White); AddDashboardCard(page, "Contas a receber", _dashReceive, 740, 126, Color.FromArgb(12,91,163), Color.White);
            AddDashboardCard(page, "Contas a pagar", _dashPay, 20, 235, Color.FromArgb(9,70,128), Color.White);
            if (ServiceModuleEnabled())
            {
                AddDashboardCard(page, "OS em andamento", _dashOrders, 260, 235, Color.FromArgb(84,202,213), Color.FromArgb(4,40,72));
                AddDashboardCard(page, "OS prontas", _dashReady, 500, 235, Color.FromArgb(84,202,213), Color.FromArgb(4,40,72));
                AddDashboardCard(page, "Estoque baixo", _dashStock, 740, 235, Color.FromArgb(84,202,213), Color.FromArgb(4,40,72));
            }
            else
            {
                AddDashboardCard(page, "Estoque baixo", _dashStock, 260, 235, Color.FromArgb(84,202,213), Color.FromArgb(4,40,72));
            }
            var bottom = new Panel { Location=new Point(20,345),Size=new Size(958,135),BackColor=Color.FromArgb(4,57,97),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right };
            bottom.Controls.Add(new Label { Text="Atenção operacional",Location=new Point(20,18),AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=Color.White });
            _dashAlerts.Location=new Point(22,54);_dashAlerts.Size=new Size(470,58);_dashAlerts.Font=new Font("Segoe UI",10);_dashAlerts.ForeColor=Color.FromArgb(205,239,245);bottom.Controls.Add(_dashAlerts);
            var hide = new Button { Text = "Ocultar valores", Location = new Point(510,50), Size = new Size(210,34), BackColor = Color.FromArgb(24,189,200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, TextImageRelation = TextImageRelation.ImageBeforeText, ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleCenter, Image = CreateDashboardEyeIcon(false) };
            hide.Click += (s,e) => { _dashValuesHidden=!_dashValuesHidden; hide.Text=_dashValuesHidden?"Exibir valores":"Ocultar valores"; hide.Image=CreateDashboardEyeIcon(_dashValuesHidden); RefreshDashboard(); }; bottom.Controls.Add(hide);
            var alerts=new Button{Text="Ver alertas financeiros",Location=new Point(750,50),Size=new Size(170,34),BackColor=Color.FromArgb(232,139,37),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};alerts.Click+=(s,e)=>{using(var f=new FinancialAlertsForm(_database))f.ShowDialog(this);RefreshData();};bottom.Controls.Add(alerts);page.Controls.Add(bottom);
            return page;
        }

        private static void AddDashboardCard(TabPage page, string title, Label value, int x, int y, Color color, Color foreground)
        {
            var card=new Panel { Location=new Point(x,y),Size=new Size(220,92),BackColor=color }; card.Controls.Add(new Label { Text=title,Location=new Point(16,15),AutoSize=true,Font=new Font("Segoe UI",10),ForeColor=foreground }); value.Location=new Point(16,45);value.AutoSize=true;value.Font=new Font("Segoe UI",17,FontStyle.Bold);value.ForeColor=foreground;card.Controls.Add(value);page.Controls.Add(card);
        }

        private static Bitmap CreateDashboardEyeIcon(bool hidden)
        {
            var image = new Bitmap(28, 24);
            using (var graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.White, 1.7F))
            using (var brush = new SolidBrush(Color.White))
            {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var eye = new Rectangle(5, 7, 18, 11);
                graphics.DrawEllipse(pen, eye);
                graphics.FillEllipse(brush, new Rectangle(12, 10, 5, 5));
                if (hidden) graphics.DrawLine(pen, 4, 20, 24, 5);
            }
            return image;
        }

        private TabPage ClientsPage()
        {
            var page = Page("Clientes");
            page.Controls.Add(TopButton("Novo cliente", 18, (s, e) => { using (var form = new ClientEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionado", 145, (s, e) => EditClient()));
            page.Controls.Add(TopButton("Excluir selecionado", 300, (s, e) => DeleteClient()));
            page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(450,22),AutoSize=true});_clientSearch.Location=new Point(520,18);_clientSearch.Size=new Size(250,24);_clientSearch.TextChanged+=(s,e)=>RefreshClientSearch();page.Controls.Add(_clientSearch);var search=new Button{Text="Buscar",Location=new Point(782,16),Size=new Size(90,28)};search.Click+=(s,e)=>RefreshClientSearch();page.Controls.Add(search);
            _clients.Location = new Point(18, 62); _clients.Size = new Size(960, 420); _clients.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_clients);
            return page;
        }

        private TabPage VehiclesPage()
        {
            var page = Page("Equipamentos");
            page.Controls.Add(TopButton("Novo equipamento", 18, (s, e) => { using (var form = new VehicleEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionado", 145, (s, e) => EditVehicle()));
            page.Controls.Add(TopButton("Excluir selecionado", 300, (s, e) => DeleteVehicle()));
            page.Controls.Add(TopButton("Histórico", 455, (s, e) => OpenVehicleHistory()));
            page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(595,22),AutoSize=true});_vehicleSearch.Location=new Point(665,18);_vehicleSearch.Size=new Size(190,24);_vehicleSearch.TextChanged+=(s,e)=>RefreshVehicleSearch();page.Controls.Add(_vehicleSearch);var search=new Button{Text="Buscar",Location=new Point(865,16),Size=new Size(90,28)};search.Click+=(s,e)=>RefreshVehicleSearch();page.Controls.Add(search);
            _vehicles.Location = new Point(18, 62); _vehicles.Size = new Size(960, 420); _vehicles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_vehicles);
            return page;
        }

        private TabPage WorkOrdersPage()
        {
            var page = Page("Ordens de Serviço");
            page.Controls.Add(TopButton("Nova OS", 18, (s, e) => { using (var form = new WorkOrderEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionada", 145, (s, e) => EditWorkOrder()));
            page.Controls.Add(TopButton("Itens / orçamento", 300, (s, e) => EditWorkOrderItems()));
            page.Controls.Add(TopButton("Documento / WhatsApp", 455, (s, e) => OpenDocument()));
            page.Controls.Add(TopButton("Avançar status", 610, (s, e) => AdvanceWorkOrderStatus()));
            page.Controls.Add(TopButton("Excluir selecionada", 700, (s, e) => DeleteWorkOrder()));
            page.Controls.Add(TopButton("Receber OS", 850, (s, e) => ReceiveWorkOrder()));
            page.Controls.Add(new Label{Text="Status:",Location=new Point(18,58),AutoSize=true});_workOrderStatus.Items.AddRange(new object[]{"Todos","Aberta","Em diagnóstico","Aguardando orçamento","Aguardando aprovação","Aprovada","Em manutenção","Aguardando peça","Serviço concluído","Aguardando pagamento","Pronta para retirada","Finalizada","Cancelada"});_workOrderStatus.Text="Todos";_workOrderStatus.Location=new Point(70,54);_workOrderStatus.Size=new Size(160,24);_workOrderStatus.SelectedIndexChanged+=(s,e)=>RefreshWorkOrderSearch();page.Controls.Add(_workOrderStatus);page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(250,58),AutoSize=true});_workOrderSearch.Location=new Point(315,54);_workOrderSearch.Size=new Size(250,24);_workOrderSearch.TextChanged+=(s,e)=>RefreshWorkOrderSearch();page.Controls.Add(_workOrderSearch);var search=new Button{Text="Buscar",Location=new Point(578,52),Size=new Size(90,28)};search.Click+=(s,e)=>RefreshWorkOrderSearch();page.Controls.Add(search);
            _workOrders.Location = new Point(18, 92); _workOrders.Size = new Size(960, 390); _workOrders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_workOrders);
            return page;
        }

        private TabPage AppointmentsPage()
        {
            var page = Page("Agenda");
            page.Controls.Add(TopButton("Novo agendamento", 18, (s,e) => { using (var form = new AppointmentEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionado", 165, (s,e) => EditAppointment()));
            page.Controls.Add(TopButton("Confirmar", 320, (s,e) => ConfirmAppointment()));
            page.Controls.Add(TopButton("Cancelar", 440, (s,e) => CancelAppointment()));
            page.Controls.Add(TopButton("Gerar OS", 560, (s,e) => ConvertAppointment()));
            page.Controls.Add(TopButton("WhatsApp", 680, (s,e) => SendAppointmentWhatsApp()));
            _appointments.Location=new Point(18,62); _appointments.Size=new Size(960,420); _appointments.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; page.Controls.Add(_appointments);
            return page;
        }

        private TabPage QuotesPage()
        {
            var page = Page("Orçamentos");
            page.Controls.Add(TopButton("Novo orçamento", 18, (s, e) => { using (var form = new QuoteEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Abrir orçamento", 160, (s, e) => EditQuote()));
            page.Controls.Add(TopButton("Editar cabeçalho", 315, (s, e) => EditQuoteHeader()));
            page.Controls.Add(TopButton("Aprovar", 470, (s, e) => ApproveQuote()));
            page.Controls.Add(TopButton("Converter em OS", 595, (s, e) => ConvertQuote()));
            page.Controls.Add(TopButton("Reprovar", 750, (s, e) => RejectQuote()));
            _quotes.Location = new Point(18, 62); _quotes.Size = new Size(960, 420); _quotes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_quotes);
            return page;
        }

        private TabPage WarrantiesPage()
        {
            var page=Page("Garantias");var open=new Button{Text="Consultar garantias",Location=new Point(28,28),Size=new Size(185,40),BackColor=Color.FromArgb(24,189,200),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};open.Click+=(s,e)=>{using(var form=new WarrantyForm(_database))form.ShowDialog(this);};page.Controls.Add(open);page.Controls.Add(new Label{Text="Consulte OS finalizadas que estão em garantia ou já tiveram a validade vencida.",Location=new Point(30,88),AutoSize=true,ForeColor=Color.FromArgb(53,73,94)});return page;
        }

        private TabPage ProductsPage()
        {
            var page = Page("Produtos / Peças");
            page.Controls.Add(TopButton("Novo produto", 18, (s,e) => { using (var form = new ProductEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionado", 145, (s,e) => EditProduct()));
            page.Controls.Add(TopButton("Excluir selecionado", 300, (s,e) => DeleteProduct()));
            page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(450,22),AutoSize=true});_productSearch.Location=new Point(520,18);_productSearch.Size=new Size(250,24);_productSearch.TextChanged+=(s,e)=>RefreshProductSearch();page.Controls.Add(_productSearch);var search=new Button{Text="Buscar",Location=new Point(782,16),Size=new Size(90,28)};search.Click+=(s,e)=>RefreshProductSearch();page.Controls.Add(search);
            var pricing=new Button{Text="Formar preço",Location=new Point(18,54),Size=new Size(150,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"catalog.edit")};pricing.Click+=(sender,e)=>{var id=SelectedId(_products);if(!id.HasValue)return;using(var form=new ProductEditor(_database,_products.CurrentRow,true))form.ShowDialog(this);RefreshData();};page.Controls.Add(pricing);
            var adjustStock=new Button{Text="Entrada / ajuste",Location=new Point(175,54),Size=new Size(155,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"stock.adjust")};adjustStock.Click+=(sender,e)=>{var id=SelectedId(_products);if(!id.HasValue){MessageBox.Show("Selecione primeiro o produto que deseja movimentar.");return;}using(var form=new StockMovementEditor(_database,id))form.ShowDialog(this);RefreshData();};page.Controls.Add(adjustStock);
            _products.Location = new Point(18, 94); _products.Size = new Size(960, 388); _products.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_products); return page;
        }
        private TabPage ServicesPage()
        {
            var page = Page("Serviços");
            page.Controls.Add(TopButton("Novo serviço", 18, (s,e) => { using (var form = new ServiceEditor(_database, null)) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Editar selecionado", 145, (s,e) => EditService()));
            page.Controls.Add(TopButton("Excluir selecionado", 300, (s,e) => DeleteService()));
            page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(450,22),AutoSize=true});_serviceSearch.Location=new Point(520,18);_serviceSearch.Size=new Size(250,24);_serviceSearch.TextChanged+=(s,e)=>RefreshServiceSearch();page.Controls.Add(_serviceSearch);var search=new Button{Text="Buscar",Location=new Point(782,16),Size=new Size(90,28)};search.Click+=(s,e)=>RefreshServiceSearch();page.Controls.Add(search);
            _services.Location = new Point(18, 62); _services.Size = new Size(960, 420); _services.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_services); return page;
        }
        private TabPage StockPage()
        {
            var page = Page("Estoque");
            page.Controls.Add(TopButton("Entrada / ajuste", 18, (s,e) => { using (var form = new StockMovementEditor(_database)) form.ShowDialog(this); RefreshData(); }));
            _stockMovements.Location = new Point(18, 62); _stockMovements.Size = new Size(960, 420); _stockMovements.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(_stockMovements); return page;
        }
        private TabPage PdvPage()
        {
            var page = Page("PDV"); var open = new Button { Text = "Abrir PDV", Location = new Point(28, 28), Size = new Size(160, 40), BackColor = Color.FromArgb(24,189,200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = _database.HasPermission(_user.Id,_user.Role,"sales.create") }; open.Click += (s,e) => { using (var form = new PdvForm(_database)) form.ShowDialog(this); RefreshData(); }; page.Controls.Add(open);
            page.Controls.Add(new Label { Text = "Venda rápida com busca por nome, código interno ou leitor de código de barras.", Location = new Point(30, 88), AutoSize = true, ForeColor = Color.FromArgb(53,73,94) }); return page;
        }
        private TabPage CashPage()
        {
            var page = Page("Caixa"); var open = new Button { Text="Abrir / gerenciar caixa", Location=new Point(28,28), Size=new Size(190,40), BackColor=Color.FromArgb(24,189,200), ForeColor=Color.White, FlatStyle=FlatStyle.Flat }; open.Click += (s,e) => { using(var form=new CashForm(_database,_user)) form.ShowDialog(this); RefreshData(); }; page.Controls.Add(open); page.Controls.Add(new Label { Text="Abra o caixa antes de realizar vendas no PDV.", Location=new Point(30,88), AutoSize=true, ForeColor=Color.FromArgb(53,73,94) }); return page;
        }
        private TabPage FinancialPage()
        {
            var page = Page("Financeiro");
            page.Controls.Add(TopButton("Nova conta a receber", 18, (s,e) => { using(var form=new FinancialEntryEditor(_database, "Receber")) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Nova conta a pagar", 175, (s,e) => { using(var form=new FinancialEntryEditor(_database, "Pagar")) form.ShowDialog(this); RefreshData(); }));
            page.Controls.Add(TopButton("Baixar selecionada", 332, (s,e) => MarkFinancialPaid()));
            var cancel=new Button{Text="Cancelar conta",Location=new Point(487,16),Size=new Size(120,31)};cancel.Click+=(s,e)=>CancelFinancial();page.Controls.Add(cancel);
            var flow=new Button{Text="Fluxo de caixa",Location=new Point(618,16),Size=new Size(120,31)};flow.Click+=(s,e)=>{using(var form=new FinancialFlowForm(_database))form.ShowDialog(this);};page.Controls.Add(flow);
            var daily=new Button{Text="Resumo do dia",Location=new Point(750,16),Size=new Size(125,31)};daily.Click+=(s,e)=>{using(var form=new FinancialDailyForm(_database))form.ShowDialog(this);};page.Controls.Add(daily);
            var alerts=new Button{Text="Alertas",Location=new Point(887,16),Size=new Size(90,31)};alerts.Click+=(s,e)=>{using(var form=new FinancialAlertsForm(_database))form.ShowDialog(this);RefreshData();};page.Controls.Add(alerts);
            var receive=new Button{Text="Ver contas a receber",Location=new Point(18,48),Size=new Size(155,28)};receive.Click+=(s,e)=>{using(var form=new FinancialAccountsForm(_database,"Receber"))form.ShowDialog(this);RefreshData();};page.Controls.Add(receive);
            var pay=new Button{Text="Ver contas a pagar",Location=new Point(183,48),Size=new Size(155,28)};pay.Click+=(s,e)=>{using(var form=new FinancialAccountsForm(_database,"Pagar"))form.ShowDialog(this);RefreshData();};page.Controls.Add(pay);
            page.Controls.Add(new Label{Text="Status:",Location=new Point(360,53),AutoSize=true});_financialStatus.Items.AddRange(new object[]{"Todos","Pendente","Vencido","Pago","Cancelado"});_financialStatus.Text="Todos";_financialStatus.Location=new Point(412,49);_financialStatus.Size=new Size(115,24);_financialStatus.SelectedIndexChanged+=(s,e)=>RefreshFinancialSearch();page.Controls.Add(_financialStatus);
            page.Controls.Add(new Label{Text="Buscar:",Location=new Point(545,53),AutoSize=true});_financialSearch.Location=new Point(600,49);_financialSearch.Size=new Size(180,24);_financialSearch.BorderStyle=BorderStyle.FixedSingle;_financialSearch.TextChanged+=(s,e)=>RefreshFinancialSearch();page.Controls.Add(_financialSearch);var search=new Button{Text="Buscar",Location=new Point(792,47),Size=new Size(92,28)};search.Click+=(s,e)=>RefreshFinancialSearch();page.Controls.Add(search);
            _financialSummary.Location=new Point(20,84);_financialSummary.AutoSize=true;_financialSummary.Font=new Font("Segoe UI",9,FontStyle.Bold);_financialSummary.ForeColor=Color.FromArgb(7,55,94);page.Controls.Add(_financialSummary);
            _financial.Location = new Point(18,110); _financial.Size = new Size(960,372); _financial.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; page.Controls.Add(_financial);
            page.Controls.Add(new Label{Text="Dinheiro exige caixa aberto. PIX, cartão, transferência e boleto são registrados somente no fluxo financeiro.",Location=new Point(20,490),AutoSize=true,Anchor=AnchorStyles.Bottom|AnchorStyles.Left,ForeColor=Color.FromArgb(53,73,94)});
            return page;
        }
        private TabPage ReportsPage()
        {
            var page = Page("Relatórios");
            var open = new Button { Text = "Abrir relatórios gerenciais", Location = new Point(28, 28), Size = new Size(220, 40), BackColor = Color.FromArgb(24, 189, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            open.Click += (s, e) => { using (var form = new ReportsForm(_database)) form.ShowDialog(this); };
            page.Controls.Add(open);
            page.Controls.Add(new Label { Text = "Acompanhe receitas, despesas, resultado, vendas por pagamento, produtos vendidos, estoque baixo e ordens de serviço por período.", Location = new Point(30, 88), AutoSize = true, ForeColor = Color.FromArgb(53, 73, 94) });
            return page;
        }
        private TabPage PurchasesPage()
        {
            var page=Page("Compras"); page.Controls.Add(TopButton("Nova compra",18,(s,e)=>{using(var form=new PurchaseEditor(_database))form.ShowDialog(this);RefreshData();})); page.Controls.Add(TopButton("Cancelar / devolver",145,(s,e)=>CancelSelectedPurchase()));
            page.Controls.Add(new Label{Text="A compra aumenta o estoque e, se desejado, gera a conta a pagar ao fornecedor.",Location=new Point(290,23),AutoSize=true,ForeColor=Color.FromArgb(53,73,94)});
            _purchases.Location=new Point(18,62);_purchases.Size=new Size(960,420);_purchases.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;page.Controls.Add(_purchases);return page;
        }
        private TabPage SuppliersPage()
        {
            var page=Page("Fornecedores"); page.Controls.Add(TopButton("Novo fornecedor",18,(s,e)=>{using(var f=new SupplierEditor(_database,null))f.ShowDialog(this);RefreshData();})); page.Controls.Add(TopButton("Editar selecionado",145,(s,e)=>EditSupplier())); page.Controls.Add(TopButton("Excluir selecionado",300,(s,e)=>DeleteSupplier())); page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(480,22),AutoSize=true}); _supplierSearch.Location=new Point(550,18);_supplierSearch.Size=new Size(280,24);_supplierSearch.BorderStyle=BorderStyle.FixedSingle;_supplierSearch.TextChanged+=(s,e)=>RefreshSupplierSearch();page.Controls.Add(_supplierSearch); var search=new Button{Text="Buscar",Location=new Point(840,16),Size=new Size(92,28)};search.Click+=(s,e)=>RefreshSupplierSearch();page.Controls.Add(search); _suppliers.Location=new Point(18,62);_suppliers.Size=new Size(960,420);_suppliers.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;page.Controls.Add(_suppliers);return page;
        }
        private TabPage DebtorsPage()
        {
            var page=Page("Devedores"); page.Controls.Add(TopButton("Novo devedor",18,(s,e)=>{using(var f=new DebtorEditor(_database,null))f.ShowDialog(this);RefreshData();})); page.Controls.Add(TopButton("Editar selecionado",145,(s,e)=>EditDebtor())); page.Controls.Add(TopButton("Excluir selecionado",300,(s,e)=>DeleteDebtor())); page.Controls.Add(new Label{Text="Pesquisar:",Location=new Point(480,22),AutoSize=true}); _debtorSearch.Location=new Point(550,18);_debtorSearch.Size=new Size(280,24);_debtorSearch.BorderStyle=BorderStyle.FixedSingle;_debtorSearch.TextChanged+=(s,e)=>RefreshDebtorSearch();page.Controls.Add(_debtorSearch); var search=new Button{Text="Buscar",Location=new Point(840,16),Size=new Size(92,28)};search.Click+=(s,e)=>RefreshDebtorSearch();page.Controls.Add(search); _debtors.Location=new Point(18,62);_debtors.Size=new Size(960,420);_debtors.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;page.Controls.Add(_debtors);return page;
        }
        private TabPage CategoriesPage()
        {
            var page=Page("Categorias");page.Controls.Add(TopButton("Nova categoria",18,(s,e)=>{using(var f=new FinancialCategoryEditor(_database,null))f.ShowDialog(this);RefreshData();}));page.Controls.Add(TopButton("Editar selecionada",145,(s,e)=>EditFinancialCategory()));page.Controls.Add(TopButton("Excluir selecionada",300,(s,e)=>DeleteFinancialCategory()));_financialCategories.Location=new Point(18,62);_financialCategories.Size=new Size(960,420);_financialCategories.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;page.Controls.Add(_financialCategories);return page;
        }
        private TabPage SettingsPage()
        {
            var page = Page("Configurações");
            AddSettingsButton(page, "Dados da empresa", 28, "settings.company", () => { using (var form = new CompanySettingsForm(_database)) form.ShowDialog(this); });
            AddSettingsButton(page, "Módulos da empresa", 220, "settings.modules", () => { using (var form = new BusinessModulesForm(_database)) form.ShowDialog(this); });
            AddSettingsButton(page, "Criar backup", 412, "settings.backup", () => CreateBackup());
            AddSettingsButton(page, "Restaurar backup", 574, "settings.backup", () => RestoreBackup());
            AddSettingsButton(page, "Usuários", 736, "settings.users", () => { using (var form = new UsersForm(_database, _user.Id)) form.ShowDialog(this); });
            AddSettingsButton(page, "Auditoria", 876, "settings.audit", () => { using (var form = new AuditLogForm(_database)) form.ShowDialog(this); });
            page.Controls.Add(new Label { Text = "Configure os dados da empresa e escolha os módulos utilizados. Faça backups frequentes em uma pasta sincronizada ou dispositivo externo.", Location = new Point(30, 88), Size = new Size(940, 60), ForeColor = Color.FromArgb(53, 73, 94) });
            return page;
        }

        private void AddSettingsButton(TabPage page, string text, int x, string permission, Action action)
        {
            var button = new Button { Text = text, Location = new Point(x, 28), Size = new Size(150, 40), BackColor = Color.FromArgb(24, 110, 170), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            button.Enabled = _database.HasPermission(_user.Id,_user.Role,permission);
            button.Click += (s, e) => action();
            page.Controls.Add(button);
        }

        private void RefreshData()
        {
            var clients = _database.FindClients(_clientSearch.Text.Trim()); var vehicles = _database.FindVehicles(_vehicleSearch.Text.Trim()); var orders = _database.FindWorkOrders(string.IsNullOrEmpty(_workOrderStatus.Text)?"Todos":_workOrderStatus.Text,_workOrderSearch.Text.Trim());
            _clients.DataSource = clients; _vehicles.DataSource = vehicles; _appointments.DataSource=_database.GetAppointments(); _workOrders.DataSource = orders; _quotes.DataSource = _database.GetQuotes(); _products.DataSource = _database.FindProductsForList(_productSearch.Text.Trim()); _services.DataSource = _database.FindServicesForList(_serviceSearch.Text.Trim()); _stockMovements.DataSource = _database.GetStockMovements(); _financial.DataSource = _database.FindFinancialEntries(string.IsNullOrEmpty(_financialStatus.Text) ? "Todos" : _financialStatus.Text, _financialSearch.Text.Trim()); ConfigureFinancialGrid(); _purchases.DataSource=_database.GetPurchases(); _suppliers.DataSource=_database.GetSuppliers(); _debtors.DataSource=_database.GetDebtors(); _financialCategories.DataSource=_database.GetFinancialCategories();
            if (_workOrders.Columns["client_id"] != null) { _workOrders.Columns["client_id"].Visible = false; _workOrders.Columns["vehicle_id"].Visible = false; _workOrders.Columns["estado_entrada"].Visible = false; _workOrders.Columns["acessorios_recebidos"].Visible = false; _workOrders.Columns["reclamacao"].Visible = false; _workOrders.Columns["diagnostico"].Visible = false; _workOrders.Columns["observacoes"].Visible = false; _workOrders.Columns["historico_aprovacao"].Visible = false; }
            if (_quotes.Columns["client_id"] != null) { _quotes.Columns["client_id"].Visible = false; _quotes.Columns["vehicle_id"].Visible = false; _quotes.Columns["reclamacao"].Visible = false; _quotes.Columns["observacoes"].Visible = false; _quotes.Columns["os_convertida"].Visible = false; }
            ConfigureOperationalGrids();
            RefreshDashboard();
            _financialSummary.Text="A receber pendente: "+_database.GetFinancialTotal("Receber","Pendente").ToString("C2")+"  |  Vencido: "+_database.GetFinancialOverdueTotal("Receber").ToString("C2")+"     A pagar pendente: "+_database.GetFinancialTotal("Pagar","Pendente").ToString("C2")+"  |  Vencido: "+_database.GetFinancialOverdueTotal("Pagar").ToString("C2");
        }

        private void RefreshWorkOrderSearch(){_workOrders.DataSource=_database.FindWorkOrders(string.IsNullOrEmpty(_workOrderStatus.Text)?"Todos":_workOrderStatus.Text,_workOrderSearch.Text.Trim());if(_workOrders.Columns["client_id"]!=null){_workOrders.Columns["client_id"].Visible=false;_workOrders.Columns["vehicle_id"].Visible=false;_workOrders.Columns["estado_entrada"].Visible=false;_workOrders.Columns["acessorios_recebidos"].Visible=false;_workOrders.Columns["reclamacao"].Visible=false;_workOrders.Columns["diagnostico"].Visible=false;_workOrders.Columns["observacoes"].Visible=false;_workOrders.Columns["historico_aprovacao"].Visible=false;}ConfigureWorkOrderGrid();}
        private void RefreshClientSearch(){_clients.DataSource=_database.FindClients(_clientSearch.Text.Trim());ConfigureClientGrid();}
        private void RefreshVehicleSearch(){_vehicles.DataSource=_database.FindVehicles(_vehicleSearch.Text.Trim());ConfigureVehicleGrid();}
        private void RefreshProductSearch(){_products.DataSource=_database.FindProductsForList(_productSearch.Text.Trim());ConfigureProductGrid();}
        private void RefreshServiceSearch(){_services.DataSource=_database.FindServicesForList(_serviceSearch.Text.Trim());ConfigureServiceGrid();}
        private void ConfigureProductGrid()
        {
            if (_products.Columns.Contains("Código")) _products.Columns["Código"].Visible=false;
            _products.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None;
            SetProductWidth("Código interno",65); SetProductWidth("Código de barras",95); SetProductWidth("Produto / peça",190);
            SetProductWidth("Categoria",70); SetProductWidth("Unidade",45); SetProductWidth("Prateleira",55);
            SetProductWidth("Localização",75); SetProductWidth("Preço de venda",85); SetProductWidth("Em estoque",65);
            SetProductWidth("Estoque mínimo",70); SetProductWidth("Status",55);
        }
        private void SetProductWidth(string name,int width){if(_products.Columns.Contains(name)){_products.Columns[name].AutoSizeMode=DataGridViewAutoSizeColumnMode.None;_products.Columns[name].Width=width;}}

        private void ConfigureOperationalGrids()
        {
            ConfigureClientGrid(); ConfigureVehicleGrid(); ConfigureServiceGrid(); ConfigureAppointmentGrid(); ConfigureWorkOrderGrid();
            ConfigureQuoteGrid(); ConfigureStockGrid(); ConfigurePurchaseGrid(); ConfigureSupplierGrid(); ConfigureDebtorGrid(); ConfigureProductGrid();
        }
        private static void ConfigureGrid(DataGridView grid, string[] names, int[] widths)
        {
            grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.None; grid.RowHeadersWidth=24;
            for(var index=0;index<names.Length;index++) if(grid.Columns.Contains(names[index])) { grid.Columns[names[index]].AutoSizeMode=DataGridViewAutoSizeColumnMode.None; grid.Columns[names[index]].Width=widths[index]; }
        }
        private static void HideGridColumns(DataGridView grid, params string[] names){foreach(var name in names)if(grid.Columns.Contains(name))grid.Columns[name].Visible=false;}
        private void ConfigureClientGrid(){ConfigureGrid(_clients,new[]{"Código","Nome","CPF/CNPJ","Telefone","E-mail"},new[]{45,220,120,115,190});}
        private void ConfigureVehicleGrid(){ConfigureGrid(_vehicles,new[]{"Código","Cliente","Tipo","Identificador","Marca","Modelo","Versão"},new[]{45,170,70,80,95,175,55});}
        private void ConfigureServiceGrid(){HideGridColumns(_services,"Código");ConfigureGrid(_services,new[]{"Código interno","Serviço","Categoria","Duração estimada","Preço padrão","Status"},new[]{75,220,105,100,95,70});}
        private void ConfigureAppointmentGrid(){HideGridColumns(_appointments,"Observações");ConfigureGrid(_appointments,new[]{"Código","Data e hora","Cliente","Equipamento","Serviço previsto","Status","OS gerada"},new[]{45,120,145,155,145,95,90});}
        private void ConfigureWorkOrderGrid(){HideGridColumns(_workOrders,"Origem","Entrada","Previsão");ConfigureGrid(_workOrders,new[]{"Código","OS","Cliente","Equipamento","Status","Pagamento"},new[]{45,120,155,185,145,95});}
        private void ConfigureQuoteGrid(){ConfigureGrid(_quotes,new[]{"Código","Orçamento","Cliente","Equipamento","Status","Emissão","Validade","Total"},new[]{45,120,140,165,90,85,85,90});}
        private void ConfigureStockGrid(){ConfigureGrid(_stockMovements,new[]{"Produto","Tipo","Quantidade","Saldo anterior","Saldo final","OS","Motivo","Data"},new[]{250,95,80,90,90,65,210,135});}
        private void ConfigurePurchaseGrid(){ConfigureGrid(_purchases,new[]{"Código","Compra","Fornecedor","NF / documento","Data","Total","Conta a pagar","Status"},new[]{45,115,150,125,80,90,90,75});}
        private void ConfigureSupplierGrid(){ConfigureGrid(_suppliers,new[]{"Código","Fornecedor","Contato","CPF/CNPJ","Telefone","E-mail","Status"},new[]{45,155,115,115,105,160,70});}
        private void ConfigureDebtorGrid(){ConfigureGrid(_debtors,new[]{"Código","Devedor","CPF/CNPJ","Telefone","E-mail","Status"},new[]{45,185,120,105,160,70});}

        private void RefreshDashboard()
        {
            var now=DateTime.Today; var daily=_database.GetReportSummary(now,now).Rows[0]; var monthStart=new DateTime(now.Year,now.Month,1);var monthly=_database.GetReportSummary(monthStart,now).Rows[0];var entries=Convert.ToDecimal(daily["received"]);var monthResult=Convert.ToDecimal(monthly["received"])-Convert.ToDecimal(monthly["paid"]);var openCash=_database.GetOpenCash();var cash=openCash.Rows.Count==0?0M:_database.GetCashExpected(Convert.ToInt64(openCash.Rows[0]["id"]));var orders=_database.GetWorkOrders();var active=0;var ready=0;foreach(DataRow row in orders.Rows){var status=Convert.ToString(row["Status"]);if(status=="Pronta para retirada")ready++;else if(status!="Finalizada"&&status!="Cancelada")active++;}var alerts=_database.GetFinancialAlerts(7);var overdue=0;var today=0;var upcoming=0;foreach(DataRow alert in alerts.Rows){var kind=Convert.ToString(alert["Alerta"]);if(kind=="Vencida")overdue++;else if(kind=="Vence hoje")today++;else upcoming++;}_dashToday.Text=DashMoney(entries);_dashMonth.Text=DashMoney(monthResult);_dashCash.Text=DashMoney(cash);_dashReceive.Text=DashMoney(_database.GetFinancialTotal("Receber","Pendente"));_dashPay.Text=DashMoney(_database.GetFinancialTotal("Pagar","Pendente"));_dashOrders.Text=active.ToString();_dashReady.Text=ready.ToString();_dashStock.Text=_database.GetLowStockCount().ToString();_dashAlerts.Text="Contas vencidas: "+overdue+"   •   Vencem hoje: "+today+"   •   Próximos 7 dias: "+upcoming+"\nClientes, equipamentos, vendas, estoque e financeiro integrados em um único painel.";
        }

        private string DashMoney(decimal value) { return _dashValuesHidden ? "R$ •••••" : value.ToString("C2"); }

        private void EditClient() { var id = SelectedId(_clients); if (id == null) return; using (var form = new ClientEditor(_database, _clients.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void CreateBackup(){using(var dialog=new FolderBrowserDialog()){dialog.Description="Escolha a pasta onde o backup será salvo";if(dialog.ShowDialog(this)!=DialogResult.OK)return;try{var file=_database.CreateBackup(dialog.SelectedPath);MessageBox.Show("Backup criado com sucesso:\n"+file,"Backup",MessageBoxButtons.OK,MessageBoxIcon.Information);}catch(Exception ex){MessageBox.Show(ex.Message,"Backup",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}}
        private void RestoreBackup(){using(var dialog=new OpenFileDialog()){dialog.Title="Selecione o backup do RMC Flow Comércio Local";dialog.Filter="Backup do RMC Flow (*.db)|*.db";dialog.Multiselect=false;if(dialog.ShowDialog(this)!=DialogResult.OK)return;if(MessageBox.Show("A base atual será substituída pelo backup escolhido. Uma cópia de segurança do estado atual será criada automaticamente.\n\nDeseja continuar?","Restaurar backup",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;try{var safetyCopy=_database.RestoreBackup(dialog.FileName);MessageBox.Show("Backup restaurado com sucesso.\n\nCópia de segurança da base anterior:\n"+safetyCopy+"\n\nO sistema será reiniciado.","Restaurar backup",MessageBoxButtons.OK,MessageBoxIcon.Information);Application.Restart();}catch(Exception ex){MessageBox.Show(ex.Message,"Restaurar backup",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}}
        private void EditVehicle() { var id = SelectedId(_vehicles); if (id == null) return; using (var form = new VehicleEditor(_database, _vehicles.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void OpenVehicleHistory() { var id=SelectedId(_vehicles); if(id==null)return; using(var form=new VehicleHistoryForm(_database,id.Value))form.ShowDialog(this); }
        private void EditAppointment() { var id = SelectedId(_appointments); if (id == null) return; if (Convert.ToString(_appointments.CurrentRow.Cells["Status"].Value)=="Convertido") { MessageBox.Show("O agendamento já foi convertido em OS e não pode mais ser alterado."); return; } using (var form = new AppointmentEditor(_database, _appointments.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void ConfirmAppointment() { var id=SelectedId(_appointments); if(id==null)return; try { _database.ConfirmAppointment(id.Value); RefreshData(); MessageBox.Show("Atendimento confirmado.", "Agenda"); } catch(Exception ex) { MessageBox.Show(ex.Message, "Agenda", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
        private void CancelAppointment() { var id = SelectedId(_appointments); if (id == null) return; if (MessageBox.Show("Cancelar o agendamento selecionado?", "Agenda", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; try { _database.CancelAppointment(id.Value); RefreshData(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Agenda", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
        private void ConvertAppointment() { var id = SelectedId(_appointments); if (id == null) return; if (MessageBox.Show("Gerar uma Ordem de Serviço para este agendamento?", "Agenda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return; try { var number = _database.ConvertAppointmentToWorkOrder(id.Value); RefreshData(); MessageBox.Show("A " + number + " foi criada. Inclua peças e serviços pelo módulo de OS.", "Agenda"); } catch (Exception ex) { MessageBox.Show(ex.Message, "Agenda", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
        private void SendAppointmentWhatsApp()
        {
            var id=SelectedId(_appointments); if(id==null)return; var data=_database.GetAppointmentDocument(id.Value); if(data.Rows.Count==0){MessageBox.Show("Agendamento não encontrado.");return;} var item=data.Rows[0]; if(Convert.ToString(item["status"])=="Cancelado"){MessageBox.Show("Não é possível enviar lembrete de um agendamento cancelado.");return;} var phone=PhoneFormatter.ToWhatsApp(Convert.ToString(item["client_phone"])); if(phone==null){MessageBox.Show("Telefone do cliente inválido. Edite o cadastro com DDD e número.");return;} DateTime date;var when=DateTime.TryParse(Convert.ToString(item["scheduled_at"]),out date)?date.ToString("dd/MM/yyyy 'às' HH:mm"):Convert.ToString(item["scheduled_at"]);var vehicle=(Convert.ToString(item["vehicle_type"])+" "+Convert.ToString(item["manufacturer"])+" "+Convert.ToString(item["model"])).Trim();var message="Olá, "+Convert.ToString(item["client_name"])+". Lembramos do seu agendamento na empresa para "+when+". Equipamento: "+vehicle+". Serviço previsto: "+Convert.ToString(item["service_description"])+". Aguardamos você!";System.Diagnostics.Process.Start("https://wa.me/"+phone+"?text="+Uri.EscapeDataString(message));
        }
        private void EditWorkOrder() { var id = SelectedId(_workOrders); if (id == null) return; using (var form = new WorkOrderEditor(_database, _workOrders.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void EditWorkOrderItems() { var id = SelectedId(_workOrders); if (id == null) return; var number = Convert.ToString(_workOrders.CurrentRow.Cells["OS"].Value); using (var form = new WorkOrderItemsForm(_database, id.Value, number)) form.ShowDialog(this); RefreshData(); }
        private void AdvanceWorkOrderStatus()
        {
            var id = SelectedId(_workOrders); if (id == null) return;
            var current = Convert.ToString(_workOrders.CurrentRow.Cells["Status"].Value);
            string next = null;
            if (current == "Aprovada") next = "Em manutenção";
            else if (current == "Em manutenção" || current == "Aguardando peça") next = "Serviço concluído";
            else if (current == "Serviço concluído") next = "Aguardando pagamento";
            else if (current == "Pronta para retirada") next = "Finalizada";

            if (next == null)
            {
                if (current == "Aguardando pagamento") MessageBox.Show("Registre o recebimento da OS para liberá-la para retirada.");
                else MessageBox.Show("O status atual não possui avanço automático. Use Editar selecionada para escolher outro status.");
                return;
            }

            var message = next == "Finalizada"
                ? "Confirmar a retirada do equipamento e finalizar esta OS?"
                : "Alterar status da OS de '" + current + "' para '" + next + "'?";
            if (MessageBox.Show(message, "Ordem de serviço", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            _database.UpdateWorkOrderStatus(id.Value, next);
            RefreshData();
            if (next == "Serviço concluído") MessageBox.Show("Serviço concluído. As peças vinculadas foram baixadas do estoque.");
            else if (next == "Aguardando pagamento") MessageBox.Show("OS aguardando pagamento. Use o botão Receber OS para registrar a quitação.");
            else MessageBox.Show("Retirada registrada. A OS foi finalizada.");
        }
        private void ReceiveWorkOrder()
        {
            var id = SelectedId(_workOrders);
            if (id == null) return;

            var status = Convert.ToString(_workOrders.CurrentRow.Cells["Status"].Value);
            if (status != "Serviço concluído" && status != "Aguardando pagamento" && status != "Pronta para retirada")
            {
                MessageBox.Show("Conclua o serviço antes de registrar o recebimento.");
                return;
            }

            using (var form = new WorkOrderPaymentForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    _database.ReceiveWorkOrder(id.Value, form.PaymentMethod);
                    RefreshData();
                    MessageBox.Show("Recebimento registrado. A OS está pronta para retirada.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Recebimento", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        private void EditQuote() { var id = SelectedId(_quotes); if (id == null) return; using (var form = new QuoteWorkspaceForm(_database, id.Value)) form.ShowDialog(this); RefreshData(); }
        private void EditQuoteHeader() { var id = SelectedId(_quotes); if (id == null) return; if (Convert.ToString(_quotes.CurrentRow.Cells["Status"].Value) == "Convertido") { MessageBox.Show("O orçamento convertido não pode mais ser alterado."); return; } using (var form = new QuoteEditor(_database, _quotes.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void ApproveQuote() { var id = SelectedId(_quotes); if (id == null) return; if (Convert.ToString(_quotes.CurrentRow.Cells["Status"].Value) != "Em aberto") { MessageBox.Show("Somente orçamento em aberto pode ser aprovado."); return; } using(var form=new QuoteApprovalForm()){if(form.ShowDialog(this)==DialogResult.OK){_database.ApproveQuote(id.Value,form.Method,form.Notes);RefreshData();MessageBox.Show("Orçamento aprovado e histórico de aceite registrado. Agora ele pode ser convertido em Ordem de Serviço.");}} }
        private void ConvertQuote() { var id = SelectedId(_quotes); if (id == null) return; try { var number = _database.ConvertQuoteToWorkOrder(id.Value); RefreshData(); MessageBox.Show("Orçamento convertido com sucesso na " + number + ". O estoque só será baixado quando a OS for concluída."); } catch (Exception ex) { MessageBox.Show(ex.Message); } }
        private void RejectQuote() { var id = SelectedId(_quotes); if (id == null) return; if (MessageBox.Show("Marcar este orçamento como reprovado?", "Orçamento", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.RejectQuote(id.Value, "Reprovado pelo atendimento"); RefreshData(); } }
        private void OpenDocument() { var id = SelectedId(_workOrders); if (id == null) return; using (var form = new WorkOrderDocumentForm(_database, id.Value)) form.ShowDialog(this); }
        private void EditProduct() { var id = SelectedId(_products); if (id == null) return; using (var form = new ProductEditor(_database, _products.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void EditService() { var id = SelectedId(_services); if (id == null) return; using (var form = new ServiceEditor(_database, _services.CurrentRow)) form.ShowDialog(this); RefreshData(); }
        private void DeleteClient() { var id = SelectedId(_clients); if (id == null) return; if (MessageBox.Show("Excluir o cliente e seus equipamentos?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteClient(id.Value); RefreshData(); } }
        private void DeleteVehicle() { var id = SelectedId(_vehicles); if (id == null) return; if (MessageBox.Show("Excluir o equipamento selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteVehicle(id.Value); RefreshData(); } }
        private void DeleteWorkOrder() { var id = SelectedId(_workOrders); if (id == null) return; if (MessageBox.Show("Excluir a OS selecionada?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteWorkOrder(id.Value); RefreshData(); } }
        private void DeleteProduct() { var id = SelectedId(_products); if (id == null) return; if (MessageBox.Show("Excluir o produto selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteProduct(id.Value); RefreshData(); } }
        private void DeleteService() { var id = SelectedId(_services); if (id == null) return; if (MessageBox.Show("Excluir o serviço selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteService(id.Value); RefreshData(); } }
        private void EditSupplier(){var id=SelectedId(_suppliers);if(id==null)return;using(var f=new SupplierEditor(_database,_suppliers.CurrentRow))f.ShowDialog(this);RefreshData();}
        private void EditDebtor(){var id=SelectedId(_debtors);if(id==null)return;using(var f=new DebtorEditor(_database,_debtors.CurrentRow))f.ShowDialog(this);RefreshData();}
        private void DeleteSupplier(){var id=SelectedId(_suppliers);if(id==null)return;if(MessageBox.Show("Excluir o fornecedor selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)==DialogResult.Yes){_database.DeleteSupplier(id.Value);RefreshData();}}
        private void DeleteDebtor(){var id=SelectedId(_debtors);if(id==null)return;if(MessageBox.Show("Excluir o devedor selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)==DialogResult.Yes){_database.DeleteDebtor(id.Value);RefreshData();}}
        private void CancelSelectedPurchase(){var id=SelectedId(_purchases);if(id==null){MessageBox.Show("Selecione uma compra.");return;}if(MessageBox.Show("Cancelar/devolver a compra selecionada?\n\nO estoque será estornado e as parcelas pendentes serão canceladas. Parcelas já pagas permanecerão registradas no financeiro.","Confirmar devolução",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;try{_database.CancelPurchase(id.Value,"Cancelamento/devolução informado no módulo Compras");RefreshData();MessageBox.Show("Compra cancelada. O estoque e as parcelas pendentes foram estornados.");}catch(Exception ex){MessageBox.Show(ex.Message);}}
        private void EditFinancialCategory(){var id=SelectedId(_financialCategories);if(id==null)return;using(var f=new FinancialCategoryEditor(_database,_financialCategories.CurrentRow))f.ShowDialog(this);RefreshData();}
        private void DeleteFinancialCategory(){var id=SelectedId(_financialCategories);if(id==null)return;if(MessageBox.Show("Excluir a categoria selecionada?", "Confirmar", MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){_database.DeleteFinancialCategory(id.Value);RefreshData();}}
        private void RefreshSupplierSearch(){_suppliers.DataSource=_database.FindSuppliers(_supplierSearch.Text.Trim());ConfigureSupplierGrid();}
        private void RefreshDebtorSearch(){_debtors.DataSource=_database.FindDebtors(_debtorSearch.Text.Trim());ConfigureDebtorGrid();}
        private void RefreshFinancialSearch(){_financial.DataSource=_database.FindFinancialEntries(string.IsNullOrEmpty(_financialStatus.Text)?"Todos":_financialStatus.Text,_financialSearch.Text.Trim());ConfigureFinancialGrid();}
        private void ConfigureFinancialGrid()
        {
            if (_financial.Columns.Count == 0) return;
            _financial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            SetFinancialWidth("Código", 55); SetFinancialWidth("Tipo", 70); SetFinancialWidth("Vencimento", 88); SetFinancialWidth("Status", 78); SetFinancialWidth("Valor", 88); SetFinancialWidth("Categoria", 105); SetFinancialWidth("Documento", 90); SetFinancialWidth("Pagamento", 95);
            if (_financial.Columns.Contains("Origem")) _financial.Columns["Origem"].Visible = false;
            if (_financial.Columns.Contains("Cliente / fornecedor")) _financial.Columns["Cliente / fornecedor"].Width = 175;
            if (_financial.Columns.Contains("Descrição")) _financial.Columns["Descrição"].Width = 240;
        }
        private void SetFinancialWidth(string name, int width) { if (_financial.Columns.Contains(name)) _financial.Columns[name].Width = width; }
        private void MarkFinancialPaid() { var id=SelectedId(_financial); if(id==null)return; var status=Convert.ToString(_financial.CurrentRow.Cells["Status"].Value); if(status=="Pago" || status=="Cancelado") { MessageBox.Show("Esta conta não pode ser baixada."); return; } using(var form=new FinancialSettlementForm(_financial.CurrentRow)){if(form.ShowDialog(this)==DialogResult.OK){try{_database.MarkFinancialEntryPaid(id.Value,form.PaymentMethod,form.PaidAt);RefreshData();}catch(Exception ex){MessageBox.Show(ex.Message,"Financeiro",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}} }
        private void CancelFinancial(){var id=SelectedId(_financial);if(id==null)return;var status=Convert.ToString(_financial.CurrentRow.Cells["Status"].Value);if(status=="Pago"||status=="Cancelado"){MessageBox.Show("Esta conta não pode ser cancelada.");return;}if(MessageBox.Show("Cancelar a conta selecionada?", "Financeiro", MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){_database.CancelFinancialEntry(id.Value);RefreshData();}}
        private static long? SelectedId(DataGridView grid) { if (grid.CurrentRow == null) { MessageBox.Show("Selecione um registro."); return null; } return Convert.ToInt64(grid.CurrentRow.Cells["Código"].Value); }
        private static DataGridView Grid() { return new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BackgroundColor = Color.White, BorderStyle = BorderStyle.None }; }
        private static TabPage Page(string title) { return new TabPage(title) { BackColor = Color.FromArgb(245, 249, 252), Padding = new Padding(12) }; }
        private static TabPage Placeholder(string title, string text) { var p = Page(title); p.Controls.Add(new Label { Text = text, Location = new Point(28, 32), AutoSize = true, ForeColor = Color.FromArgb(53, 73, 94) }); return p; }
        private static Button TopButton(string text, int x, EventHandler click) { var b = new Button { Text = text, Location = new Point(x, 16), Size = new Size(text.Length > 15 ? 150 : 120, 31), BackColor = Color.FromArgb(24, 189, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; b.Click += click; return b; }
    }

    internal sealed class QuoteWorkspaceForm : Form
    {
        private readonly LocalDatabase _database; private readonly long _quoteId; private readonly ComboBox _type=new ComboBox(); private readonly TextBox _search=new TextBox(); private readonly ComboBox _catalog=new ComboBox(); private readonly NumericUpDown _quantity=new NumericUpDown(); private readonly NumericUpDown _price=new NumericUpDown(); private readonly TextBox _manual=new TextBox(); private readonly DataGridView _items=new DataGridView(); private readonly Label _header=new Label(); private readonly Label _total=new Label(); private readonly Label _searchLabel=new Label();
        public QuoteWorkspaceForm(LocalDatabase database,long quoteId){_database=database;_quoteId=quoteId;Text="Orçamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(1000,720);MinimumSize=new Size(820,600);Build();}
        private void Build(){var quote=_database.GetQuoteDocument(_quoteId);if(quote.Rows.Count==0){MessageBox.Show("Orçamento não encontrado.");Close();return;}var q=quote.Rows[0];Text="Orçamento — "+Convert.ToString(q["number"]);_header.Text="ORÇAMENTO "+q["number"]+"    |    "+q["client_name"]+"    |    "+q["vehicle_type"]+" "+q["manufacturer"]+" "+q["model"]+"    |    Validade: "+q["valid_until"]+"    |    Status: "+q["status"];_header.Location=new Point(18,16);_header.AutoSize=true;_header.Font=new Font("Segoe UI",10,FontStyle.Bold);_header.ForeColor=Color.FromArgb(7,55,94);Controls.Add(_header);var print=new Button{Text="Visualizar / imprimir",Location=new Point(18,48),Size=new Size(145,30)};print.Click+=(s,e)=>{using(var f=new QuoteDocumentForm(_database,_quoteId))f.ShowDialog(this);};Controls.Add(print);var whatsapp=new Button{Text="Enviar WhatsApp",Location=new Point(173,48),Size=new Size(135,30)};whatsapp.Click+=(s,e)=>SendWhatsApp();Controls.Add(whatsapp);
            Controls.Add(new Label{Text="Tipo do item",Location=new Point(18,92),AutoSize=true});_type.Items.AddRange(new object[]{"Peça","Serviço","Outro"});_type.Text="Peça";_type.Location=new Point(18,112);_type.Size=new Size(110,24);_type.SelectedIndexChanged+=(s,e)=>ChangeType();Controls.Add(_type);_searchLabel.Location=new Point(140,92);_searchLabel.AutoSize=true;Controls.Add(_searchLabel);_search.Location=new Point(140,112);_search.Size=new Size(260,24);_search.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;SearchCatalog();}};Controls.Add(_search);var find=new Button{Text="Buscar",Location=new Point(410,110),Size=new Size(80,28)};find.Click+=(s,e)=>SearchCatalog();Controls.Add(find);_catalog.Location=new Point(500,112);_catalog.Size=new Size(300,24);_catalog.DisplayMember="display_name";_catalog.ValueMember="id";_catalog.SelectedIndexChanged+=(s,e)=>FillCatalog();Controls.Add(_catalog);_quantity.DecimalPlaces=2;_quantity.Minimum=0.01M;_quantity.Maximum=100000M;_quantity.Value=1;_quantity.Location=new Point(810,112);_quantity.Size=new Size(70,24);Controls.Add(_quantity);_price.DecimalPlaces=2;_price.Maximum=1000000M;_price.Location=new Point(890,112);_price.Size=new Size(85,24);Controls.Add(_price);Controls.Add(new Label{Text="Ou descrição manual",Location=new Point(18,146),AutoSize=true});_manual.Location=new Point(18,166);_manual.Size=new Size(470,24);Controls.Add(_manual);var add=new Button{Text="Adicionar ao orçamento",Location=new Point(500,164),Size=new Size(160,29),BackColor=Color.FromArgb(24,189,200),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};add.Click+=(s,e)=>AddItem();Controls.Add(add);var remove=new Button{Text="Excluir item",Location=new Point(670,164),Size=new Size(105,29)};remove.Click+=(s,e)=>RemoveItem();Controls.Add(remove);var newProduct=new Button{Text="Nova peça",Location=new Point(785,164),Size=new Size(90,29)};newProduct.Click+=(s,e)=>NewProduct();Controls.Add(newProduct);var newService=new Button{Text="Novo serviço",Location=new Point(885,164),Size=new Size(90,29)};newService.Click+=(s,e)=>NewService();Controls.Add(newService);ChangeType();
            _items.Location=new Point(18,205);_items.Size=new Size(955,385);_items.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_items.ReadOnly=true;_items.AllowUserToAddRows=false;_items.AllowUserToDeleteRows=false;_items.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_items.MultiSelect=false;_items.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_items.BackgroundColor=Color.White;Controls.Add(_items);_total.Location=new Point(18,608);_total.AutoSize=true;_total.Font=new Font("Segoe UI",14,FontStyle.Bold);_total.ForeColor=Color.FromArgb(7,55,94);_total.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;Controls.Add(_total);var close=new Button{Text="Fechar",Location=new Point(853,603),Size=new Size(120,34),Anchor=AnchorStyles.Bottom|AnchorStyles.Right};close.Click+=(s,e)=>Close();Controls.Add(close);RefreshItems();}
        private void ChangeType(){_search.Text="";_catalog.DataSource=null;_manual.Text="";_searchLabel.Text=_type.Text=="Peça"?"Buscar peça, código ou código de barras":_type.Text=="Serviço"?"Buscar serviço por nome ou código":"Item manual";_search.Enabled=_type.Text!="Outro";if(_type.Text=="Serviço")SearchCatalog();}
        private void SearchCatalog(){if(_type.Text=="Peça")_catalog.DataSource=_database.FindProductLookup(_search.Text.Trim());else if(_type.Text=="Serviço")_catalog.DataSource=_database.FindServiceLookup(_search.Text.Trim());else{_catalog.DataSource=null;_manual.Focus();return;}_catalog.DisplayMember="display_name";_catalog.ValueMember="id";if(_catalog.Items.Count>0)_catalog.Focus();}
        private void FillCatalog(){var row=_catalog.SelectedItem as DataRowView;if(row==null)return;_manual.Text=Convert.ToString(row["name"]);_price.Value=Convert.ToDecimal(row["sale_price"]);}
        private void AddItem(){var description=_manual.Text.Trim();if(string.IsNullOrWhiteSpace(description)){MessageBox.Show("Pesquise e selecione um item do catálogo ou informe uma descrição manual.");return;}var row=_catalog.SelectedItem as DataRowView;long? productId=_type.Text=="Peça"&&row!=null?(long?)Convert.ToInt64(row["id"]):null;_database.SaveQuoteItem(_quoteId,productId,_type.Text,description,_quantity.Value,_price.Value);_manual.Text="";_search.Text="";_catalog.DataSource=null;_quantity.Value=1;_price.Value=0;RefreshItems();}
        private void RemoveItem(){if(_items.CurrentRow==null){MessageBox.Show("Selecione um item.");return;}if(MessageBox.Show("Excluir o item selecionado?","Confirmar",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes){_database.DeleteQuoteItem(Convert.ToInt64(_items.CurrentRow.Cells["Código"].Value));RefreshItems();}}
        private void NewProduct(){using(var form=new ProductEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK){_type.Text="Peça";SearchCatalog();}}}
        private void NewService(){using(var form=new ServiceEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK){_type.Text="Serviço";SearchCatalog();}}}
        private void RefreshItems(){_items.DataSource=_database.GetQuoteItems(_quoteId);if(_items.Columns["Código"]!=null)_items.Columns["Código"].Visible=false;_total.Text="Total do orçamento: "+_database.GetQuoteSubtotal(_quoteId).ToString("C2");}
        private void SendWhatsApp(){var q=_database.GetQuoteDocument(_quoteId);if(q.Rows.Count==0)return;var row=q.Rows[0];var phone=PhoneFormatter.ToWhatsApp(Convert.ToString(row["client_phone"]));if(phone==null){MessageBox.Show("Telefone do cliente inválido. Edite o cadastro com DDD e número.");return;}var message="Olá, "+Convert.ToString(row["client_name"])+". Segue o orçamento "+Convert.ToString(row["number"])+" no valor de "+_database.GetQuoteSubtotal(_quoteId).ToString("C2")+". Válido até "+Convert.ToString(row["valid_until"])+".";System.Diagnostics.Process.Start("https://wa.me/"+phone+"?text="+Uri.EscapeDataString(message));}
    }

    internal sealed class QuoteDocumentForm : Form
    {
        private readonly LocalDatabase _database;private readonly long _quoteId;private readonly WebBrowser _browser=new WebBrowser();private DataRow _quote;
        public QuoteDocumentForm(LocalDatabase database,long quoteId){_database=database;_quoteId=quoteId;Text="Visualização do orçamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(900,680);MinimumSize=new Size(720,520);Build();}
        private void Build(){var data=_database.GetQuoteDocument(_quoteId);if(data.Rows.Count==0){MessageBox.Show("Orçamento não encontrado.");Close();return;}_quote=data.Rows[0];var print=new Button{Text="Imprimir A4 / PDF",Location=new Point(18,14),Size=new Size(140,32)};print.Click+=(s,e)=>PrintDocument(false);Controls.Add(print);var thermal=new Button{Text="Imprimir cupom",Location=new Point(164,14),Size=new Size(130,32)};thermal.Click+=(s,e)=>PrintDocument(true);Controls.Add(thermal);var whats=new Button{Text="Enviar pelo WhatsApp",Location=new Point(300,14),Size=new Size(160,32)};whats.Click+=SendWhatsApp;Controls.Add(whats);_browser.Location=new Point(18,58);_browser.Size=new Size(845,560);_browser.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_browser.DocumentText=Html();Controls.Add(_browser);}
        private void PrintDocument(bool thermal){if(_browser.Document==null||_browser.Document.Body==null){MessageBox.Show("Aguarde o documento terminar de carregar.");return;}_browser.Document.Body.SetAttribute("class",thermal?"thermal":"a4");_browser.ShowPrintPreviewDialog();}
        private string Html(){var items=_database.GetQuoteItems(_quoteId);var lines="";foreach(DataRow item in items.Rows)lines+="<tr><td>"+H(item["Tipo"])+"</td><td>"+H(item["Descrição"])+"</td><td class='right'>"+H(item["Qtd."])+"</td><td class='right'>"+H(item["Valor unitário"])+"</td><td class='right'>"+H(item["Total"])+"</td></tr>";var approval=string.IsNullOrWhiteSpace(Convert.ToString(_quote["approved_at"]))?"<p>Aprovação do cliente: ___________________________________________</p>":"<h2>Aprovação registrada</h2><p><b>Data/hora:</b> "+H(_quote["approved_at"])+"<br><b>Forma:</b> "+H(_quote["approval_method"])+"<br><b>Observação:</b> "+H(_quote["approval_notes"])+"</p>";return "<html><head><style>@page{size:A4;margin:14mm}body{font-family:Segoe UI,Arial;color:#17304a;margin:30px}body.thermal{width:72mm;margin:4mm auto;font-size:10pt}body.thermal h1{font-size:15pt}body.thermal h2{font-size:11pt}body.thermal table{font-size:9pt}body.thermal th,body.thermal td{padding:3px}body.thermal .total{font-size:12pt}h1{color:#087b94;margin:0}h2{border-bottom:2px solid #20bdc8;padding-bottom:6px;font-size:16px}table{border-collapse:collapse;width:100%;font-size:12px}th,td{border:1px solid #ccd6df;padding:7px;text-align:left}th{background:#eef8fa}.right{text-align:right}.total{font-size:18px;font-weight:bold;text-align:right;margin-top:16px}.muted{color:#52687a}</style></head><body><h1>RMC Flow Comércio Local</h1><p class='muted'>ORÇAMENTO — "+H(_quote["number"])+"</p><h2>Cliente e equipamento</h2><p><b>Cliente:</b> "+H(_quote["client_name"])+"<br><b>Telefone:</b> "+H(_quote["client_phone"])+"<br><b>Equipamento:</b> "+H(_quote["vehicle_type"])+" "+H(_quote["manufacturer"])+" "+H(_quote["model"])+"<br><b>Identificador:</b> "+H(_quote["plate"])+"</p><h2>Condições</h2><p><b>Status:</b> "+H(_quote["status"])+"<br><b>Válido até:</b> "+H(_quote["valid_until"])+"<br><b>Problema relatado:</b> "+H(_quote["complaint"])+"<br><b>Observações:</b> "+H(_quote["notes"])+"</p><h2>Itens</h2><table><tr><th>Tipo</th><th>Descrição</th><th>Qtd.</th><th>Valor unitário</th><th>Total</th></tr>"+lines+"</table><p class='total'>Total: "+_database.GetQuoteSubtotal(_quoteId).ToString("C2")+"</p>"+approval+"</body></html>";}
        private void SendWhatsApp(object sender,EventArgs e){var phone=PhoneFormatter.ToWhatsApp(Convert.ToString(_quote["client_phone"]));if(phone==null){MessageBox.Show("Telefone do cliente inválido. Edite o cadastro com DDD e número.");return;}var message="Olá, "+Convert.ToString(_quote["client_name"])+". Segue o orçamento "+Convert.ToString(_quote["number"])+" no valor de "+_database.GetQuoteSubtotal(_quoteId).ToString("C2")+". Válido até "+Convert.ToString(_quote["valid_until"])+".";System.Diagnostics.Process.Start("https://wa.me/"+phone+"?text="+Uri.EscapeDataString(message));}
        private static string H(object value){return System.Web.HttpUtility.HtmlEncode(Convert.ToString(value)).Replace("\r\n","<br>");}
    }

    internal sealed class WorkOrderPaymentForm : Form
    {
        private readonly ComboBox _payment=new ComboBox(); public string PaymentMethod { get { return _payment.Text; } }
        public WorkOrderPaymentForm(){Text="Receber Ordem de Serviço";StartPosition=FormStartPosition.CenterParent;Size=new Size(430,230);Controls.Add(new Label{Text="Forma de pagamento *",Location=new Point(24,24),AutoSize=true});_payment.Items.AddRange(new object[]{"Dinheiro","PIX","Débito","Crédito","Transferência","Boleto","Outros"});_payment.Text="PIX";_payment.Location=new Point(24,46);_payment.Size=new Size(360,24);Controls.Add(_payment);Controls.Add(new Label{Text="O valor total da OS será lançado como receita paga.",Location=new Point(24,90),AutoSize=true,ForeColor=Color.FromArgb(53,73,94)});Controls.Add(new Label{Text="Dinheiro exige caixa aberto. PIX e cartões ficam apenas no Financeiro.",Location=new Point(24,114),Size=new Size(360,35),ForeColor=Color.FromArgb(53,73,94)});var save=new Button{Text="Confirmar recebimento",Location=new Point(224,158),Size=new Size(160,32)};save.Click+=(s,e)=>DialogResult=DialogResult.OK;Controls.Add(save);}
    }

    internal sealed class QuoteApprovalForm : Form
    {
        private readonly ComboBox _method=new ComboBox(); private readonly TextBox _notes=new TextBox(); public string Method { get { return _method.Text; } } public string Notes { get { return _notes.Text.Trim(); } }
        public QuoteApprovalForm(){Text="Registrar aprovação do orçamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(490,330);Build();}
        private void Build(){Controls.Add(new Label{Text="Forma de aprovação *",Location=new Point(24,24),AutoSize=true});_method.Items.AddRange(new object[]{"Presencial / assinatura","WhatsApp","Telefone","E-mail","Outro"});_method.Text="WhatsApp";_method.Location=new Point(24,46);_method.Size=new Size(420,24);Controls.Add(_method);Controls.Add(new Label{Text="Observação do aceite",Location=new Point(24,88),AutoSize=true});_notes.Location=new Point(24,110);_notes.Size=new Size(420,100);_notes.Multiline=true;_notes.ScrollBars=ScrollBars.Vertical;Controls.Add(_notes);Controls.Add(new Label{Text="A data e a hora atuais serão registradas automaticamente.",Location=new Point(24,220),AutoSize=true,ForeColor=Color.FromArgb(53,73,94)});var save=new Button{Text="Confirmar aprovação",Location=new Point(284,252),Size=new Size(160,32)};save.Click+=(s,e)=>{if(string.IsNullOrWhiteSpace(_method.Text)){MessageBox.Show("Informe a forma de aprovação.");return;}DialogResult=DialogResult.OK;};Controls.Add(save);}
    }

    internal sealed class QuoteItemsForm : Form
    {
        private readonly LocalDatabase _database; private readonly long _quoteId; private readonly DataGridView _items = new DataGridView(); private readonly Label _total = new Label();
        public QuoteItemsForm(LocalDatabase database, long quoteId, string number)
        {
            _database = database; _quoteId = quoteId; Text = "Itens do orçamento — " + number; StartPosition = FormStartPosition.CenterParent; Size = new Size(760, 510); MinimumSize = new Size(680, 440);
            var add = new Button { Text = "Adicionar serviço ou peça", Location = new Point(18, 16), Size = new Size(180, 31) }; add.Click += (s,e) => { using (var f = new QuoteItemEditor(_database, _quoteId)) f.ShowDialog(this); RefreshItems(); }; Controls.Add(add);
            var remove = new Button { Text = "Excluir item", Location = new Point(210,16), Size = new Size(110,31) }; remove.Click += (s,e) => { if (_items.CurrentRow == null) { MessageBox.Show("Selecione um item."); return; } if (MessageBox.Show("Excluir o item selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteQuoteItem(Convert.ToInt64(_items.CurrentRow.Cells["Código"].Value)); RefreshItems(); } }; Controls.Add(remove);
            _items.Location = new Point(18,62); _items.Size = new Size(706,330); _items.Anchor = AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; _items.ReadOnly=true; _items.AllowUserToAddRows=false; _items.AllowUserToDeleteRows=false; _items.SelectionMode=DataGridViewSelectionMode.FullRowSelect; _items.MultiSelect=false; _items.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; _items.BackgroundColor=Color.White; Controls.Add(_items);
            _total.Location=new Point(18,410);_total.AutoSize=true;_total.Font=new Font("Segoe UI",13,FontStyle.Bold);_total.ForeColor=Color.FromArgb(7,55,94);_total.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;Controls.Add(_total);RefreshItems();
        }
        private void RefreshItems(){_items.DataSource=_database.GetQuoteItems(_quoteId);if(_items.Columns["Código"]!=null)_items.Columns["Código"].Visible=false;_total.Text="Subtotal do orçamento: "+_database.GetQuoteSubtotal(_quoteId).ToString("C2");}
    }

    internal sealed class QuoteItemEditor : Form
    {
        private readonly LocalDatabase _database; private readonly long _quoteId; private readonly ComboBox _type=new ComboBox(); private readonly ComboBox _catalog=new ComboBox(); private readonly TextBox _description=new TextBox(); private readonly NumericUpDown _quantity=new NumericUpDown(); private readonly NumericUpDown _price=new NumericUpDown();
        public QuoteItemEditor(LocalDatabase database,long quoteId){_database=database;_quoteId=quoteId;Text="Adicionar item ao orçamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(470,400);MinimumSize=new Size(470,400);Build();}
        private void Build(){_type.Items.AddRange(new object[]{"Serviço","Peça","Outro"});_type.Text="Serviço";_type.SelectedIndexChanged+=(s,e)=>LoadCatalog();_catalog.DisplayMember="display_name";_catalog.ValueMember="id";_catalog.SelectedIndexChanged+=(s,e)=>FillCatalog();LookupControls.EnableTyping(_catalog);_quantity.DecimalPlaces=2;_quantity.Minimum=0.01M;_quantity.Maximum=100000M;_quantity.Value=1;_price.DecimalPlaces=2;_price.Maximum=1000000M;_price.ThousandsSeparator=true;Add("Tipo *",_type,18);Add("Catálogo (opcional)",_catalog,70);_catalog.Size=new Size(270,24);var search=new Button{Text="Pesquisar",Location=new Point(305,88),Size=new Size(115,27)};search.Click+=(s,e)=>SearchCatalog();Controls.Add(search);Add("Descrição *",_description,122);Add("Quantidade *",_quantity,174);Add("Valor unitário (R$) *",_price,226);LoadCatalog();var save=new Button{Text="Adicionar",Location=new Point(300,310),Size=new Size(120,34)};save.Click+=Save;Controls.Add(save);}
        private void LoadCatalog(){if(_type.Text=="Peça"){_catalog.DataSource=_database.GetProductLookup();_catalog.Enabled=true;}else if(_type.Text=="Serviço"){_catalog.DataSource=_database.GetServiceLookup();_catalog.Enabled=true;}else{_catalog.DataSource=null;_catalog.Enabled=false;}}
        private void FillCatalog(){var row=_catalog.SelectedItem as DataRowView;if(row==null)return;_description.Text=Convert.ToString(row["name"]);_price.Value=Convert.ToDecimal(row["sale_price"]);}
        private void SearchCatalog(){if(_type.Text=="Outro"){MessageBox.Show("Para item manual, informe a descrição.");return;}if(_type.Text=="Peça"){using(var f=new ProductSearchForm(_database)){if(f.ShowDialog(this)==DialogResult.OK){LoadCatalog();_catalog.SelectedValue=f.SelectedId;}}}else{using(var f=new ServiceSearchForm(_database)){if(f.ShowDialog(this)==DialogResult.OK){LoadCatalog();_catalog.SelectedValue=f.SelectedId;}}}}
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(25,y),AutoSize=true});control.Location=new Point(25,y+20);control.Size=new Size(395,24);Controls.Add(control);}
        private void Save(object sender,EventArgs e){if(string.IsNullOrWhiteSpace(_description.Text)){MessageBox.Show("Informe a descrição do item.");return;}var selected=_catalog.SelectedItem as DataRowView;long? productId=_type.Text=="Peça"&&selected!=null?(long?)Convert.ToInt64(selected["id"]):null;_database.SaveQuoteItem(_quoteId,productId,_type.Text,_description.Text.Trim(),_quantity.Value,_price.Value);DialogResult=DialogResult.OK;}
    }

    internal sealed class QuoteEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row; private readonly ComboBox _client=new ComboBox(); private readonly ComboBox _vehicle=new ComboBox(); private readonly DateTimePicker _validity=new DateTimePicker(); private readonly TextBox _complaint=new TextBox(); private readonly TextBox _notes=new TextBox();
        public QuoteEditor(LocalDatabase database,DataGridViewRow row){_database=database;_row=row;Text=row==null?"Novo orçamento":"Editar orçamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(620,510);MinimumSize=new Size(620,510);Build();}
        private void Build(){_client.DataSource=_database.GetClientLookup();_client.DisplayMember="name";_client.ValueMember="id";LookupControls.EnableTyping(_client);_client.SelectedIndexChanged+=(s,e)=>LoadVehicles();_vehicle.DisplayMember="display_name";_vehicle.ValueMember="id";LookupControls.EnableTyping(_vehicle);_validity.Format=DateTimePickerFormat.Short;_validity.Value=DateTime.Today.AddDays(10);Add("Cliente *",_client,18);_client.Size=new Size(320,24);var findClient=new Button{Text="Pesquisar cliente",Location=new Point(355,37),Size=new Size(120,27)};findClient.Click+=(s,e)=>SearchClient();Controls.Add(findClient);var newClient=new Button{Text="Novo cliente",Location=new Point(485,37),Size=new Size(95,27)};newClient.Click+=(s,e)=>NewClient();Controls.Add(newClient);Add("Equipamento *",_vehicle,72);_vehicle.Size=new Size(370,24);var newVehicle=new Button{Text="Novo equipamento / equipamento",Location=new Point(420,91),Size=new Size(160,27)};newVehicle.Click+=(s,e)=>NewVehicle();Controls.Add(newVehicle);Add("Validade do orçamento",_validity,126);AddLong("Problema relatado",_complaint,180);AddLong("Observações",_notes,272);if(_row!=null){_client.SelectedValue=Convert.ToInt64(_row.Cells["client_id"].Value);LoadVehicles();_vehicle.SelectedValue=Convert.ToInt64(_row.Cells["vehicle_id"].Value);DateTime date;if(DateTime.TryParse(Convert.ToString(_row.Cells["Validade"].Value),out date))_validity.Value=date;_complaint.Text=Convert.ToString(_row.Cells["reclamacao"].Value);_notes.Text=Convert.ToString(_row.Cells["observacoes"].Value);}else LoadVehicles();var save=new Button{Text="Salvar e abrir itens",Location=new Point(390,402),Size=new Size(180,34)};save.Click+=Save;Controls.Add(save);}
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(22,y),AutoSize=true});control.Location=new Point(22,y+19);control.Size=new Size(548,24);Controls.Add(control);}
        private void AddLong(string label,TextBox text,int y){Controls.Add(new Label{Text=label,Location=new Point(22,y),AutoSize=true});text.Location=new Point(22,y+19);text.Size=new Size(548,54);text.Multiline=true;text.ScrollBars=ScrollBars.Vertical;Controls.Add(text);}
        private void LoadVehicles(){long clientId;try{clientId=Convert.ToInt64(_client.SelectedValue);}catch{_vehicle.DataSource=null;return;}if(clientId<=0){_vehicle.DataSource=null;return;}_vehicle.DataSource=_database.GetVehicleLookupByClient(clientId);_vehicle.DisplayMember="display_name";_vehicle.ValueMember="id";}
        private void SearchClient(){using(var form=new ClientSearchForm(_database)){if(form.ShowDialog(this)==DialogResult.OK){_client.DataSource=_database.GetClientLookup();_client.DisplayMember="name";_client.ValueMember="id";_client.SelectedValue=form.SelectedId;LoadVehicles();}}}
        private void NewClient(){using(var form=new ClientEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK){_client.DataSource=_database.GetClientLookup();_client.DisplayMember="name";_client.ValueMember="id";}}}
        private void NewVehicle(){if(_client.SelectedValue==null){MessageBox.Show("Selecione primeiro o cliente.");return;}using(var form=new VehicleEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK)LoadVehicles();}}
        private void Save(object sender,EventArgs e){if(_client.SelectedValue==null||_vehicle.SelectedValue==null){MessageBox.Show("Informe cliente e equipamento.");return;}if(!_database.VehicleBelongsToClient(Convert.ToInt64(_vehicle.SelectedValue),Convert.ToInt64(_client.SelectedValue))){MessageBox.Show("O equipamento selecionado não pertence ao cliente escolhido.");return;}var id=_row==null?(long?)null:Convert.ToInt64(_row.Cells["Código"].Value);var number=_row==null?_database.NextQuoteNumber():Convert.ToString(_row.Cells["Orçamento"].Value);_database.SaveQuote(id,number,Convert.ToInt64(_client.SelectedValue),Convert.ToInt64(_vehicle.SelectedValue),_validity.Value.ToString("dd/MM/yyyy"),_complaint.Text.Trim(),_notes.Text.Trim());var quoteId=id??_database.GetQuoteId(number);using(var form=new QuoteWorkspaceForm(_database,quoteId))form.ShowDialog(this);DialogResult=DialogResult.OK;}
    }

    internal sealed class WorkOrderItemsForm : Form
    {
        private readonly LocalDatabase _database; private readonly long _workOrderId; private readonly DataGridView _items = new DataGridView(); private readonly Label _total = new Label();
        public WorkOrderItemsForm(LocalDatabase database, long workOrderId, string number)
        {
            _database = database; _workOrderId = workOrderId; Text = "Itens e orçamento — " + number; StartPosition = FormStartPosition.CenterParent; Size = new Size(760, 510); MinimumSize = new Size(680, 440);
            var newItem = new Button { Text = "Adicionar serviço ou peça", Location = new Point(18, 16), Size = new Size(180, 31) }; newItem.Click += (s, e) => { using (var editor = new WorkOrderItemEditor(_database, _workOrderId)) editor.ShowDialog(this); RefreshItems(); }; Controls.Add(newItem);
            var remove = new Button { Text = "Excluir item", Location = new Point(210, 16), Size = new Size(110, 31) }; remove.Click += DeleteSelected; Controls.Add(remove);
            _items.Location = new Point(18, 62); _items.Size = new Size(706, 330); _items.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _items.ReadOnly = true; _items.AllowUserToAddRows = false; _items.AllowUserToDeleteRows = false; _items.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _items.MultiSelect = false; _items.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _items.BackgroundColor = Color.White; Controls.Add(_items);
            _total.Location = new Point(18, 410); _total.AutoSize = true; _total.Font = new Font("Segoe UI", 13, FontStyle.Bold); _total.ForeColor = Color.FromArgb(7, 55, 94); _total.Anchor = AnchorStyles.Bottom | AnchorStyles.Left; Controls.Add(_total); RefreshItems();
        }
        private void RefreshItems() { _items.DataSource = _database.GetWorkOrderItems(_workOrderId); if (_items.Columns["Código"] != null) _items.Columns["Código"].Visible = false; _total.Text = "Subtotal da OS: " + _database.GetWorkOrderSubtotal(_workOrderId).ToString("C2"); }
        private void DeleteSelected(object sender, EventArgs e) { if (_items.CurrentRow == null) { MessageBox.Show("Selecione um item."); return; } if (MessageBox.Show("Excluir o item selecionado?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.DeleteWorkOrderItem(Convert.ToInt64(_items.CurrentRow.Cells["Código"].Value)); RefreshItems(); } }
    }

    internal sealed class WorkOrderDocumentForm : Form
    {
        private readonly LocalDatabase _database; private readonly long _workOrderId; private readonly WebBrowser _preview = new WebBrowser(); private DataRow _order;
        public WorkOrderDocumentForm(LocalDatabase database, long workOrderId)
        {
            _database = database; _workOrderId = workOrderId; Text = "Documento da Ordem de Serviço"; StartPosition = FormStartPosition.CenterParent; Size = new Size(900, 680); MinimumSize = new Size(720, 520); Build();
        }
        private void Build()
        {
            var data = _database.GetWorkOrderDocument(_workOrderId); if (data.Rows.Count == 0) { MessageBox.Show("OS não encontrada."); Close(); return; } _order = data.Rows[0];
            var print = new Button { Text = "Imprimir A4 / PDF", Location = new Point(18, 14), Size = new Size(140, 32) }; print.Click += (s,e) => PrintDocument(false); Controls.Add(print);
            var thermal = new Button { Text = "Imprimir cupom", Location = new Point(164, 14), Size = new Size(130, 32) }; thermal.Click += (s,e) => PrintDocument(true); Controls.Add(thermal);
            var whatsapp = new Button { Text = "Enviar pelo WhatsApp", Location = new Point(300, 14), Size = new Size(160, 32) }; whatsapp.Click += SendWhatsApp; Controls.Add(whatsapp);
            _preview.Location = new Point(18, 58); _preview.Size = new Size(845, 560); _preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _preview.DocumentText = BuildHtml(); Controls.Add(_preview);
        }
        private void PrintDocument(bool thermal){if(_preview.Document==null||_preview.Document.Body==null){MessageBox.Show("Aguarde o documento terminar de carregar.");return;}_preview.Document.Body.SetAttribute("class",thermal?"thermal":"a4");_preview.ShowPrintPreviewDialog();}
        private string BuildHtml()
        {
            var items = _database.GetWorkOrderItems(_workOrderId); decimal subtotal = _database.GetWorkOrderSubtotal(_workOrderId); var lines = "";
            foreach (DataRow item in items.Rows) lines += "<tr><td>" + H(item["Tipo"]) + "</td><td>" + H(item["Descrição"]) + "</td><td class='right'>" + H(item["Qtd."]) + "</td><td class='right'>" + H(item["Valor unitário"]) + "</td><td class='right'>" + H(item["Total"]) + "</td></tr>";
            var origin=string.IsNullOrWhiteSpace(Convert.ToString(_order["quote_number"]))?"":"<h2>Origem e aprovação</h2><p><b>Orçamento de origem:</b> "+H(_order["quote_number"])+"<br><b>Histórico de aprovação:</b> "+H(_order["approval_history"])+"</p>";
            var payment=string.IsNullOrWhiteSpace(Convert.ToString(_order["paid_at"]))?"<h2>Pagamento</h2><p><b>Situação:</b> Pendente</p>":"<h2>Pagamento</h2><p><b>Situação:</b> Quitada<br><b>Forma:</b> "+H(_order["payment_method"])+"<br><b>Data/hora:</b> "+H(_order["paid_at"])+"</p>";
            var warrantyDays=Convert.ToInt32(_order["warranty_days"]);var warranty="";if(warrantyDays>0){DateTime baseDate;var baseText=Convert.ToString(_order["finalized_at"]);if(!DateTime.TryParse(baseText,out baseDate))DateTime.TryParse(Convert.ToString(_order["opened_at"]),out baseDate);warranty="<h2>Garantia</h2><p><b>Prazo:</b> "+warrantyDays+" dia(s)<br><b>Válida até:</b> "+H(baseDate.AddDays(warrantyDays).ToString("dd/MM/yyyy"))+"</p>";}
            var workshopName=_database.Get("company_trade_name");if(string.IsNullOrWhiteSpace(workshopName))workshopName="Sua Empresa";var workshopDetails="";var document=_database.Get("company_document");var phone=_database.Get("company_phone");var address=_database.Get("company_address");if(!string.IsNullOrWhiteSpace(document))workshopDetails+="CNPJ/CPF: "+H(document)+" &nbsp; ";if(!string.IsNullOrWhiteSpace(phone))workshopDetails+="Telefone: "+H(phone)+"<br>";if(!string.IsNullOrWhiteSpace(address))workshopDetails+=H(address);
            return "<html><head><style>@page{size:A4;margin:14mm}body{font-family:Segoe UI,Arial;color:#17304a;margin:30px}body.thermal{width:72mm;margin:4mm auto;font-size:10pt}body.thermal h1{font-size:15pt}body.thermal h2{font-size:11pt}body.thermal table{font-size:9pt}body.thermal th,body.thermal td{padding:3px}body.thermal .total{font-size:12pt}h1{color:#087b94;margin:0}h2{border-bottom:2px solid #20bdc8;padding-bottom:6px;font-size:16px}table{border-collapse:collapse;width:100%;font-size:12px}th,td{border:1px solid #ccd6df;padding:7px;text-align:left}th{background:#eef8fa}.right{text-align:right}.total{font-size:18px;font-weight:bold;text-align:right;margin-top:16px}.muted{color:#52687a}</style></head><body><h1>"+H(workshopName)+"</h1><p class='muted'>"+workshopDetails+"</p><p class='muted'>ORDEM DE SERVIÇO / ORÇAMENTO — " + H(_order["number"]) + "</p><h2>Cliente e equipamento</h2><p><b>Cliente:</b> " + H(_order["client_name"]) + "<br><b>Telefone:</b> " + H(_order["client_phone"]) + "<br><b>Equipamento:</b> " + H(_order["vehicle_type"]) + " " + H(_order["manufacturer"]) + " " + H(_order["model"]) + "<br><b>Identificador:</b> " + H(_order["plate"]) + " &nbsp; <b>KM:</b> " + H(_order["mileage"]) + "</p><h2>Atendimento</h2><p><b>Status:</b> " + H(_order["status"]) + "<br><b>Previsão:</b> " + H(_order["expected_at"]) + "<br><b>Problema relatado:</b> " + H(_order["complaint"]) + "<br><b>Diagnóstico:</b> " + H(_order["diagnosis"]) + "<br><b>Observações:</b> " + H(_order["notes"]) + "</p>"+payment+warranty+origin+"<h2>Itens</h2><table><tr><th>Tipo</th><th>Descrição</th><th>Qtd.</th><th>Valor unitário</th><th>Total</th></tr>" + lines + "</table><p class='total'>Total: " + subtotal.ToString("C2") + "</p><br><br><p>Assinatura do cliente: ___________________________________________</p></body></html>";
        }
        private void SendWhatsApp(object sender, EventArgs e)
        {
            var phone = PhoneFormatter.ToWhatsApp(Convert.ToString(_order["client_phone"])); if (phone == null) { MessageBox.Show("Telefone do cliente inválido. Edite o cadastro com DDD e número."); return; }
            var message = "Olá, " + Convert.ToString(_order["client_name"]) + ". Segue o orçamento/OS " + Convert.ToString(_order["number"]) + " no valor de " + _database.GetWorkOrderSubtotal(_workOrderId).ToString("C2") + ".";
            System.Diagnostics.Process.Start("https://wa.me/" + phone + "?text=" + Uri.EscapeDataString(message));
        }
        private static string H(object value) { return System.Web.HttpUtility.HtmlEncode(Convert.ToString(value)).Replace("\r\n", "<br>"); }
    }

    internal sealed class WorkOrderItemEditor : Form
    {
        private readonly LocalDatabase _database; private readonly long _workOrderId; private readonly ComboBox _type = new ComboBox(); private readonly ComboBox _catalog = new ComboBox(); private readonly TextBox _description = new TextBox(); private readonly NumericUpDown _quantity = new NumericUpDown(); private readonly NumericUpDown _price = new NumericUpDown();
        public WorkOrderItemEditor(LocalDatabase database, long workOrderId) { _database = database; _workOrderId = workOrderId; Text = "Adicionar item à OS"; StartPosition = FormStartPosition.CenterParent; Size = new Size(470, 450); MinimumSize = new Size(470, 450); Build(); }
        private void Build()
        {
            _type.Items.AddRange(new object[] { "Serviço", "Peça", "Outro" }); _type.Text = "Serviço"; _type.SelectedIndexChanged += (s,e) => LoadCatalog(); _catalog.DisplayMember = "display_name"; _catalog.ValueMember = "id"; _catalog.SelectedIndexChanged += (s,e) => FillSelectedCatalog(); _quantity.DecimalPlaces = 2; _quantity.Minimum = 0.01M; _quantity.Maximum = 100000M; _quantity.Value = 1; _price.DecimalPlaces = 2; _price.Maximum = 1000000M; _price.ThousandsSeparator = true;
            LookupControls.EnableTyping(_catalog); Add("Tipo *", _type, 18); Add("Catálogo (opcional)", _catalog, 70); _catalog.Size=new Size(270,24); var search=new Button{Text="Pesquisar",Location=new Point(305,88),Size=new Size(115,27)};search.Click+=(s,e)=>SearchCatalog();Controls.Add(search); Add("Descrição *", _description, 122); Add("Quantidade *", _quantity, 174); Add("Valor unitário (R$) *", _price, 226); LoadCatalog();
            var newService = new Button { Text = "Novo serviço", Location = new Point(25, 278), Size = new Size(145, 32) }; newService.Click += (s, e) => NewServiceForWorkOrder(); Controls.Add(newService);
            var save = new Button { Text = "Adicionar", Location = new Point(300, 355), Size = new Size(120, 34), Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; save.Click += Save; Controls.Add(save);
        }
        private void LoadCatalog() { if (_type.Text == "Peça") { _catalog.DataSource = _database.GetProductLookup(); _catalog.Enabled = true; } else if (_type.Text == "Serviço") { _catalog.DataSource = _database.GetServiceLookup(); _catalog.Enabled = true; } else { _catalog.DataSource = null; _catalog.Enabled = false; } }
        private void FillSelectedCatalog() { var row = _catalog.SelectedItem as DataRowView; if (row == null) return; _description.Text = Convert.ToString(row["name"]); _price.Value = Convert.ToDecimal(row["sale_price"]); }
        private void SearchCatalog() { if (_type.Text == "Outro") { MessageBox.Show("Para item manual, informe a descrição."); return; } if (_type.Text == "Peça") { using (var f = new ProductSearchForm(_database)) { if (f.ShowDialog(this) == DialogResult.OK) { LoadCatalog(); _catalog.SelectedValue = f.SelectedId; } } } else { using (var f = new ServiceSearchForm(_database)) { if (f.ShowDialog(this) == DialogResult.OK) { LoadCatalog(); _catalog.SelectedValue = f.SelectedId; } } } }
        private void NewServiceForWorkOrder() { if (_type.Text != "Serviço") { MessageBox.Show("Selecione o tipo Serviço para cadastrar um novo serviço."); return; } using (var form = new ServiceEditor(_database, null)) { if (form.ShowDialog(this) == DialogResult.OK) { LoadCatalog(); _catalog.SelectedValue = form.SavedId; } } }
        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(25, y), AutoSize = true }); control.Location = new Point(25, y + 20); control.Size = new Size(395, 24); Controls.Add(control); }
        private void Save(object sender, EventArgs e) { if (string.IsNullOrWhiteSpace(_description.Text)) { MessageBox.Show("Informe a descrição do item."); return; } var selected = _catalog.SelectedItem as DataRowView; long? productId = _type.Text == "Peça" && selected != null ? (long?)Convert.ToInt64(selected["id"]) : null; _database.SaveWorkOrderItem(_workOrderId, productId, _type.Text, _description.Text.Trim(), _quantity.Value, _price.Value); DialogResult = DialogResult.OK; }
    }

    internal sealed class ProductEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row; private readonly TextBox _code = new TextBox(); private readonly TextBox _barcode = new TextBox(); private readonly TextBox _name = new TextBox(); private readonly ComboBox _category = new ComboBox(); private readonly TextBox _unit = new TextBox(); private readonly TextBox _shelf = new TextBox(); private readonly TextBox _location = new TextBox(); private readonly NumericUpDown _price = new NumericUpDown(); private readonly NumericUpDown _minimum = new NumericUpDown(); private readonly NumericUpDown _initialStock = new NumericUpDown(); private readonly CheckBox _active = new CheckBox { Text = "Produto ativo", Checked = true, AutoSize = true };
        private readonly PricingFields _pricing = new PricingFields();
        public ProductEditor(LocalDatabase database, DataGridViewRow row, bool showPricing = false) { _database = database; _row = row; Text = row == null ? "Novo produto / peça" : "Editar produto / peça"; StartPosition = FormStartPosition.CenterParent; Size = new Size(500, 660); Build(); IntegratePricing(showPricing); }
        private void Build() { _price.DecimalPlaces = 2; _price.Maximum = 1000000M; _price.ThousandsSeparator = true; _minimum.DecimalPlaces = 2; _minimum.Maximum = 1000000M; _initialStock.DecimalPlaces=2; _initialStock.Maximum=1000000M; _unit.Text = "UN"; _category.DisplayMember="display_name";_category.ValueMember="id";LookupControls.EnableTyping(_category);_category.DataSource=_database.GetProductCategoryLookup(); Add("Código interno", _code, 18); Add("Código de barras", _barcode, 70); Add("Nome do produto / peça *", _name, 122); Add("Categoria *", _category, 174);_category.Size=new Size(275,24);var newCategory=new Button{Text="Nova categoria",Location=new Point(310,194),Size=new Size(145,27)};newCategory.Click+=(s,e)=>NewCategory();Controls.Add(newCategory); Add("Unidade", _unit, 226); Add("Prateleira", _shelf, 278); Add("Localização na prateleira", _location, 330); Add("Preço de venda (R$)", _price, 382); Add("Quantidade inicial em estoque", _initialStock, 434); Add("Estoque mínimo", _minimum, 486); _active.Location = new Point(25, 538); Controls.Add(_active); if (_row != null) { _code.Text = Convert.ToString(_row.Cells["Código interno"].Value); _barcode.Text = Convert.ToString(_row.Cells["Código de barras"].Value); _name.Text = Convert.ToString(_row.Cells["Produto / peça"].Value); _category.Text = Convert.ToString(_row.Cells["Categoria"].Value); _unit.Text = Convert.ToString(_row.Cells["Unidade"].Value); _shelf.Text=Convert.ToString(_row.Cells["Prateleira"].Value);_location.Text=Convert.ToString(_row.Cells["Localização"].Value); _price.Value = ParseMoney(_row.Cells["Preço de venda"].Value); _minimum.Value = ParseMoney(_row.Cells["Estoque mínimo"].Value); _initialStock.Value=ParseMoney(_row.Cells["Em estoque"].Value); _initialStock.Enabled=false; _active.Checked = Convert.ToString(_row.Cells["Status"].Value) == "Ativo"; } var save = new Button { Text = "Salvar", Location = new Point(335, 545), Size = new Size(120,32) }; save.Click += Save; Controls.Add(save); }
        private void IntegratePricing(bool showPricing)
        {
            Text=_row==null?"Novo produto":"Editar produto";
            Size=new Size(650,740);MinimumSize=new Size(590,620);
            var tabs=new TabControl{Dock=DockStyle.Fill};
            var data=new TabPage("Dados do produto"){AutoScroll=true};
            var costs=new TabPage("Custos e preço"){AutoScroll=true};
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=60,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(12,10,12,8)};
            var original=new Control[Controls.Count];Controls.CopyTo(original,0);
            foreach(var control in original) {
                var button=control as Button;
                if(button!=null && button.Text=="Salvar") {button.Anchor=AnchorStyles.None;button.AutoSize=false;button.Size=new Size(150,34);button.Margin=new Padding(4,0,4,0);footer.Controls.Add(button);AcceptButton=button;}
                else data.Controls.Add(control);
            }
            // The existing sale price control remains the only final-price value.
            costs.Controls.Add(_pricing);_pricing.Location=new Point(12,12);
            costs.Controls.Add(new Label{Text="Preço de venda escolhido (R$)",Location=new Point(20,500),AutoSize=true});
            costs.Controls.Add(_price);_price.Location=new Point(20,525);_price.Size=new Size(220,26);
            foreach(Control control in data.Controls) if(control is Label && control.Text=="Preço de venda (R$)")control.Visible=false;
            _pricing.LoadValues(_row==null?new decimal[9]:_database.GetPricing(Convert.ToInt64(_row.Cells["Código"].Value)));
            _pricing.ApplyPrice += (sender,e)=>{if(_pricing.SuggestedPrice>_price.Maximum){MessageBox.Show("O preço sugerido ultrapassa o limite do cadastro.");return;}_price.Value=_pricing.SuggestedPrice;};
            tabs.TabPages.Add(data);tabs.TabPages.Add(costs);Controls.Add(tabs);Controls.Add(footer);tabs.BringToFront();tabs.SelectedIndex=showPricing?1:0;
        }
        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(25,y), AutoSize = true }); control.Location = new Point(25,y+20); control.Size = new Size(430,24); Controls.Add(control); }
        private void NewCategory(){using(var f=new ProductCategoryEditor(_database)){if(f.ShowDialog(this)==DialogResult.OK){_category.DataSource=_database.GetProductCategoryLookup();_category.DisplayMember="display_name";_category.ValueMember="id";}}}
        private void Save(object sender, EventArgs e) { if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Informe o nome do produto."); return; } if(_category.SelectedValue==null){MessageBox.Show("Cadastre e selecione uma categoria.");return;}try { var category=_category.SelectedItem as DataRowView; _database.SaveProduct(_row == null ? (long?)null : Convert.ToInt64(_row.Cells["Código"].Value), _code.Text.Trim(), _barcode.Text.Trim(), _name.Text.Trim(), category==null?_category.Text:Convert.ToString(category["name"]), _unit.Text.Trim(), _shelf.Text.Trim(), _location.Text.Trim(), _price.Value, _minimum.Value, _active.Checked, _pricing.Values, _initialStock.Value); DialogResult = DialogResult.OK; } catch(Exception ex) { MessageBox.Show(ex.Message); } }
        private static decimal ParseMoney(object value) { decimal result; decimal.TryParse(Convert.ToString(value).Replace("R$", "").Trim(), out result); return result; }
    }

    internal sealed class ProductCategoryEditor : Form
    {
        private readonly LocalDatabase _database; private readonly TextBox _name = new TextBox(); private readonly CheckBox _active = new CheckBox { Text = "Categoria ativa", Checked = true, AutoSize = true };
        public ProductCategoryEditor(LocalDatabase database) { _database = database; Text = "Nova categoria de produto"; StartPosition = FormStartPosition.CenterParent; Size = new Size(450, 240); Build(); }
        private void Build() { Controls.Add(new Label { Text = "Nome da categoria *", Location = new Point(25, 24), AutoSize = true }); _name.Location = new Point(25, 46); _name.Size = new Size(380, 24); Controls.Add(_name); _active.Location = new Point(25, 88); Controls.Add(_active); var save = new Button { Text = "Salvar", Location = new Point(285, 135), Size = new Size(120, 32) }; save.Click += Save; Controls.Add(save); }
        private void Save(object sender, EventArgs e) { if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Informe o nome da categoria."); return; } try { _database.SaveProductCategory(null, _name.Text.Trim(), _active.Checked); DialogResult = DialogResult.OK; } catch (Exception) { MessageBox.Show("Já existe uma categoria com este nome."); } }
    }

    internal sealed class ServiceEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row; private readonly TextBox _code = new TextBox(); private readonly TextBox _name = new TextBox(); private readonly ComboBox _category = new ComboBox(); private readonly TextBox _duration = new TextBox(); private readonly NumericUpDown _price = new NumericUpDown(); private readonly CheckBox _active = new CheckBox { Text = "Serviço ativo", Checked = true, AutoSize = true };
        private readonly PricingFields _pricing = new PricingFields(true);
        public ServiceEditor(LocalDatabase database, DataGridViewRow row) { _database = database; _row = row; Text = row == null ? "Novo serviço" : "Editar serviço"; StartPosition = FormStartPosition.CenterParent; Size = new Size(500, 450); Build(); IntegratePricing(); }
        private void Build() { _price.DecimalPlaces = 2; _price.Maximum = 1000000M; _price.ThousandsSeparator = true; Add("Código interno", _code, 18); Add("Nome do serviço *", _name, 70); _category.DropDownStyle=ComboBoxStyle.DropDownList;LoadCategories(null);Add("Categoria", _category, 122);_category.Width=275;var addCategory=new Button{Text="Nova categoria",Location=new Point(310,142),Size=new Size(145,27)};addCategory.Click+=(sender,e)=>NewServiceCategory();Controls.Add(addCategory); Add("Duração estimada", _duration, 174); Add("Preço padrão (R$)", _price, 226); _active.Location = new Point(25, 284); Controls.Add(_active); if (_row != null) { _code.Text = Convert.ToString(_row.Cells["Código interno"].Value); _name.Text = Convert.ToString(_row.Cells["Serviço"].Value); _category.Text = Convert.ToString(_row.Cells["Categoria"].Value); _duration.Text = Convert.ToString(_row.Cells["Duração estimada"].Value); _price.Value = ParseMoney(_row.Cells["Preço padrão"].Value); _active.Checked = Convert.ToString(_row.Cells["Status"].Value) == "Ativo"; } var save = new Button { Text = "Salvar", Location = new Point(335, 344), Size = new Size(120,32) }; save.Click += Save; Controls.Add(save); }
        private void LoadCategories(string selected){_category.Items.Clear();_category.Items.Add("");foreach(DataRow row in _database.GetServiceCategories().Rows)_category.Items.Add(Convert.ToString(row["name"]));_category.SelectedIndex=0;if(selected!=null)foreach(var item in _category.Items)if(string.Equals(Convert.ToString(item),selected,StringComparison.OrdinalIgnoreCase)){_category.SelectedItem=item;break;}}
        private void NewServiceCategory()
        {
            using(var dialog=new Form{Text="Nova categoria de serviço",Size=new Size(420,180),StartPosition=FormStartPosition.CenterParent}){
                var input=new TextBox{Location=new Point(20,30),Width=360};var save=new Button{Text="Salvar categoria",Location=new Point(225,75),Size=new Size(155,32)};dialog.Controls.Add(new Label{Text="Nome da categoria",Location=new Point(20,10),AutoSize=true});dialog.Controls.Add(input);dialog.Controls.Add(save);dialog.AcceptButton=save;
                save.Click+=(sender,e)=>{try{_database.SaveServiceCategory(input.Text);LoadCategories(input.Text.Trim());dialog.DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(ex.Message);}};dialog.ShowDialog(this);
            }
        }
        private void IntegratePricing()
        {
            Size=new Size(650,740);MinimumSize=new Size(590,620);
            var tabs=new TabControl{Dock=DockStyle.Fill};var data=new TabPage("Dados do serviço"){AutoScroll=true};var costs=new TabPage("Custos e preço"){AutoScroll=true};
            var footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=60,FlowDirection=FlowDirection.RightToLeft,WrapContents=false,Padding=new Padding(12,10,12,8)};
            var original=new Control[Controls.Count];Controls.CopyTo(original,0);
            foreach(var control in original){var button=control as Button;if(button!=null&&button.Text=="Salvar"){button.Anchor=AnchorStyles.None;button.Size=new Size(150,34);button.Margin=new Padding(4,0,4,0);footer.Controls.Add(button);AcceptButton=button;}else data.Controls.Add(control);}
            foreach(Control control in data.Controls)if(control is Label && control.Text=="Preço padrão (R$)")control.Visible=false;
            costs.Controls.Add(_pricing);_pricing.Location=new Point(12,12);costs.Controls.Add(new Label{Text="Preço final do serviço (R$)",Location=new Point(20,500),AutoSize=true});costs.Controls.Add(_price);_price.Location=new Point(20,525);_price.Size=new Size(220,26);
            _pricing.LoadValues(_row==null?new decimal[9]:_database.GetServicePricing(Convert.ToInt64(_row.Cells["Código"].Value)));
            _pricing.ApplyPrice+=(sender,e)=>{if(_pricing.SuggestedPrice>_price.Maximum){MessageBox.Show("O preço sugerido ultrapassa o limite do cadastro.");return;}_price.Value=_pricing.SuggestedPrice;};
            tabs.TabPages.Add(data);tabs.TabPages.Add(costs);Controls.Add(tabs);Controls.Add(footer);tabs.BringToFront();
        }
        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(25,y), AutoSize = true }); control.Location = new Point(25,y+20); control.Size = new Size(430,24); Controls.Add(control); }
        public long SavedId { get; private set; }
        private void Save(object sender, EventArgs e) { if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Informe o nome do serviço."); return; } try { SavedId = _database.SaveService(_row == null ? (long?)null : Convert.ToInt64(_row.Cells["Código"].Value), _code.Text.Trim(), _name.Text.Trim(), _category.Text.Trim(), _duration.Text.Trim(), _price.Value, _active.Checked, _pricing.Values); DialogResult = DialogResult.OK; } catch(Exception ex) { MessageBox.Show(ex.Message); } }
        private static decimal ParseMoney(object value) { decimal result; decimal.TryParse(Convert.ToString(value).Replace("R$", "").Trim(), out result); return result; }
    }

    internal sealed class StockMovementEditor : Form
    {
        private readonly LocalDatabase _database; private readonly long? _selectedProductId; private readonly ComboBox _product = new ComboBox(); private readonly ComboBox _type = new ComboBox(); private readonly NumericUpDown _quantity = new NumericUpDown(); private readonly TextBox _reason = new TextBox();
        public StockMovementEditor(LocalDatabase database, long? selectedProductId = null) { _database = database; _selectedProductId=selectedProductId; Text = "Entrada ou ajuste de estoque"; StartPosition = FormStartPosition.CenterParent; Size = new Size(700, 450); MinimumSize=new Size(660,450); Build(); }
        private void Build()
        {
            _product.DataSource = _database.GetStockProductLookup(); _product.DisplayMember = "display_name"; _product.ValueMember = "id"; _product.DropDownStyle=ComboBoxStyle.DropDownList; _type.Items.AddRange(new object[] { "Entrada", "Ajuste positivo", "Ajuste negativo", "Perda" }); _type.Text = "Entrada"; _quantity.DecimalPlaces = 2; _quantity.Increment=1M; _quantity.Minimum = 0.01M; _quantity.Maximum = 1000000M; _quantity.Value=1M;
            Add("Produto / peça *", _product, 20);var search=new Button{Text="Pesquisar produto",Location=new Point(25,72),Size=new Size(620,30)};search.Click+=(s,e)=>SearchProduct();Controls.Add(search); Add("Tipo de movimentação *", _type, 122); Add("Quantidade *", _quantity, 184); Add("Motivo / observação", _reason, 246);
            if(_selectedProductId.HasValue)_product.SelectedValue=_selectedProductId.Value;
            var save = new Button { Text = "Registrar movimentação", Location = new Point(480, 345), Size = new Size(165, 34), Anchor=AnchorStyles.Bottom|AnchorStyles.Right }; save.Click += Save; Controls.Add(save);
        }
        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(25,y), AutoSize = true }); control.Location = new Point(25,y+20); control.Size = new Size(620,26); Controls.Add(control); }
        private void SearchProduct(){using(var f=new ProductSearchForm(_database,true)){if(f.ShowDialog(this)==DialogResult.OK){_product.DataSource=_database.GetStockProductLookup();_product.DisplayMember="display_name";_product.ValueMember="id";_product.SelectedValue=f.SelectedId;}}}
        private void Save(object sender, EventArgs e) { if (_product.SelectedValue == null) { MessageBox.Show("Cadastre e selecione um produto."); return; } var signedQuantity = (_type.Text == "Ajuste negativo" || _type.Text == "Perda") ? -_quantity.Value : _quantity.Value; try{_database.MoveStockManually(Convert.ToInt64(_product.SelectedValue), signedQuantity, _type.Text, _reason.Text.Trim()); DialogResult = DialogResult.OK;}catch(Exception ex){MessageBox.Show(ex.Message,"Estoque",MessageBoxButtons.OK,MessageBoxIcon.Warning);} }
    }

    internal sealed class ProductSearchForm : Form
    {
        private readonly LocalDatabase _database; private readonly bool _stockOnly; private readonly TextBox _search = new TextBox(); private readonly DataGridView _grid = new DataGridView(); public long SelectedId { get; private set; }
        public ProductSearchForm(LocalDatabase database, bool stockOnly = false) { _database = database; _stockOnly=stockOnly; Text = "Pesquisar produto / peça"; StartPosition = FormStartPosition.CenterParent; Size = new Size(760, 500); Build(); RefreshResults(); }
        private void Build() { Controls.Add(new Label { Text = "Digite nome, código interno ou código de barras:", Location = new Point(18, 18), AutoSize = true }); _search.Location = new Point(18, 40); _search.Size = new Size(490, 24); _search.TextChanged += (s, e) => RefreshResults(); Controls.Add(_search); var search = new Button { Text = "Buscar", Location = new Point(520, 38), Size = new Size(100, 28) }; search.Click += (s, e) => RefreshResults(); Controls.Add(search); _grid.Location = new Point(18, 78); _grid.Size = new Size(710, 330); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; _grid.DoubleClick += (s, e) => SelectProduct(); Controls.Add(_grid); var select = new Button { Text = "Selecionar", Location = new Point(608, 425), Size = new Size(120, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; select.Click += (s, e) => SelectProduct(); Controls.Add(select); }
        private void RefreshResults() { _grid.DataSource = _stockOnly ? _database.FindStockProductLookup(_search.Text.Trim()) : _database.FindProductLookup(_search.Text.Trim()); }
        private void SelectProduct() { if (_grid.CurrentRow == null) { MessageBox.Show("Selecione um produto."); return; } SelectedId = Convert.ToInt64(_grid.CurrentRow.Cells["id"].Value); DialogResult = DialogResult.OK; }
    }

    internal sealed class ServiceSearchForm : Form
    {
        private readonly LocalDatabase _database; private readonly TextBox _search = new TextBox(); private readonly DataGridView _grid = new DataGridView(); public long SelectedId { get; private set; }
        public ServiceSearchForm(LocalDatabase database) { _database = database; Text = "Pesquisar serviço"; StartPosition = FormStartPosition.CenterParent; Size = new Size(760, 500); Build(); RefreshResults(); }
        private void Build() { Controls.Add(new Label { Text = "Digite nome ou código do serviço:", Location = new Point(18, 18), AutoSize = true }); _search.Location = new Point(18, 40); _search.Size = new Size(490, 24); _search.TextChanged += (s, e) => RefreshResults(); Controls.Add(_search); var search = new Button { Text = "Buscar", Location = new Point(520, 38), Size = new Size(100, 28) }; search.Click += (s, e) => RefreshResults(); Controls.Add(search); _grid.Location = new Point(18, 78); _grid.Size = new Size(710, 330); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; _grid.DoubleClick += (s, e) => SelectService(); Controls.Add(_grid); var select = new Button { Text = "Selecionar", Location = new Point(608, 425), Size = new Size(120, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; select.Click += (s, e) => SelectService(); Controls.Add(select); }
        private void RefreshResults() { _grid.DataSource = _database.FindServiceLookup(_search.Text.Trim()); }
        private void SelectService() { if (_grid.CurrentRow == null) { MessageBox.Show("Selecione um serviço."); return; } SelectedId = Convert.ToInt64(_grid.CurrentRow.Cells["id"].Value); DialogResult = DialogResult.OK; }
    }

    internal sealed class PurchaseEditor : Form
    {
        private readonly LocalDatabase _database; private readonly ComboBox _supplier = new ComboBox(); private readonly TextBox _invoice = new TextBox(); private readonly DateTimePicker _date = new DateTimePicker(); private readonly ComboBox _payment = new ComboBox(); private readonly CheckBox _generatePayable = new CheckBox { Text = "Gerar conta a pagar", Checked = true, AutoSize = true }; private readonly ComboBox _terms = new ComboBox(); private readonly NumericUpDown _installmentCount = new NumericUpDown(); private readonly NumericUpDown _intervalDays = new NumericUpDown(); private readonly NumericUpDown _entryAmount = new NumericUpDown(); private readonly NumericUpDown _entryDays = new NumericUpDown(); private readonly ComboBox _product = new ComboBox(); private readonly NumericUpDown _quantity = new NumericUpDown(); private readonly NumericUpDown _cost = new NumericUpDown(); private readonly DataGridView _itemsGrid = new DataGridView(); private readonly DataGridView _installmentsGrid = new DataGridView(); private readonly DataTable _items = new DataTable(); private readonly DataTable _installments = new DataTable(); private readonly Label _total = new Label();
        public PurchaseEditor(LocalDatabase database) { _database = database; Text = "Nova compra"; StartPosition = FormStartPosition.CenterParent; Size = new Size(930, 830); MinimumSize = new Size(800, 700); Build(); }
        private void Build()
        {
            _supplier.DisplayMember = "display_name"; _supplier.ValueMember = "id"; LookupControls.EnableTyping(_supplier); LoadSuppliers(); _supplier.Location = new Point(20, 38); _supplier.Size = new Size(190, 24); Controls.Add(new Label { Text = "Fornecedor *", Location = new Point(20, 18), AutoSize = true }); Controls.Add(_supplier);
            var searchSupplier = new Button { Text = "Pesquisar", Location = new Point(220, 36), Size = new Size(100, 28) }; searchSupplier.Click += (s, e) => SearchSupplier(); Controls.Add(searchSupplier); var newSupplier = new Button { Text = "Novo fornecedor", Location = new Point(330, 36), Size = new Size(130, 28) }; newSupplier.Click += (s, e) => NewSupplier(); Controls.Add(newSupplier);
            Controls.Add(new Label { Text = "NF / documento", Location = new Point(480, 18), AutoSize = true }); _invoice.Location = new Point(480, 38); _invoice.Size = new Size(180, 24); Controls.Add(_invoice);
            Controls.Add(new Label { Text = "Data da compra", Location = new Point(680, 18), AutoSize = true }); _date.Format = DateTimePickerFormat.Short; _date.Location = new Point(680, 38); _date.Size = new Size(150, 24); Controls.Add(_date);
            Controls.Add(new Label { Text = "Forma prevista", Location = new Point(20, 78), AutoSize = true }); _payment.Items.AddRange(new object[] { "Dinheiro", "PIX", "Débito", "Crédito", "Transferência", "Boleto", "Crediário", "Outros" }); _payment.Text = "Boleto"; _payment.Location = new Point(20, 98); _payment.Size = new Size(150, 24); Controls.Add(_payment); _generatePayable.Location = new Point(190, 100); _generatePayable.CheckedChanged += (s, e) => UpdateTermsState(); Controls.Add(_generatePayable);
            Controls.Add(new Label { Text = "Condição", Location = new Point(20, 136), AutoSize = true }); _terms.Items.AddRange(new object[] { "À vista", "Parcelado", "Entrada + parcelas" }); _terms.DropDownStyle = ComboBoxStyle.DropDownList; _terms.SelectedIndex = 1; _terms.Location = new Point(20, 156); _terms.Size = new Size(150, 24); _terms.SelectedIndexChanged += (s, e) => UpdateTermsState(); Controls.Add(_terms);
            Controls.Add(new Label { Text = "Parcelas", Location = new Point(185, 136), AutoSize = true }); _installmentCount.Minimum = 1; _installmentCount.Maximum = 48; _installmentCount.Value = 3; _installmentCount.Location = new Point(185, 156); _installmentCount.Size = new Size(85, 24); Controls.Add(_installmentCount);
            Controls.Add(new Label { Text = "Intervalo (dias)", Location = new Point(285, 136), AutoSize = true }); _intervalDays.Minimum = 1; _intervalDays.Maximum = 365; _intervalDays.Value = 30; _intervalDays.Location = new Point(285, 156); _intervalDays.Size = new Size(110, 24); Controls.Add(_intervalDays);
            Controls.Add(new Label { Text = "Entrada (R$)", Location = new Point(410, 136), AutoSize = true }); _entryAmount.DecimalPlaces = 2; _entryAmount.Maximum = 1000000; _entryAmount.Location = new Point(410, 156); _entryAmount.Size = new Size(110, 24); Controls.Add(_entryAmount);
            Controls.Add(new Label { Text = "Entrada em dias", Location = new Point(535, 136), AutoSize = true }); _entryDays.Maximum = 365; _entryDays.Location = new Point(535, 156); _entryDays.Size = new Size(105, 24); Controls.Add(_entryDays);
            var generate = new Button { Text = "Gerar parcelas", Location = new Point(655, 153), Size = new Size(130, 28) }; generate.Click += (s, e) => GenerateInstallments(); Controls.Add(generate); UpdateTermsState();
            Controls.Add(new Label { Text = "Produto / peça *", Location = new Point(20, 205), AutoSize = true }); _product.DisplayMember = "display_name"; _product.ValueMember = "id"; LookupControls.EnableTyping(_product); _product.DataSource = _database.GetProductLookup(); _product.Location = new Point(20, 225); _product.Size = new Size(230, 24); Controls.Add(_product); var searchProduct=new Button{Text="Pesquisar",Location=new Point(260,223),Size=new Size(120,28)};searchProduct.Click+=(s,e)=>SearchProduct();Controls.Add(searchProduct);
            Controls.Add(new Label { Text = "Quantidade *", Location = new Point(395, 205), AutoSize = true }); _quantity.DecimalPlaces = 2; _quantity.Minimum = 0.01M; _quantity.Maximum = 1000000; _quantity.Value = 1; _quantity.Location = new Point(395, 225); _quantity.Size = new Size(105, 24); Controls.Add(_quantity);
            Controls.Add(new Label { Text = "Custo unitário (R$) *", Location = new Point(515, 205), AutoSize = true }); _cost.DecimalPlaces = 2; _cost.Maximum = 1000000; _cost.Location = new Point(515, 225); _cost.Size = new Size(140, 24); Controls.Add(_cost);
            var add = new Button { Text = "Adicionar item", Location = new Point(670, 223), Size = new Size(130, 28) }; add.Click += (s, e) => AddItem(); Controls.Add(add);
            _items.Columns.Add("product_id", typeof(long)); _items.Columns.Add("description", typeof(string)); _items.Columns.Add("quantity", typeof(decimal)); _items.Columns.Add("unit_cost", typeof(decimal)); _items.Columns.Add("Produto / peça", typeof(string)); _items.Columns.Add("Qtd.", typeof(decimal)); _items.Columns.Add("Custo unitário", typeof(decimal)); _items.Columns.Add("Total", typeof(decimal));
            _itemsGrid.Location = new Point(20, 275); _itemsGrid.Size = new Size(790, 190); _itemsGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; _itemsGrid.ReadOnly = true; _itemsGrid.AllowUserToAddRows = false; _itemsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _itemsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _itemsGrid.BackgroundColor = Color.White; _itemsGrid.AutoGenerateColumns = false;
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "product_id", Name = "product_id", Visible = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "description", Name = "description", Visible = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "quantity", Name = "quantity", Visible = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "unit_cost", Name = "unit_cost", Visible = false });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Produto / peça", Name = "Produto / peça", HeaderText = "Produto / peça" });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Qtd.", Name = "Qtd.", HeaderText = "Qtd." });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Custo unitário", Name = "Custo unitário", HeaderText = "Custo unitário" });
            _itemsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Total", Name = "Total", HeaderText = "Total" });
            _itemsGrid.Columns["Qtd."].DefaultCellStyle.Format = "N2"; _itemsGrid.Columns["Custo unitário"].DefaultCellStyle.Format = "N2"; _itemsGrid.Columns["Total"].DefaultCellStyle.Format = "N2";
            _itemsGrid.DataSource = _items; Controls.Add(_itemsGrid);
            var remove = new Button { Text = "Remover item", Location = new Point(20, 475), Size = new Size(110, 30) }; remove.Click += (s, e) => { if (_itemsGrid.CurrentRow != null) { _items.Rows.RemoveAt(_itemsGrid.CurrentRow.Index); RefreshTotal(); } }; Controls.Add(remove);
            Controls.Add(new Label { Text = "Parcelas geradas — ajuste vencimento e valor, se necessário", Location = new Point(20, 520), AutoSize = true, ForeColor = Color.FromArgb(53, 73, 94) });
            _installments.Columns.Add("number", typeof(int)); _installments.Columns.Add("due_date", typeof(DateTime)); _installments.Columns.Add("amount", typeof(decimal));
            _installmentsGrid.Location = new Point(20, 545); _installmentsGrid.Size = new Size(500, 115); _installmentsGrid.AllowUserToAddRows = false; _installmentsGrid.AllowUserToDeleteRows = true; _installmentsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _installmentsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _installmentsGrid.BackgroundColor = Color.White; _installmentsGrid.AutoGenerateColumns = false;
            _installmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "number", Name = "number", HeaderText = "Parcela", ReadOnly = true });
            _installmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "due_date", Name = "due_date", HeaderText = "Vencimento" });
            _installmentsGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "amount", Name = "amount", HeaderText = "Valor" });
            _installmentsGrid.Columns["due_date"].DefaultCellStyle.Format = "dd/MM/yyyy"; _installmentsGrid.Columns["amount"].DefaultCellStyle.Format = "N2";
            _installmentsGrid.DataSource = _installments; Controls.Add(_installmentsGrid);
            _total.Location = new Point(20, 685); _total.AutoSize = true; _total.Font = new Font("Segoe UI", 14, FontStyle.Bold); _total.ForeColor = Color.FromArgb(7, 55, 94); Controls.Add(_total);
            var save = new Button { Text = "Finalizar compra", Location = new Point(650, 680), Size = new Size(160, 36), BackColor = Color.FromArgb(24, 189, 200), ForeColor = Color.White, FlatStyle = FlatStyle.Flat }; save.Click += Save; Controls.Add(save); RefreshTotal();
        }
        private void LoadSuppliers() { _supplier.DataSource = _database.GetSupplierLookup(); _supplier.DisplayMember = "display_name"; _supplier.ValueMember = "id"; }
        private void SearchSupplier() { using (var form = new FinancialPartySearchForm(_database, "Pagar")) { if (form.ShowDialog(this) == DialogResult.OK) { LoadSuppliers(); _supplier.SelectedValue = form.SelectedId; } } }
        private void NewSupplier() { using (var form = new SupplierEditor(_database, null)) { if (form.ShowDialog(this) == DialogResult.OK) LoadSuppliers(); } }
        private void SearchProduct() { using (var form = new ProductSearchForm(_database)) { if (form.ShowDialog(this) == DialogResult.OK) { _product.DataSource = _database.GetProductLookup(); _product.DisplayMember = "display_name"; _product.ValueMember = "id"; _product.SelectedValue = form.SelectedId; } } }
        private void AddItem()
        {
            var selected = _product.SelectedItem as DataRowView; if (selected == null) { MessageBox.Show("Selecione um produto."); return; } if (_cost.Value <= 0) { MessageBox.Show("Informe o custo unitário."); return; } var id = Convert.ToInt64(selected["id"]); foreach (DataRow item in _items.Rows) if (Convert.ToInt64(item["product_id"]) == id) { item["Qtd."] = item["quantity"] = Convert.ToDecimal(item["quantity"]) + _quantity.Value; item["Total"] = Math.Round(Convert.ToDecimal(item["quantity"]) * Convert.ToDecimal(item["unit_cost"]), 2, MidpointRounding.AwayFromZero); RefreshTotal(); return; }
            var row = _items.NewRow(); row["product_id"] = id; row["description"] = row["Produto / peça"] = Convert.ToString(selected["name"]); row["quantity"] = row["Qtd."] = _quantity.Value; row["unit_cost"] = row["Custo unitário"] = _cost.Value; row["Total"] = Math.Round(_quantity.Value * _cost.Value, 2, MidpointRounding.AwayFromZero); _items.Rows.Add(row); RefreshTotal();
        }
        private decimal PurchaseTotal() { decimal total = 0; foreach (DataRow row in _items.Rows) total += Convert.ToDecimal(row["Total"]); return Math.Round(total, 2, MidpointRounding.AwayFromZero); }
        private void UpdateTermsState() { var enabled = _generatePayable.Checked; _terms.Enabled = enabled; _installmentCount.Enabled = enabled && _terms.Text != "À vista"; _intervalDays.Enabled = enabled && _terms.Text != "À vista"; var entry = enabled && _terms.Text == "Entrada + parcelas"; _entryAmount.Enabled = entry; _entryDays.Enabled = entry; }
        private void GenerateInstallments()
        {
            var total = PurchaseTotal(); if (total <= 0) { MessageBox.Show("Inclua as peças antes de gerar as parcelas."); return; } _installments.Rows.Clear(); var count = Convert.ToInt32(_installmentCount.Value); var interval = Convert.ToInt32(_intervalDays.Value); decimal remaining = total; var number = 1;
            if (_terms.Text == "Entrada + parcelas") { if (_entryAmount.Value <= 0 || _entryAmount.Value >= total) { MessageBox.Show("Informe uma entrada maior que zero e menor que o total da compra."); return; } _installments.Rows.Add(number++, _date.Value.Date.AddDays(Convert.ToInt32(_entryDays.Value)), _entryAmount.Value); remaining -= _entryAmount.Value; }
            if (_terms.Text == "À vista") { _installments.Rows.Add(number, _date.Value.Date, remaining); return; }
            var part = Math.Round(remaining / count, 2, MidpointRounding.AwayFromZero); for (var i = 1; i <= count; i++) { var value = i == count ? Math.Round(remaining - part * (count - 1), 2, MidpointRounding.AwayFromZero) : part; _installments.Rows.Add(number++, _date.Value.Date.AddDays(interval * i), value); }
        }
        private void RefreshTotal() { decimal total = 0; foreach (DataRow row in _items.Rows) total += Convert.ToDecimal(row["Total"]); _total.Text = "Total da compra: " + total.ToString("C2"); }
        private void Save(object sender, EventArgs e)
        {
            if (_supplier.SelectedValue == null) { MessageBox.Show("Selecione o fornecedor."); return; } if (_items.Rows.Count == 0) { MessageBox.Show("Inclua ao menos uma peça."); return; } if (_generatePayable.Checked && _installments.Rows.Count == 0) { GenerateInstallments(); if (_installments.Rows.Count == 0) return; }
            var supplier = _supplier.SelectedItem as DataRowView; try { _database.SavePurchase(_database.NextPurchaseNumber(), Convert.ToInt64(_supplier.SelectedValue), supplier == null ? _supplier.Text : Convert.ToString(supplier["name"]), _invoice.Text.Trim(), _date.Value.Date, _payment.Text, _generatePayable.Checked, _items, _installments); MessageBox.Show("Compra registrada com sucesso."); DialogResult = DialogResult.OK; } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }

    internal sealed class CashForm : Form
    {
        private readonly LocalDatabase _database; private readonly SystemUser _user; private readonly DataGridView _grid=new DataGridView(); private readonly Label _status=new Label(); private long? _cashId;
        public CashForm(LocalDatabase database,SystemUser user) { _database=database; _user=user; Text="Caixa"; StartPosition=FormStartPosition.CenterParent; Size=new Size(820,560); Build(); RefreshCash(); }
        private void Build() { var open=new Button{Text="Abrir caixa",Location=new Point(18,16),Size=new Size(110,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"cash.open")};open.Click+=(s,e)=>Open();Controls.Add(open); var supply=new Button{Text="Suprimento",Location=new Point(140,16),Size=new Size(110,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"cash.movement")};supply.Click+=(s,e)=>Move("Suprimento");Controls.Add(supply);var withdrawal=new Button{Text="Sangria",Location=new Point(262,16),Size=new Size(110,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"cash.movement")};withdrawal.Click+=(s,e)=>Move("Sangria");Controls.Add(withdrawal);var close=new Button{Text="Fechar caixa",Location=new Point(384,16),Size=new Size(110,30),Enabled=_database.HasPermission(_user.Id,_user.Role,"cash.close")};close.Click+=(s,e)=>CloseCash();Controls.Add(close);_status.Location=new Point(18,58);_status.AutoSize=true;_status.Font=new Font("Segoe UI",11,FontStyle.Bold);Controls.Add(_status);_grid.Location=new Point(18,88);_grid.Size=new Size(760,400);_grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.BackgroundColor=Color.White;Controls.Add(_grid); }
        private void RefreshCash() { var open=_database.GetOpenCash(); if(open.Rows.Count==0){_cashId=null;_status.Text="Caixa fechado";_grid.DataSource=null;return;} _cashId=Convert.ToInt64(open.Rows[0]["id"]);var initial=Convert.ToDecimal(open.Rows[0]["opening_balance"]);_status.Text="Caixa aberto • Saldo inicial: "+initial.ToString("C2")+" • Dinheiro esperado: "+_database.GetCashExpected(_cashId.Value).ToString("C2");_grid.DataSource=_database.GetCashMovements(_cashId.Value);}
        private void Open(){if(_cashId.HasValue){MessageBox.Show("Já existe um caixa aberto.");return;} using(var f=new CashValueForm("Abrir caixa","Saldo inicial (R$)")){if(f.ShowDialog(this)==DialogResult.OK){_database.OpenCash(f.Value);RefreshCash();}}}
        private void Move(string type){if(!_cashId.HasValue){MessageBox.Show("Abra o caixa primeiro.");return;}using(var f=new CashValueForm(type,type+" (R$)",true)){if(f.ShowDialog(this)==DialogResult.OK){_database.AddCashMovement(_cashId.Value,type,"Dinheiro",type=="Sangria"?-f.Value:f.Value,f.Description);RefreshCash();}}}
        private void CloseCash(){if(!_cashId.HasValue){MessageBox.Show("Não há caixa aberto.");return;}using(var f=new CashValueForm("Fechar caixa","Valor contado em dinheiro (R$)")){if(f.ShowDialog(this)==DialogResult.OK){var expected=_database.GetCashExpected(_cashId.Value);_database.CloseCash(_cashId.Value,f.Value);MessageBox.Show("Caixa fechado. Esperado: "+expected.ToString("C2")+" | Diferença: "+(f.Value-expected).ToString("C2"));RefreshCash();}}}
    }
    internal sealed class CashValueForm : Form
    {
        public decimal Value { get { return _value.Value; } } public string Description { get { return _description.Text.Trim(); } }
        private readonly NumericUpDown _value = new NumericUpDown(); private readonly TextBox _description = new TextBox(); private readonly bool _requireDescription;
        public CashValueForm(string title, string label) : this(title, label, false) { }
        public CashValueForm(string title, string label, bool requireDescription)
        {
            _requireDescription = requireDescription; Text = title; StartPosition = FormStartPosition.CenterParent; Size = new Size(420, requireDescription ? 255 : 190);
            Controls.Add(new Label { Text = label, Location = new Point(22, 22), AutoSize = true }); _value.DecimalPlaces = 2; _value.Maximum = 1000000; _value.Location = new Point(22, 48); _value.Size = new Size(360, 24); Controls.Add(_value);
            if (requireDescription) { Controls.Add(new Label { Text = "Descrição / motivo *", Location = new Point(22, 88), AutoSize = true }); _description.Location = new Point(22, 110); _description.Size = new Size(360, 24); Controls.Add(_description); }
            var ok = new Button { Text = "Confirmar", Location = new Point(260, requireDescription ? 165 : 95), Size = new Size(122, 32) }; ok.Click += Confirm; Controls.Add(ok);
        }
        private void Confirm(object sender, EventArgs e) { if (_requireDescription && string.IsNullOrWhiteSpace(_description.Text)) { MessageBox.Show("Informe a descrição ou o motivo do lançamento."); return; } DialogResult = DialogResult.OK; }
    }

    internal sealed class FinancialFlowForm : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridView _grid=new DataGridView(); private readonly Label _summary=new Label();
        public FinancialFlowForm(LocalDatabase database){_database=database;Text="Fluxo de caixa";StartPosition=FormStartPosition.CenterParent;Size=new Size(850,580);MinimumSize=new Size(700,460);Build();}
        private void Build(){var receive=_database.GetFinancialPaidTotal("Receber");var pay=_database.GetFinancialPaidTotal("Pagar");_summary.Text="Receitas recebidas: "+receive.ToString("C2")+"    Despesas pagas: "+pay.ToString("C2")+"    Resultado: "+(receive-pay).ToString("C2");_summary.Location=new Point(18,18);_summary.AutoSize=true;_summary.Font=new Font("Segoe UI",11,FontStyle.Bold);_summary.ForeColor=Color.FromArgb(7,55,94);Controls.Add(_summary);_grid.Location=new Point(18,55);_grid.Size=new Size(795,450);_grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AllowUserToDeleteRows=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.BackgroundColor=Color.White;_grid.DataSource=_database.GetFinancialFlow();Controls.Add(_grid);}
    }

    internal sealed class FinancialDailyForm : Form
    {
        private readonly LocalDatabase _database; private readonly DateTimePicker _date = new DateTimePicker(); private readonly DataGridView _grid = new DataGridView(); private readonly Label _summary = new Label();
        public FinancialDailyForm(LocalDatabase database) { _database = database; Text = "Resumo financeiro do dia"; StartPosition = FormStartPosition.CenterParent; Size = new Size(960, 620); MinimumSize = new Size(760, 500); Build(); RefreshResults(); }
        private void Build()
        {
            Controls.Add(new Label { Text = "Data", Location = new Point(18, 20), AutoSize = true }); _date.Format = DateTimePickerFormat.Short; _date.Location = new Point(18, 41); _date.Size = new Size(130, 24); _date.ValueChanged += (s, e) => RefreshResults(); Controls.Add(_date);
            var refresh = new Button { Text = "Atualizar", Location = new Point(160, 38), Size = new Size(100, 28) }; refresh.Click += (s, e) => RefreshResults(); Controls.Add(refresh);
            _summary.Location = new Point(18, 84); _summary.Size = new Size(880, 44); _summary.Font = new Font("Segoe UI", 10, FontStyle.Bold); _summary.ForeColor = Color.FromArgb(7, 55, 94); Controls.Add(_summary);
            _grid.Location = new Point(18, 132); _grid.Size = new Size(885, 410); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; Controls.Add(_grid);
        }
        private void RefreshResults()
        {
            var data = _database.GetFinancialDailyMovements(_date.Value.Date); _grid.DataSource = data; if (_grid.Columns.Contains("raw_impact")) _grid.Columns["raw_impact"].Visible = false; if (_grid.Columns.Contains("Data")) _grid.Columns["Data"].Visible = false;
            decimal received = 0, paid = 0; var byPayment = new System.Collections.Generic.Dictionary<string, decimal>();
            foreach (DataRow row in data.Rows) { var amount = Convert.ToDecimal(row["raw_impact"]); var type = Convert.ToString(row["Tipo"]); var impact = type == "Pagar" || type == "Sangria" ? -Math.Abs(amount) : amount; if (impact >= 0) received += impact; else paid += Math.Abs(impact); var payment = Convert.ToString(row["Pagamento"]); if (!byPayment.ContainsKey(payment)) byPayment[payment] = 0; byPayment[payment] += impact; }
            var methods = ""; foreach (var item in byPayment) methods += (methods == "" ? "" : "  |  ") + item.Key + ": " + item.Value.ToString("C2");
            _summary.Text = "Entradas: " + received.ToString("C2") + "    Saídas: " + paid.ToString("C2") + "    Resultado: " + (received - paid).ToString("C2") + (methods == "" ? "" : "\nPor forma: " + methods);
        }
    }

    internal sealed class FinancialAlertsForm : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridView _grid = new DataGridView(); private readonly Label _summary = new Label();
        public FinancialAlertsForm(LocalDatabase database) { _database = database; Text = "Alertas financeiros"; StartPosition = FormStartPosition.CenterParent; Size = new Size(1020, 620); MinimumSize = new Size(780, 500); Build(); RefreshAlerts(); }
        private void Build()
        {
            _summary.Location = new Point(18, 18); _summary.Size = new Size(950, 45); _summary.Font = new Font("Segoe UI", 11, FontStyle.Bold); _summary.ForeColor = Color.FromArgb(7, 55, 94); Controls.Add(_summary);
            var refresh = new Button { Text = "Atualizar", Location = new Point(18, 70), Size = new Size(100, 28) }; refresh.Click += (s, e) => RefreshAlerts(); Controls.Add(refresh);
            Controls.Add(new Label { Text = "São exibidas somente contas pendentes que já venceram, vencem hoje ou vencem nos próximos 7 dias.", Location = new Point(135, 76), AutoSize = true, ForeColor = Color.FromArgb(53, 73, 94) });
            _grid.Location = new Point(18, 112); _grid.Size = new Size(965, 430); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; Controls.Add(_grid);
        }
        private void RefreshAlerts()
        {
            var data = _database.GetFinancialAlerts(7); _grid.DataSource = data; var overdue = 0; var today = 0; var upcoming = 0; foreach (DataRow row in data.Rows) { var alert = Convert.ToString(row["Alerta"]); if (alert == "Vencida") overdue++; else if (alert == "Vence hoje") today++; else upcoming++; }
            _summary.Text = "Vencidas: " + overdue + "    Vencem hoje: " + today + "    Próximos 7 dias: " + upcoming;
        }
    }

    internal sealed class FinancialAccountsForm : Form
    {
        private readonly LocalDatabase _database; private readonly string _type; private readonly ComboBox _status = new ComboBox(); private readonly ComboBox _period = new ComboBox(); private readonly DateTimePicker _start = new DateTimePicker(); private readonly DateTimePicker _end = new DateTimePicker(); private readonly TextBox _search = new TextBox(); private readonly DataGridView _grid = new DataGridView(); private readonly Label _summary = new Label();
        public FinancialAccountsForm(LocalDatabase database, string type) { _database = database; _type = type; Text = type == "Receber" ? "Contas a Receber" : "Contas a Pagar"; StartPosition = FormStartPosition.CenterParent; Size = new Size(1060, 680); MinimumSize = new Size(820, 540); Build(); _period.Text = "Mês"; SetPeriod("Mês"); RefreshResults(); }
        private void Build()
        {
            Controls.Add(new Label { Text = "Situação", Location = new Point(18, 18), AutoSize = true }); _status.Items.AddRange(new object[] { "Todos", "Pendente", "Vencido", "Pago", "Cancelado" }); _status.Text = "Todos"; _status.Location = new Point(18, 39); _status.Size = new Size(115, 24); _status.SelectedIndexChanged += (s, e) => RefreshResults(); Controls.Add(_status);
            Controls.Add(new Label { Text = "Período", Location = new Point(150, 18), AutoSize = true }); _period.Items.AddRange(new object[] { "Todos", "Hoje", "Semana", "Mês", "Versão", "Personalizado" }); _period.DropDownStyle = ComboBoxStyle.DropDownList; _period.Location = new Point(150, 39); _period.Size = new Size(120, 24); _period.SelectedIndexChanged += (s, e) => { if (_period.Text != "Personalizado") SetPeriod(_period.Text); RefreshResults(); }; Controls.Add(_period);
            Controls.Add(new Label { Text = "De", Location = new Point(290, 18), AutoSize = true }); _start.Format = DateTimePickerFormat.Short; _start.Location = new Point(290, 39); _start.Size = new Size(120, 24); _start.ValueChanged += (s, e) => { if (_period.Text == "Personalizado") RefreshResults(); }; Controls.Add(_start);
            Controls.Add(new Label { Text = "Até", Location = new Point(425, 18), AutoSize = true }); _end.Format = DateTimePickerFormat.Short; _end.Location = new Point(425, 39); _end.Size = new Size(120, 24); _end.ValueChanged += (s, e) => { if (_period.Text == "Personalizado") RefreshResults(); }; Controls.Add(_end);
            Controls.Add(new Label { Text = "Pesquisar", Location = new Point(565, 18), AutoSize = true }); _search.Location = new Point(565, 39); _search.Size = new Size(240, 24); _search.BorderStyle = BorderStyle.FixedSingle; _search.TextChanged += (s, e) => RefreshResults(); Controls.Add(_search);
            var refresh = new Button { Text = "Atualizar", Location = new Point(820, 37), Size = new Size(100, 28) }; refresh.Click += (s, e) => RefreshResults(); Controls.Add(refresh);
            var settle = new Button { Text = _type == "Receber" ? "Receber selecionada" : "Pagar selecionada", Location = new Point(18, 82), Size = new Size(150, 30) }; settle.Click += (s, e) => Settle(); Controls.Add(settle);
            var cancel = new Button { Text = "Cancelar selecionada", Location = new Point(178, 82), Size = new Size(150, 30) }; cancel.Click += (s, e) => Cancel(); Controls.Add(cancel);
            _summary.Location = new Point(18, 124); _summary.Size = new Size(980, 42); _summary.Font = new Font("Segoe UI", 10, FontStyle.Bold); _summary.ForeColor = Color.FromArgb(7, 55, 94); Controls.Add(_summary);
            _grid.Location = new Point(18, 175); _grid.Size = new Size(1005, 410); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; Controls.Add(_grid);
            Controls.Add(new Label { Text = "Dinheiro exige caixa aberto. PIX, cartão, transferência e boleto atualizam apenas o Financeiro.", Location = new Point(18, 600), AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, ForeColor = Color.FromArgb(53, 73, 94) });
        }
        private void SetPeriod(string period)
        {
            var today = DateTime.Today; if (period == "Todos") { _start.Enabled = false; _end.Enabled = false; return; } _start.Enabled = true; _end.Enabled = true;
            if (period == "Hoje") { _start.Value = today; _end.Value = today; }
            else if (period == "Semana") { var first = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1)); _start.Value = first; _end.Value = first.AddDays(6); }
            else if (period == "Mês") { _start.Value = new DateTime(today.Year, today.Month, 1); _end.Value = _start.Value.AddMonths(1).AddDays(-1); }
            else if (period == "Versão") { _start.Value = new DateTime(today.Year, 1, 1); _end.Value = new DateTime(today.Year, 12, 31); }
        }
        private void RefreshResults()
        {
            DateTime? start = _period.Text == "Todos" ? (DateTime?)null : _start.Value.Date; DateTime? end = _period.Text == "Todos" ? (DateTime?)null : _end.Value.Date; if (start.HasValue && end.Value < start.Value) { MessageBox.Show("A data final deve ser igual ou posterior à inicial."); return; }
            _grid.DataSource = _database.FindFinancialEntriesByType(_type, _status.Text, _search.Text.Trim(), start, end); if (_grid.Columns.Contains("Tipo")) _grid.Columns["Tipo"].Visible = false;
            var summary = _database.GetFinancialAccountSummary(_type, start, end).Rows[0]; _summary.Text = "Pendentes: " + Convert.ToDecimal(summary["pending"]).ToString("C2") + "    Vencidas: " + Convert.ToDecimal(summary["overdue"]).ToString("C2") + "    Pagas: " + Convert.ToDecimal(summary["paid"]).ToString("C2") + "    Canceladas: " + Convert.ToDecimal(summary["cancelled"]).ToString("C2");
        }
        private long? SelectedId() { if (_grid.CurrentRow == null) { MessageBox.Show("Selecione uma conta."); return null; } return Convert.ToInt64(_grid.CurrentRow.Cells["Código"].Value); }
        private void Settle() { var id = SelectedId(); if (id == null) return; var status = Convert.ToString(_grid.CurrentRow.Cells["Status"].Value); if (status == "Pago" || status == "Cancelado") { MessageBox.Show("Esta conta não pode ser baixada."); return; } using (var form = new FinancialSettlementForm(_grid.CurrentRow)) { if (form.ShowDialog(this) == DialogResult.OK) { try { _database.MarkFinancialEntryPaid(id.Value, form.PaymentMethod, form.PaidAt); RefreshResults(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Financeiro", MessageBoxButtons.OK, MessageBoxIcon.Warning); } } } }
        private void Cancel() { var id = SelectedId(); if (id == null) return; var status = Convert.ToString(_grid.CurrentRow.Cells["Status"].Value); if (status == "Pago" || status == "Cancelado") { MessageBox.Show("Esta conta não pode ser cancelada."); return; } if (MessageBox.Show("Cancelar a conta selecionada?", "Financeiro", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _database.CancelFinancialEntry(id.Value); RefreshResults(); } }
    }

    internal sealed class ReportsForm : Form
    {
        private readonly LocalDatabase _database; private readonly DateTimePicker _start = new DateTimePicker(); private readonly DateTimePicker _end = new DateTimePicker(); private readonly Label _summary = new Label(); private readonly DataGridView _serviceRanking = ReportGrid(); private readonly DataGridView _payments = ReportGrid(); private readonly DataGridView _products = ReportGrid(); private readonly DataGridView _lowStock = ReportGrid();
        public ReportsForm(LocalDatabase database) { _database = database; Text = "Relatórios gerenciais"; StartPosition = FormStartPosition.CenterParent; Size = new Size(1060, 720); MinimumSize = new Size(840, 580); Build(); SetMonth(); RefreshReports(); }
        private void Build()
        {
            Controls.Add(new Label { Text = "Período", Location = new Point(18, 19), AutoSize = true }); _start.Format = DateTimePickerFormat.Short; _start.Location = new Point(18, 40); _start.Size = new Size(120, 24); Controls.Add(_start); Controls.Add(new Label { Text = "até", Location = new Point(148, 44), AutoSize = true }); _end.Format = DateTimePickerFormat.Short; _end.Location = new Point(176, 40); _end.Size = new Size(120, 24); Controls.Add(_end);
            AddPeriodButton("Hoje", 310, () => { _start.Value = DateTime.Today; _end.Value = DateTime.Today; }); AddPeriodButton("Semana", 390, SetWeek); AddPeriodButton("Mês", 480, SetMonth); AddPeriodButton("Versão", 560, SetYear);
            var update = new Button { Text = "Atualizar", Location = new Point(640, 38), Size = new Size(100, 28) }; update.Click += (s, e) => RefreshReports(); Controls.Add(update);
            _summary.Location = new Point(18, 82); _summary.Size = new Size(980, 80); _summary.Font = new Font("Segoe UI", 11, FontStyle.Bold); _summary.ForeColor = Color.FromArgb(7, 55, 94); Controls.Add(_summary);
            var tabs = new TabControl { Location = new Point(18, 175), Size = new Size(1005, 480), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            tabs.TabPages.Add(Tab("Vendas por pagamento", _payments)); tabs.TabPages.Add(Tab("Produtos mais vendidos", _products)); if(_database.Get("modules.services")=="1")tabs.TabPages.Add(Tab("Serviços por descrição (OS finalizadas)", _serviceRanking)); tabs.TabPages.Add(Tab("Estoque baixo", _lowStock)); Controls.Add(tabs);
        }
        private void AddPeriodButton(string text, int x, Action action) { var button = new Button { Text = text, Location = new Point(x, 38), Size = new Size(72, 28) }; button.Click += (s, e) => { action(); RefreshReports(); }; Controls.Add(button); }
        private TabPage Tab(string text, DataGridView grid) { var page = new TabPage(text); grid.Location = new Point(12, 12); grid.Size = new Size(955, 390); grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; page.Controls.Add(grid); return page; }
        private void SetWeek() { var today = DateTime.Today; var first = today.AddDays(-((int)today.DayOfWeek == 0 ? 6 : (int)today.DayOfWeek - 1)); _start.Value = first; _end.Value = first.AddDays(6); }
        private void SetMonth() { _start.Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); _end.Value = _start.Value.AddMonths(1).AddDays(-1); }
        private void SetYear() { _start.Value = new DateTime(DateTime.Today.Year, 1, 1); _end.Value = new DateTime(DateTime.Today.Year, 12, 31); }
        private void RefreshReports()
        {
            if (_end.Value.Date < _start.Value.Date) { MessageBox.Show("A data final deve ser igual ou posterior à data inicial."); return; }
            var data = _database.GetReportSummary(_start.Value.Date, _end.Value.Date); var row = data.Rows[0]; var received = Convert.ToDecimal(row["received"]); var paid = Convert.ToDecimal(row["paid"]); var result = received - paid;
            _summary.Text = "Receitas recebidas: " + received.ToString("C2") + "    Despesas pagas: " + paid.ToString("C2") + "    Resultado: " + result.ToString("C2") + "\nA receber pendente: " + Convert.ToDecimal(row["pending_receive"]).ToString("C2") + "    A pagar pendente: " + Convert.ToDecimal(row["pending_pay"]).ToString("C2") + "    Vendas: " + Convert.ToInt64(row["sales_count"]) + "    OS abertas: " + Convert.ToInt64(row["orders_count"]);
            _payments.DataSource = _database.GetReportSalesByPayment(_start.Value.Date, _end.Value.Date); _products.DataSource = _database.GetReportTopProducts(_start.Value.Date, _end.Value.Date); foreach(DataGridViewColumn column in _products.Columns) { if(column.Name.StartsWith("Valor")||column.Name=="Descontos")column.DefaultCellStyle.Format="C2"; if(column.Name=="Quantidade")column.DefaultCellStyle.Format="0.###"; } _lowStock.DataSource = _database.GetReportLowStock(); if(_database.Get("modules.services")=="1"){_serviceRanking.DataSource=_database.GetReportTopServices(_start.Value.Date,_end.Value.Date);_serviceRanking.Columns["Valor"].DefaultCellStyle.Format="C2";}
        }
        private static DataGridView ReportGrid() { return new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = Color.White }; }
    }

    internal sealed class FinancialSettlementForm : Form
    {
        private readonly ComboBox _payment = new ComboBox(); private readonly DateTimePicker _paidAt = new DateTimePicker();
        public string PaymentMethod { get { return _payment.Text; } } public DateTime PaidAt { get { return _paidAt.Value.Date; } }
        public FinancialSettlementForm(DataGridViewRow row)
        {
            Text = "Baixar conta"; StartPosition = FormStartPosition.CenterParent; Size = new Size(470, 315); MinimumSize = new Size(470, 315);
            var type = Convert.ToString(row.Cells["Tipo"].Value) == "Receber" ? "Recebimento" : "Pagamento";
            Controls.Add(new Label { Text = type + " da conta selecionada", Location = new Point(24, 20), AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
            Controls.Add(new Label { Text = "Descrição: " + Convert.ToString(row.Cells["Descrição"].Value), Location = new Point(24, 52), Size = new Size(400, 36) });
            Controls.Add(new Label { Text = "Valor: " + Convert.ToString(row.Cells["Valor"].Value), Location = new Point(24, 88), AutoSize = true });
            Controls.Add(new Label { Text = "Forma de pagamento *", Location = new Point(24, 122), AutoSize = true });
            _payment.Items.AddRange(new object[] { "Dinheiro", "PIX", "Débito", "Crédito", "Transferência", "Boleto", "Crediário", "Outros" }); _payment.Location = new Point(24, 143); _payment.Size = new Size(195, 24); _payment.Text = Convert.ToString(row.Cells["Pagamento"].Value); if (string.IsNullOrWhiteSpace(_payment.Text)) _payment.Text = "PIX"; Controls.Add(_payment);
            Controls.Add(new Label { Text = "Data da baixa", Location = new Point(240, 122), AutoSize = true }); _paidAt.Format = DateTimePickerFormat.Short; _paidAt.Location = new Point(240, 143); _paidAt.Size = new Size(170, 24); Controls.Add(_paidAt);
            Controls.Add(new Label { Text = "Atenção: para dinheiro, o caixa precisa estar aberto.", Location = new Point(24, 184), AutoSize = true, ForeColor = Color.FromArgb(150, 80, 0) });
            var save = new Button { Text = "Confirmar baixa", Location = new Point(280, 220), Size = new Size(130, 32) }; save.Click += (s, e) => { if (string.IsNullOrWhiteSpace(_payment.Text)) { MessageBox.Show("Informe a forma de pagamento."); return; } DialogResult = DialogResult.OK; }; Controls.Add(save);
        }
    }

    internal sealed class FinancialEntryEditor : Form
    {
        private readonly LocalDatabase _database; private readonly string _type;
        private readonly ComboBox _party=new ComboBox(); private readonly ComboBox _category=new ComboBox(); private readonly TextBox _description=new TextBox(); private readonly TextBox _document=new TextBox(); private readonly NumericUpDown _amount=new NumericUpDown(); private readonly DateTimePicker _due=new DateTimePicker(); private readonly ComboBox _payment=new ComboBox();
        public FinancialEntryEditor(LocalDatabase database, string type) { _database=database; _type=type; Text="Nova conta a "+type.ToLowerInvariant(); StartPosition=FormStartPosition.CenterParent; Size=new Size(500,570); MinimumSize=new Size(500,570); Build(); }
        private void Build()
        {
            _amount.DecimalPlaces=2; _amount.Maximum=1000000; _amount.ThousandsSeparator=true; _amount.Minimum=0.01M; _due.Format=DateTimePickerFormat.Short; _payment.Items.AddRange(new object[]{"Dinheiro","PIX","Débito","Crédito","Transferência","Boleto","Crediário","Outros"}); _payment.Text="PIX"; _party.DropDownStyle=ComboBoxStyle.DropDown; _party.AutoCompleteMode=AutoCompleteMode.SuggestAppend; _party.AutoCompleteSource=AutoCompleteSource.ListItems; LoadParties();
            Add(_type=="Receber" ? "Devedor *" : "Fornecedor *",_party,18); var searchParty=new Button{Text=_type=="Receber"?"Pesquisar devedor":"Pesquisar fornecedor",Location=new Point(25,70),Size=new Size(200,28)};searchParty.Click+=(s,e)=>SearchParty();Controls.Add(searchParty);var newParty=new Button{Text=_type=="Receber"?"Novo devedor":"Novo fornecedor",Location=new Point(235,70),Size=new Size(222,28)};newParty.Click+=(s,e)=>NewParty();Controls.Add(newParty); _category.DisplayMember="display_name";_category.ValueMember="id";_category.DataSource=_database.GetFinancialCategoryLookup();Add("Categoria *",_category,110);_category.Size=new Size(275,24);var newCategory=new Button{Text="Nova categoria",Location=new Point(310,130),Size=new Size(147,27)};newCategory.Click+=(s,e)=>NewCategory();Controls.Add(newCategory); Add("Descrição *",_description,166); Add("Documento / referência",_document,222); Add("Valor (R$) *",_amount,278); Add("Vencimento",_due,334); Add("Forma prevista",_payment,390);
            var save=new Button{Text="Salvar conta",Location=new Point(332,456),Size=new Size(125,32)}; save.Click+=Save; Controls.Add(save);
        }
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(25,y),AutoSize=true});control.Location=new Point(25,y+20);control.Size=new Size(432,24);Controls.Add(control);}
        private void LoadParties(){_party.DataSource=_type=="Receber"?_database.GetDebtorLookup():_database.GetSupplierLookup();_party.DisplayMember="display_name";_party.ValueMember="id";}
        private void SearchParty(){using(var f=new FinancialPartySearchForm(_database,_type)){if(f.ShowDialog(this)==DialogResult.OK){LoadParties();_party.SelectedValue=f.SelectedId;}}}
        private void NewParty(){if(_type=="Receber"){using(var f=new DebtorEditor(_database,null)){if(f.ShowDialog(this)==DialogResult.OK)LoadParties();}}else{using(var f=new SupplierEditor(_database,null)){if(f.ShowDialog(this)==DialogResult.OK)LoadParties();}}}
        private void NewCategory(){using(var f=new FinancialCategoryEditor(_database,null)){if(f.ShowDialog(this)==DialogResult.OK){_category.DataSource=_database.GetFinancialCategoryLookup();_category.DisplayMember="display_name";_category.ValueMember="id";}}}
        private void Save(object sender,EventArgs e){if(_party.SelectedValue==null){MessageBox.Show(_type=="Receber"?"Cadastre e selecione o devedor.":"Cadastre e selecione o fornecedor.");return;}if(_category.SelectedValue==null){MessageBox.Show("Cadastre e selecione uma categoria.");return;}if(string.IsNullOrWhiteSpace(_description.Text)){MessageBox.Show("Informe a descrição da conta.");return;}var selected=_party.SelectedItem as DataRowView;var category=_category.SelectedItem as DataRowView;_database.SaveFinancialEntry(_type,Convert.ToInt64(_party.SelectedValue),selected==null?_party.Text:Convert.ToString(selected["name"]),category==null?_category.Text:Convert.ToString(category["name"]),_description.Text.Trim(),_document.Text.Trim(),_amount.Value,_due.Value.ToString("yyyy-MM-dd"),_payment.Text);DialogResult=DialogResult.OK;}
    }

    internal sealed class FinancialCategoryEditor : Form
    {
        private readonly LocalDatabase _database;private readonly DataGridViewRow _row;private readonly TextBox _name=new TextBox();private readonly CheckBox _active=new CheckBox{Text="Categoria ativa",Checked=true,AutoSize=true};
        public FinancialCategoryEditor(LocalDatabase database,DataGridViewRow row){_database=database;_row=row;Text=row==null?"Nova categoria financeira":"Editar categoria financeira";StartPosition=FormStartPosition.CenterParent;Size=new Size(450,250);Controls.Add(new Label{Text="Nome da categoria *",Location=new Point(25,24),AutoSize=true});_name.Location=new Point(25,46);_name.Size=new Size(380,24);Controls.Add(_name);_active.Location=new Point(25,88);Controls.Add(_active);if(_row!=null){_name.Text=Convert.ToString(_row.Cells["Categoria"].Value);_active.Checked=Convert.ToString(_row.Cells["Status"].Value)=="Ativa";}var save=new Button{Text="Salvar",Location=new Point(285,135),Size=new Size(120,32)};save.Click+=Save;Controls.Add(save);}
        private void Save(object s,EventArgs e){if(string.IsNullOrWhiteSpace(_name.Text)){MessageBox.Show("Informe o nome da categoria.");return;}try{_database.SaveFinancialCategory(_row==null?(long?)null:Convert.ToInt64(_row.Cells["Código"].Value),_name.Text.Trim(),_active.Checked);DialogResult=DialogResult.OK;}catch(Exception){MessageBox.Show("Já existe uma categoria com este nome.");}}
    }

    internal sealed class FinancialPartySearchForm : Form
    {
        private readonly LocalDatabase _database;private readonly string _type;private readonly TextBox _search=new TextBox();private readonly DataGridView _grid=new DataGridView();public long SelectedId{get;private set;}
        public FinancialPartySearchForm(LocalDatabase database,string type){_database=database;_type=type;Text=type=="Receber"?"Pesquisar devedor":"Pesquisar fornecedor";StartPosition=FormStartPosition.CenterParent;Size=new Size(760,500);Build();RefreshResults();}
        private void Build(){Controls.Add(new Label{Text="Digite nome, CPF/CNPJ, telefone ou e-mail:",Location=new Point(18,18),AutoSize=true});_search.Location=new Point(18,40);_search.Size=new Size(490,24);_search.TextChanged+=(s,e)=>RefreshResults();Controls.Add(_search);var search=new Button{Text="Buscar",Location=new Point(520,38),Size=new Size(100,28)};search.Click+=(s,e)=>RefreshResults();Controls.Add(search);_grid.Location=new Point(18,78);_grid.Size=new Size(710,330);_grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AllowUserToDeleteRows=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.MultiSelect=false;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.BackgroundColor=Color.White;_grid.DoubleClick+=(s,e)=>SelectParty();Controls.Add(_grid);var select=new Button{Text="Selecionar",Location=new Point(608,425),Size=new Size(120,32),Anchor=AnchorStyles.Bottom|AnchorStyles.Right};select.Click+=(s,e)=>SelectParty();Controls.Add(select);}
        private void RefreshResults(){_grid.DataSource=_type=="Receber"?_database.FindDebtors(_search.Text.Trim()):_database.FindSuppliers(_search.Text.Trim());}
        private void SelectParty(){if(_grid.CurrentRow==null){MessageBox.Show("Selecione um cadastro.");return;}SelectedId=Convert.ToInt64(_grid.CurrentRow.Cells["Código"].Value);DialogResult=DialogResult.OK;}
    }

    internal sealed class SupplierEditor : Form
    {
        private readonly LocalDatabase _database;private readonly DataGridViewRow _row;private readonly TextBox _name=new TextBox(),_contact=new TextBox(),_document=new TextBox(),_phone=new TextBox(),_email=new TextBox();private readonly CheckBox _active=new CheckBox{Text="Fornecedor ativo",Checked=true,AutoSize=true};
        public SupplierEditor(LocalDatabase database,DataGridViewRow row){_database=database;_row=row;Text=row==null?"Novo fornecedor":"Editar fornecedor";StartPosition=FormStartPosition.CenterParent;Size=new Size(490,470);Build();}
        private void Build(){Add("Razão social / nome *",_name,18);Add("Pessoa de contato",_contact,74);Add("CPF/CNPJ",_document,130);Add("Telefone",_phone,186);Add("E-mail",_email,242);_active.Location=new Point(25,304);Controls.Add(_active);if(_row!=null){_name.Text=Convert.ToString(_row.Cells["Fornecedor"].Value);_contact.Text=Convert.ToString(_row.Cells["Contato"].Value);_document.Text=Convert.ToString(_row.Cells["CPF/CNPJ"].Value);_phone.Text=Convert.ToString(_row.Cells["Telefone"].Value);_email.Text=Convert.ToString(_row.Cells["E-mail"].Value);_active.Checked=Convert.ToString(_row.Cells["Status"].Value)=="Ativo";}var save=new Button{Text="Salvar",Location=new Point(330,350),Size=new Size(120,32)};save.Click+=Save;Controls.Add(save);}
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(25,y),AutoSize=true});control.Location=new Point(25,y+20);control.Size=new Size(425,24);Controls.Add(control);}
        private void Save(object s,EventArgs e){if(string.IsNullOrWhiteSpace(_name.Text)){MessageBox.Show("Informe o nome do fornecedor.");return;}_database.SaveSupplier(_row==null?(long?)null:Convert.ToInt64(_row.Cells["Código"].Value),_name.Text.Trim(),_document.Text.Trim(),_phone.Text.Trim(),_email.Text.Trim(),_contact.Text.Trim(),_active.Checked);DialogResult=DialogResult.OK;}
    }

    internal sealed class DebtorEditor : Form
    {
        private readonly LocalDatabase _database;private readonly DataGridViewRow _row;private readonly TextBox _name=new TextBox(),_document=new TextBox(),_phone=new TextBox(),_email=new TextBox();private readonly CheckBox _active=new CheckBox{Text="Devedor ativo",Checked=true,AutoSize=true};
        public DebtorEditor(LocalDatabase database,DataGridViewRow row){_database=database;_row=row;Text=row==null?"Novo devedor":"Editar devedor";StartPosition=FormStartPosition.CenterParent;Size=new Size(490,420);Build();}
        private void Build(){Add("Nome / razão social *",_name,18);Add("CPF/CNPJ",_document,74);Add("Telefone",_phone,130);Add("E-mail",_email,186);_active.Location=new Point(25,248);Controls.Add(_active);if(_row!=null){_name.Text=Convert.ToString(_row.Cells["Devedor"].Value);_document.Text=Convert.ToString(_row.Cells["CPF/CNPJ"].Value);_phone.Text=Convert.ToString(_row.Cells["Telefone"].Value);_email.Text=Convert.ToString(_row.Cells["E-mail"].Value);_active.Checked=Convert.ToString(_row.Cells["Status"].Value)=="Ativo";}var save=new Button{Text="Salvar",Location=new Point(330,294),Size=new Size(120,32)};save.Click+=Save;Controls.Add(save);}
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(25,y),AutoSize=true});control.Location=new Point(25,y+20);control.Size=new Size(425,24);Controls.Add(control);}
        private void Save(object s,EventArgs e){if(string.IsNullOrWhiteSpace(_name.Text)){MessageBox.Show("Informe o nome do devedor.");return;}_database.SaveDebtor(_row==null?(long?)null:Convert.ToInt64(_row.Cells["Código"].Value),_name.Text.Trim(),_document.Text.Trim(),_phone.Text.Trim(),_email.Text.Trim(),_active.Checked);DialogResult=DialogResult.OK;}
    }

    internal sealed class PdvForm : Form
    {
        private readonly LocalDatabase _database; private readonly TextBox _search = new TextBox(); private readonly ComboBox _found = new ComboBox(); private readonly ComboBox _client = new ComboBox(); private readonly DataGridView _cart = new DataGridView(); private readonly ComboBox _payment = new ComboBox(); private readonly NumericUpDown _discount = new NumericUpDown(); private readonly Label _total = new Label(); private readonly DataTable _items = new DataTable();
        public PdvForm(LocalDatabase database) { _database = database; Text = "PDV — Venda rápida"; StartPosition = FormStartPosition.CenterParent; Size = new Size(920, 700); MinimumSize = new Size(760, 570); Build(); }
        private void Build()
        {
            Controls.Add(new Label { Text = "Produto, código ou código de barras", Location = new Point(20,18), AutoSize=true }); _search.Location = new Point(20,38); _search.Size = new Size(350,24); _search.KeyDown += SearchKey; Controls.Add(_search);
            var find = new Button { Text="Buscar / adicionar", Location=new Point(380,36), Size=new Size(130,28) }; find.Click += (s,e) => SearchAndAdd(); Controls.Add(find);
            _found.Location = new Point(20,72); _found.Size = new Size(490,24); _found.DisplayMember="display_name"; _found.ValueMember="id"; Controls.Add(_found);
            var add = new Button { Text="Adicionar selecionado", Location=new Point(520,70), Size=new Size(145,28) }; add.Click += (s,e) => AddSelected(); Controls.Add(add);
            Controls.Add(new Label { Text = "Cliente (opcional)", Location = new Point(20, 108), AutoSize = true }); _client.Location = new Point(20, 128); _client.Size = new Size(390, 24); _client.DisplayMember = "name"; _client.ValueMember = "id"; Controls.Add(_client); LoadClients();
            var searchClient = new Button { Text = "Pesquisar cliente", Location = new Point(420, 126), Size = new Size(135, 28) }; searchClient.Click += (s, e) => SearchClient(); Controls.Add(searchClient);
            var newClient = new Button { Text = "Novo cliente", Location = new Point(565, 126), Size = new Size(120, 28) }; newClient.Click += (s, e) => NewClient(); Controls.Add(newClient);
            _items.Columns.Add("product_id", typeof(long)); _items.Columns.Add("Descrição", typeof(string)); _items.Columns.Add("Qtd.", typeof(decimal)); _items.Columns.Add("Valor unitário", typeof(decimal)); _items.Columns.Add("Total", typeof(decimal)); _items.Columns.Add("quantity", typeof(decimal)); _items.Columns.Add("unit_price", typeof(decimal)); _items.Columns.Add("description", typeof(string));
            _cart.Location=new Point(20,166); _cart.Size=new Size(850,330); _cart.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right; _cart.ReadOnly=true; _cart.AllowUserToAddRows=false; _cart.AllowUserToDeleteRows=false; _cart.SelectionMode=DataGridViewSelectionMode.FullRowSelect; _cart.MultiSelect=false; _cart.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill; _cart.BackgroundColor=Color.White; Controls.Add(_cart); _cart.DataSource=_items;
            foreach (string hidden in new []{"product_id","quantity","unit_price","description"}) if (_cart.Columns.Contains(hidden)) _cart.Columns[hidden].Visible=false;
            var remove = new Button { Text="Remover item", Location=new Point(20,510), Size=new Size(115,30), Anchor=AnchorStyles.Bottom|AnchorStyles.Left }; remove.Click += (s,e) => { if (_cart.CurrentRow != null) { _items.Rows.RemoveAt(_cart.CurrentRow.Index); RefreshTotal(); } }; Controls.Add(remove);
            Controls.Add(new Label { Text="Pagamento", Location=new Point(440,510), AutoSize=true, Anchor=AnchorStyles.Bottom|AnchorStyles.Right }); _payment.Items.AddRange(new object[]{"Dinheiro","PIX","Débito","Crédito","Transferência","Boleto","Crediário","Outros"}); _payment.Text="PIX"; _payment.Location=new Point(440,530); _payment.Size=new Size(150,24); _payment.Anchor=AnchorStyles.Bottom|AnchorStyles.Right; Controls.Add(_payment);
            Controls.Add(new Label { Text="Desconto (R$)", Location=new Point(600,510), AutoSize=true, Anchor=AnchorStyles.Bottom|AnchorStyles.Right }); _discount.DecimalPlaces=2; _discount.Maximum=1000000; _discount.Location=new Point(600,530); _discount.Size=new Size(120,24); _discount.Anchor=AnchorStyles.Bottom|AnchorStyles.Right; _discount.ValueChanged += (s,e) => RefreshTotal(); Controls.Add(_discount);
            _total.Location=new Point(20,565); _total.AutoSize=true; _total.Font=new Font("Segoe UI",14,FontStyle.Bold); _total.ForeColor=Color.FromArgb(7,55,94); _total.Anchor=AnchorStyles.Bottom|AnchorStyles.Left; Controls.Add(_total); var finish=new Button { Text="Finalizar venda", Location=new Point(710,560), Size=new Size(160,36), Anchor=AnchorStyles.Bottom|AnchorStyles.Right, BackColor=Color.FromArgb(24,189,200), ForeColor=Color.White, FlatStyle=FlatStyle.Flat }; finish.Click += Finish; Controls.Add(finish); RefreshTotal(); _search.Focus();
        }
        private void SearchKey(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress=true; SearchAndAdd(); } }
        private void SearchAndAdd()
        {
            var found = _database.FindProducts(_search.Text.Trim()); if (found.Rows.Count == 0) { MessageBox.Show("Produto não encontrado. Confira o código de barras ou cadastre o produto."); return; }
            _found.DataSource=found; _found.DisplayMember="name"; _found.ValueMember="id"; if (found.Rows.Count == 1) AddSelected(); else _found.Focus();
        }
        private void AddSelected()
        {
            var row = _found.SelectedItem as DataRowView; if (row == null) { MessageBox.Show("Busque e selecione um produto."); return; } long id=Convert.ToInt64(row["id"]); foreach(DataRow item in _items.Rows) if(Convert.ToInt64(item["product_id"])==id) { item["Qtd."]=item["quantity"]=Convert.ToDecimal(item["quantity"])+1; item["Total"]=Convert.ToDecimal(item["quantity"])*Convert.ToDecimal(item["unit_price"]); ClearSearch(); RefreshTotal(); return; } var price=Convert.ToDecimal(row["sale_price"]); var added=_items.NewRow(); added["product_id"]=id; added["Descrição"]=added["description"]=Convert.ToString(row["name"]); added["Qtd."]=added["quantity"]=1M; added["Valor unitário"]=added["unit_price"]=price; added["Total"]=price; _items.Rows.Add(added); ClearSearch(); RefreshTotal();
        }
        private void ClearSearch() { _search.Text=""; _found.DataSource=null; _search.Focus(); }
        private void RefreshTotal() { decimal sum=0; foreach(DataRow r in _items.Rows) sum += Convert.ToDecimal(r["Total"]); _total.Text="Total da venda: " + Math.Max(0,sum-_discount.Value).ToString("C2"); }
        private void LoadClients() { _client.DataSource = _database.GetSaleClientLookup(); _client.DisplayMember = "name"; _client.ValueMember = "id"; }
        private void NewClient() { using (var form = new ClientEditor(_database, null)) { if (form.ShowDialog(this) == DialogResult.OK) LoadClients(); } }
        private void SearchClient() { using (var form = new ClientSearchForm(_database)) { if (form.ShowDialog(this) == DialogResult.OK) { LoadClients(); _client.SelectedValue = form.SelectedId; } } }
        private void Finish(object sender, EventArgs e) { if (_items.Rows.Count==0) { MessageBox.Show("Inclua ao menos um produto no carrinho."); return; } long? clientId = null; if (_client.SelectedValue != null && Convert.ToInt64(_client.SelectedValue) > 0) clientId = Convert.ToInt64(_client.SelectedValue); var number=_database.NextSaleNumber(); try { _database.SaveSale(number,_items,_payment.Text,_discount.Value,clientId); } catch(Exception ex) { MessageBox.Show(ex.Message); return; } MessageBox.Show("Venda " + number + " finalizada com sucesso."); using(var receipt=new SaleReceiptForm(number,_items,_payment.Text,_discount.Value)) receipt.ShowDialog(this); DialogResult=DialogResult.OK; }
    }

    internal sealed class ClientSearchForm : Form
    {
        private readonly LocalDatabase _database; private readonly TextBox _search = new TextBox(); private readonly DataGridView _grid = new DataGridView(); public long SelectedId { get; private set; }
        public ClientSearchForm(LocalDatabase database) { _database = database; Text = "Pesquisar cliente"; StartPosition = FormStartPosition.CenterParent; Size = new Size(760, 500); Build(); RefreshResults(); }
        private void Build() { Controls.Add(new Label { Text = "Digite nome, CPF/CNPJ, telefone ou e-mail:", Location = new Point(18, 18), AutoSize = true }); _search.Location = new Point(18, 40); _search.Size = new Size(490, 24); _search.TextChanged += (s, e) => RefreshResults(); Controls.Add(_search); var search = new Button { Text = "Buscar", Location = new Point(520, 38), Size = new Size(100, 28) }; search.Click += (s, e) => RefreshResults(); Controls.Add(search); _grid.Location = new Point(18, 78); _grid.Size = new Size(710, 330); _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right; _grid.ReadOnly = true; _grid.AllowUserToAddRows = false; _grid.AllowUserToDeleteRows = false; _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect; _grid.MultiSelect = false; _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; _grid.BackgroundColor = Color.White; _grid.DoubleClick += (s, e) => SelectClient(); Controls.Add(_grid); var select = new Button { Text = "Selecionar", Location = new Point(608, 425), Size = new Size(120, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Right }; select.Click += (s, e) => SelectClient(); Controls.Add(select); }
        private void RefreshResults() { _grid.DataSource = _database.FindClients(_search.Text.Trim()); }
        private void SelectClient() { if (_grid.CurrentRow == null) { MessageBox.Show("Selecione um cliente."); return; } SelectedId = Convert.ToInt64(_grid.CurrentRow.Cells["Código"].Value); DialogResult = DialogResult.OK; }
    }

    internal sealed class SaleReceiptForm : Form
    { private readonly WebBrowser _browser=new WebBrowser(); public SaleReceiptForm(string number,DataTable items,string payment,decimal discount){Text="Recibo da venda";StartPosition=FormStartPosition.CenterParent;Size=new Size(620,600);var print=new Button{Text="Imprimir / Salvar PDF",Location=new Point(18,14),Size=new Size(150,32)};print.Click+=(s,e)=>_browser.ShowPrintDialog();Controls.Add(print);_browser.Location=new Point(18,56);_browser.Size=new Size(565,490);_browser.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;decimal total=0;string lines="";foreach(DataRow r in items.Rows){var value=Convert.ToDecimal(r["Total"]);total+=value;lines+="<tr><td>"+System.Web.HttpUtility.HtmlEncode(Convert.ToString(r["Descrição"]))+"</td><td>"+r["Qtd."]+"</td><td>"+value.ToString("C2")+"</td></tr>";}_browser.DocumentText="<html><body style='font-family:Arial;width:300px'><h2>RMC Flow Comércio Local</h2><b>RECIBO DE VENDA</b><p>"+number+"</p><table border='1' cellspacing='0' cellpadding='5' style='border-collapse:collapse;width:100%'><tr><th>Item</th><th>Qtd.</th><th>Total</th></tr>"+lines+"</table><p>Pagamento: "+payment+"</p><h3>Total: "+Math.Max(0,total-discount).ToString("C2")+"</h3><p>Obrigado pela preferência!</p></body></html>";Controls.Add(_browser);} }

    internal sealed class ClientEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row; private readonly TextBox _name = Field(); private readonly TextBox _document = Field(); private readonly TextBox _phone = Field(); private readonly TextBox _email = Field();
        public ClientEditor(LocalDatabase database, DataGridViewRow row) { _database = database; _row = row; Text = row == null ? "Novo cliente" : "Editar cliente"; StartPosition = FormStartPosition.CenterParent; Size = new Size(470, 340); Build(); }
        private void Build() { Add("Nome *", _name, 22); Add("CPF/CNPJ", _document, 82); Add("Telefone", _phone, 142); Add("E-mail", _email, 202); if (_row != null) { _name.Text = Convert.ToString(_row.Cells["Nome"].Value); _document.Text = Convert.ToString(_row.Cells["CPF/CNPJ"].Value); _phone.Text = Convert.ToString(_row.Cells["Telefone"].Value); _email.Text = Convert.ToString(_row.Cells["E-mail"].Value); } var save = new Button { Text = "Salvar", Location = new Point(300, 260), Size = new Size(120, 32) }; save.Click += Save; Controls.Add(save); }
        private void Add(string label, TextBox box, int y) { Controls.Add(new Label { Text = label, Location = new Point(25, y), AutoSize = true }); box.Location = new Point(25, y + 20); box.Size = new Size(395, 24); Controls.Add(box); }
        private void Save(object sender, EventArgs e) { if (string.IsNullOrWhiteSpace(_name.Text)) { MessageBox.Show("Informe o nome do cliente."); return; } var phone = PhoneFormatter.Format(_phone.Text); if (!string.IsNullOrWhiteSpace(_phone.Text) && phone == null) { MessageBox.Show("Informe um telefone brasileiro válido com DDD."); return; } _database.SaveClient(_row == null ? (long?)null : Convert.ToInt64(_row.Cells["Código"].Value), _name.Text.Trim(), _document.Text.Trim(), phone ?? string.Empty, _email.Text.Trim()); DialogResult = DialogResult.OK; }
        private static TextBox Field() { return new TextBox(); }
    }

    internal static class LookupControls
    {
        public static void EnableTyping(ComboBox combo)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            combo.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        public static void SelectById(ComboBox combo, long id)
        {
            for (var index = 0; index < combo.Items.Count; index++)
            {
                var row = combo.Items[index] as DataRowView;
                if (row != null && Convert.ToInt64(row["id"]) == id)
                {
                    combo.SelectedIndex = index;
                    return;
                }
            }
        }
    }

    internal static class PhoneFormatter
    {
        public static string Format(string input)
        {
            var digits = Digits(input); if (digits.StartsWith("55") && (digits.Length == 12 || digits.Length == 13)) digits = digits.Substring(2); if (digits.StartsWith("0") && (digits.Length == 11 || digits.Length == 12)) digits = digits.Substring(1); if (digits.Length != 10 && digits.Length != 11) return null;
            var ddd = digits.Substring(0, 2); var number = digits.Substring(2); return number.Length == 9 ? "(" + ddd + ") " + number.Substring(0, 5) + "-" + number.Substring(5) : "(" + ddd + ") " + number.Substring(0, 4) + "-" + number.Substring(4);
        }
        public static string ToWhatsApp(string input) { var formatted = Format(input); return formatted == null ? null : "55" + Digits(formatted); }
        private static string Digits(string value) { var result = ""; foreach (var c in value ?? "") if (c >= '0' && c <= '9') result += c; return result; }
    }

    internal sealed class VehicleEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row; private readonly ComboBox _client = new ComboBox(); private readonly ComboBox _type = new ComboBox(); private readonly TextBox _plate = new TextBox(); private readonly TextBox _brand = new TextBox(); private readonly TextBox _model = new TextBox(); private readonly TextBox _year = new TextBox();
        public long SavedId { get; private set; }
        public VehicleEditor(LocalDatabase database, DataGridViewRow row, long? selectedClientId = null) { _database = database; _row = row; Text = row == null ? "Novo equipamento" : "Editar equipamento"; StartPosition = FormStartPosition.CenterParent; Size = new Size(470, 450); Build(); if (selectedClientId.HasValue) _client.SelectedValue = selectedClientId.Value; }
        private void Build() { _client.DataSource = _database.GetClientLookup(); _client.DisplayMember = "name"; _client.ValueMember = "id"; LookupControls.EnableTyping(_client); _type.Items.AddRange(new object[] { "Moto", "Carro", "Outro" }); Add("Cliente *", _client, 20); _client.Size=new Size(255,24); var searchClient=new Button{Text="Pesquisar cliente",Location=new Point(290,39),Size=new Size(130,27)};searchClient.Click+=(s,e)=>SearchClient();Controls.Add(searchClient); Add("Tipo *", _type, 78); Add("Identificador", _plate, 136); Add("Marca", _brand, 194); Add("Modelo *", _model, 252); Add("Versão", _year, 310); if (_row != null) { _type.Text = Convert.ToString(_row.Cells["Tipo"].Value); _plate.Text = Convert.ToString(_row.Cells["Identificador"].Value); _brand.Text = Convert.ToString(_row.Cells["Marca"].Value); _model.Text = Convert.ToString(_row.Cells["Modelo"].Value); _year.Text = Convert.ToString(_row.Cells["Versão"].Value); _client.Text = Convert.ToString(_row.Cells["Cliente"].Value); } var save = new Button { Text = "Salvar", Location = new Point(300, 370), Size = new Size(120, 32) }; save.Click += Save; Controls.Add(save); }
        private void SearchClient(){using(var form=new ClientSearchForm(_database)){if(form.ShowDialog(this)==DialogResult.OK){_client.DataSource=_database.GetClientLookup();_client.DisplayMember="name";_client.ValueMember="id";_client.SelectedValue=form.SelectedId;}}}
        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(25, y), AutoSize = true }); control.Location = new Point(25, y + 20); control.Size = new Size(395, 24); Controls.Add(control); }
        private void Save(object sender, EventArgs e) { if (_client.SelectedValue == null || string.IsNullOrWhiteSpace(_type.Text) || string.IsNullOrWhiteSpace(_model.Text)) { MessageBox.Show("Informe cliente, tipo e modelo."); return; } SavedId = _database.SaveVehicle(_row == null ? (long?)null : Convert.ToInt64(_row.Cells["Código"].Value), Convert.ToInt64(_client.SelectedValue), _type.Text, _plate.Text.Trim(), _brand.Text.Trim(), _model.Text.Trim(), _year.Text.Trim()); DialogResult = DialogResult.OK; }
    }

    internal sealed class CompanySettingsForm : Form
    {
        private readonly LocalDatabase _database;private readonly TextBox _tradeName=new TextBox();private readonly TextBox _legalName=new TextBox();private readonly TextBox _document=new TextBox();private readonly TextBox _phone=new TextBox();private readonly TextBox _email=new TextBox();private readonly TextBox _address=new TextBox();
        public CompanySettingsForm(LocalDatabase database){_database=database;Text="Dados da empresa";StartPosition=FormStartPosition.CenterParent;Size=new Size(540,510);MinimumSize=new Size(540,510);Build();}
        private void Build(){Add("Nome fantasia *",_tradeName,22);Add("Razão social",_legalName,78);Add("CNPJ / CPF",_document,134);Add("Telefone / WhatsApp",_phone,190);Add("E-mail",_email,246);Add("Endereço",_address,302);_tradeName.Text=_database.Get("company_trade_name")??"";_legalName.Text=_database.Get("company_legal_name")??"";_document.Text=_database.Get("company_document")??"";_phone.Text=_database.Get("company_phone")??"";_email.Text=_database.Get("company_email")??"";_address.Text=_database.Get("company_address")??"";var save=new Button{Text="Salvar dados",Location=new Point(350,405),Size=new Size(140,34)};save.Click+=Save;Controls.Add(save);}
        private void Add(string label,TextBox field,int y){Controls.Add(new Label{Text=label,Location=new Point(25,y),AutoSize=true});field.Location=new Point(25,y+20);field.Size=new Size(465,24);Controls.Add(field);}
        private void Save(object sender,EventArgs e){if(string.IsNullOrWhiteSpace(_tradeName.Text)){MessageBox.Show("Informe o nome fantasia da empresa.");return;}_database.Set("company_trade_name",_tradeName.Text.Trim());_database.Set("company_legal_name",_legalName.Text.Trim());_database.Set("company_document",_document.Text.Trim());_database.Set("company_phone",_phone.Text.Trim());_database.Set("company_email",_email.Text.Trim());_database.Set("company_address",_address.Text.Trim());DialogResult=DialogResult.OK;}
    }

    internal sealed class WarrantyForm : Form
    {
        private readonly LocalDatabase _database;private readonly DataGridView _grid=new DataGridView();
        public WarrantyForm(LocalDatabase database){_database=database;Text="Consulta de garantias";StartPosition=FormStartPosition.CenterParent;Size=new Size(1000,560);MinimumSize=new Size(760,460);Build();}
        private void Build(){Controls.Add(new Label{Text="Garantias de Ordens de Serviço finalizadas",Location=new Point(20,18),AutoSize=true,Font=new Font("Segoe UI",14,FontStyle.Bold),ForeColor=Color.FromArgb(7,55,94)});var update=new Button{Text="Atualizar",Location=new Point(850,14),Size=new Size(110,30)};update.Click+=(s,e)=>RefreshData();Controls.Add(update);var note=new Button{Text="Nota de garantia",Location=new Point(650,14),Size=new Size(160,30)};note.Click+=(sender,e)=>{if(_grid.CurrentRow==null){MessageBox.Show("Selecione uma OS.");return;}try{using(var form=new WarrantyDocumentForm(_database,Convert.ToInt64(_grid.CurrentRow.Cells["Código"].Value)))form.ShowDialog(this);}catch(Exception ex){MessageBox.Show(ex.Message);}};Controls.Add(note);_grid.Location=new Point(20,58);_grid.Size=new Size(940,430);_grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AllowUserToDeleteRows=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.BackgroundColor=Color.White;Controls.Add(_grid);RefreshData();}
        private void RefreshData(){_grid.DataSource=_database.GetWarrantyOrders();if(_grid.Columns.Contains("Código"))_grid.Columns["Código"].Visible=false;}
    }

    internal sealed class WarrantyDocumentForm : Form
    {
        private static string H(object value){return System.Web.HttpUtility.HtmlEncode(Convert.ToString(value));}
        public WarrantyDocumentForm(LocalDatabase db,long id)
        {
            var data=db.GetWorkOrderDocument(id);if(data.Rows.Count==0)throw new InvalidOperationException("OS não encontrada.");var order=data.Rows[0];DateTime start;
            if(Convert.ToString(order["status"])!="Finalizada"||!DateTime.TryParse(Convert.ToString(order["finalized_at"]),out start)||Convert.ToInt32(order["warranty_days"])<=0)throw new InvalidOperationException("Finalize a OS e informe o prazo de garantia antes de emitir a nota.");
            Text="Nota de garantia — "+Convert.ToString(order["number"]);Size=new Size(900,700);StartPosition=FormStartPosition.CenterParent;
            var browser=new WebBrowser{Dock=DockStyle.Fill,AllowWebBrowserDrop=false};var toolbar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=48,Padding=new Padding(8)};var print=new Button{Text="Imprimir / Salvar PDF",Width=180,Height=30,Enabled=false};toolbar.Controls.Add(print);print.Click+=(sender,e)=>browser.ShowPrintDialog();browser.DocumentCompleted+=(sender,e)=>print.Enabled=true;Controls.Add(browser);Controls.Add(toolbar);
            var lines="";foreach(DataRow item in db.GetWorkOrderItems(id).Rows)lines+="<tr><td>"+H(item["Tipo"])+"</td><td>"+H(item["Descrição"])+"</td><td>"+H(item["Qtd."])+"</td></tr>";
            var days=Convert.ToInt32(order["warranty_days"]);
            var companyName=db.Get("company_trade_name");if(string.IsNullOrWhiteSpace(companyName))companyName=db.Get("company_legal_name");if(string.IsNullOrWhiteSpace(companyName))companyName="Empresa";
            if(string.IsNullOrWhiteSpace(lines))lines="<tr><td colspan='3'>Nenhuma peça/produto foi utilizado nesta OS.</td></tr>";
            browser.DocumentText="<html><head><meta charset='utf-8'><style>body{font:14px Arial;margin:32px;color:#17304a}h1{font-size:24px}h2{font-size:18px;border-bottom:1px solid #aaa;padding-bottom:8px}table{border-collapse:collapse;width:100%}td,th{border:1px solid #bbb;padding:8px;text-align:left}tr{page-break-inside:avoid}</style></head><body><h1>"+H(companyName)+"</h1><p>"+H(db.Get("company_document"))+"<br>"+H(db.Get("company_address"))+"<br>"+H(db.Get("company_phone"))+"</p><h2>Nota de garantia de serviços - "+H(order["number"])+"</h2><p><b>Cliente:</b> "+H(order["client_name"])+"<br><b>Documento:</b> "+H(order["client_document"])+"<br><b>Equipamento:</b> "+H(order["vehicle_type"])+" "+H(order["manufacturer"])+" "+H(order["model"])+"<br><b>Identificador:</b> "+H(order["plate"])+"</p><h2>Serviço executado e itens registrados na OS</h2><table><tr><th>Tipo</th><th>Descrição</th><th>Quantidade</th></tr>"+lines+"</table><h2>Prazo informado pela empresa</h2><p>Finalização: "+start.ToString("dd/MM/yyyy")+"<br>Prazo cadastrado: "+days+" dias<br>Data final calculada: "+start.AddDays(days).ToString("dd/MM/yyyy")+"</p><p><b>Diagnóstico/serviço executado:</b> "+H(order["diagnosis"])+"</p><p><b>Observações da OS:</b> "+H(order["notes"])+"</p><p>Documento vinculado à ordem de serviço. Não é nota fiscal.</p><br><p>Assinatura da empresa: ___________________________________</p><br><p>Assinatura do cliente: ____________________________________</p></body></html>";
        }
    }

    internal sealed class VehicleHistoryForm : Form
    {
        private readonly LocalDatabase _database; private readonly long _vehicleId; private readonly DataGridView _grid=new DataGridView();
        public VehicleHistoryForm(LocalDatabase database,long vehicleId){_database=database;_vehicleId=vehicleId;Text="Histórico do equipamento";StartPosition=FormStartPosition.CenterParent;Size=new Size(980,580);MinimumSize=new Size(760,460);Build();}
        private void Build(){var data=_database.GetVehicleHistoryHeader(_vehicleId);if(data.Rows.Count==0){MessageBox.Show("Equipamento não encontrado.");Close();return;}var item=data.Rows[0];var vehicle=(Convert.ToString(item["type"])+" "+Convert.ToString(item["manufacturer"])+" "+Convert.ToString(item["model"])).Trim();Controls.Add(new Label{Text="Histórico de atendimentos",Location=new Point(20,18),AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold),ForeColor=Color.FromArgb(7,55,94)});Controls.Add(new Label{Text="Cliente: "+Convert.ToString(item["client_name"])+"\nEquipamento: "+vehicle+"  "+Convert.ToString(item["year_model"])+"   Identificador: "+Convert.ToString(item["plate"]),Location=new Point(22,52),Size=new Size(860,44),ForeColor=Color.FromArgb(53,73,94)});_grid.Location=new Point(20,112);_grid.Size=new Size(920,390);_grid.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;_grid.ReadOnly=true;_grid.AllowUserToAddRows=false;_grid.AllowUserToDeleteRows=false;_grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_grid.MultiSelect=false;_grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;_grid.BackgroundColor=Color.White;_grid.DataSource=_database.GetVehicleHistory(_vehicleId);if(_grid.Columns.Contains("Código"))_grid.Columns["Código"].Visible=false;if(_grid.Columns.Contains("Serviços e peças"))_grid.Columns["Serviços e peças"].DefaultCellStyle.WrapMode=DataGridViewTriState.True;Controls.Add(_grid);}
    }

    internal sealed class AppointmentEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row;
        private readonly ComboBox _client = new ComboBox(); private readonly ComboBox _vehicle = new ComboBox(); private readonly ComboBox _status = new ComboBox(); private readonly DateTimePicker _scheduled = new DateTimePicker(); private readonly TextBox _service = new TextBox(); private readonly TextBox _notes = new TextBox(); private bool _changing;
        private sealed class Option { public long Id { get; private set; } public string Text { get; private set; } public Option(long id,string text){Id=id;Text=text;} public override string ToString(){return Text;} }
        public AppointmentEditor(LocalDatabase database, DataGridViewRow row) { _database=database; _row=row; Text=row==null?"Novo agendamento":"Editar agendamento"; StartPosition=FormStartPosition.CenterParent; Size=new Size(620,530); MinimumSize=new Size(620,530); Build(); }
        private void Build()
        {
            _client.DropDownStyle=ComboBoxStyle.DropDownList; _vehicle.DropDownStyle=ComboBoxStyle.DropDownList; _client.SelectedIndexChanged+=(s,e)=>{if(!_changing)LoadVehicles(null);}; _status.Items.AddRange(new object[]{"Agendado","Confirmado","Cancelado"});_status.Text="Agendado"; _scheduled.Format=DateTimePickerFormat.Custom;_scheduled.CustomFormat="dd/MM/yyyy HH:mm";_scheduled.ShowUpDown=true;
            Add("Cliente *",_client,18);_client.Size=new Size(315,24);var searchClient=new Button{Text="Pesquisar cliente",Location=new Point(350,37),Size=new Size(125,27)};searchClient.Click+=(s,e)=>SearchClient();Controls.Add(searchClient);var newClient=new Button{Text="Novo cliente",Location=new Point(485,37),Size=new Size(85,27)};newClient.Click+=(s,e)=>NewClient();Controls.Add(newClient);
            Add("Equipamento *",_vehicle,74); Add("Data e hora *",_scheduled,130); Add("Status *",_status,186); Add("Serviço previsto *",_service,242); AddLong("Observações",_notes,298);
            long? clientId=null,vehicleId=null;if(_row!=null){var data=_database.GetAppointmentForEdit(Convert.ToInt64(_row.Cells["Código"].Value));if(data.Rows.Count==0){MessageBox.Show("Agendamento não encontrado.");Close();return;}var item=data.Rows[0];clientId=Convert.ToInt64(item["client_id"]);vehicleId=Convert.ToInt64(item["vehicle_id"]);_status.Text=Convert.ToString(item["status"]);DateTime value;if(DateTime.TryParse(Convert.ToString(item["scheduled_at"]),out value))_scheduled.Value=value;_service.Text=Convert.ToString(item["service_description"]);_notes.Text=Convert.ToString(item["notes"]);}LoadClients(clientId);LoadVehicles(vehicleId);
            var save=new Button{Text="Salvar agendamento",Location=new Point(390,440),Size=new Size(180,34)};save.Click+=Save;Controls.Add(save);
        }
        private void Add(string label,Control control,int y){Controls.Add(new Label{Text=label,Location=new Point(22,y),AutoSize=true});control.Location=new Point(22,y+19);control.Size=new Size(548,24);Controls.Add(control);}
        private void AddLong(string label,TextBox text,int y){Controls.Add(new Label{Text=label,Location=new Point(22,y),AutoSize=true});text.Location=new Point(22,y+19);text.Size=new Size(548,78);text.Multiline=true;text.ScrollBars=ScrollBars.Vertical;Controls.Add(text);}
        private static long? SelectedId(ComboBox combo){var option=combo.SelectedItem as Option;return option==null?(long?)null:option.Id;}
        private static void SelectId(ComboBox combo,long? id){if(!id.HasValue)return;for(var index=0;index<combo.Items.Count;index++){var option=combo.Items[index] as Option;if(option!=null&&option.Id==id.Value){combo.SelectedIndex=index;return;}}}
        private void LoadClients(long? selectedId){_changing=true;_client.Items.Clear();var clients=_database.GetClientLookup();foreach(DataRow client in clients.Rows)_client.Items.Add(new Option(Convert.ToInt64(client["id"]),Convert.ToString(client["name"])));SelectId(_client,selectedId);if(_client.SelectedIndex<0&&_client.Items.Count>0)_client.SelectedIndex=0;_changing=false;}
        private void LoadVehicles(long? selectedId){_changing=true;_vehicle.Items.Clear();var clientId=SelectedId(_client);if(clientId.HasValue){var vehicles=_database.GetVehicleLookupByClient(clientId.Value);foreach(DataRow vehicle in vehicles.Rows)_vehicle.Items.Add(new Option(Convert.ToInt64(vehicle["id"]),Convert.ToString(vehicle["display_name"])));}SelectId(_vehicle,selectedId);if(_vehicle.SelectedIndex<0&&_vehicle.Items.Count>0)_vehicle.SelectedIndex=0;_changing=false;}
        private void SearchClient(){using(var form=new ClientSearchForm(_database)){if(form.ShowDialog(this)==DialogResult.OK){LoadClients(form.SelectedId);LoadVehicles(null);}}}
        private void NewClient(){using(var form=new ClientEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK){LoadClients(SelectedId(_client));LoadVehicles(SelectedId(_vehicle));}}}
        private void Save(object sender,EventArgs e){var clientId=SelectedId(_client);var vehicleId=SelectedId(_vehicle);if(!clientId.HasValue||!vehicleId.HasValue||string.IsNullOrWhiteSpace(_service.Text)){MessageBox.Show("Informe cliente, equipamento e serviço previsto.");return;}if(!_database.VehicleBelongsToClient(vehicleId.Value,clientId.Value)){MessageBox.Show("O equipamento selecionado não pertence ao cliente escolhido.");return;}_database.SaveAppointment(_row==null?(long?)null:Convert.ToInt64(_row.Cells["Código"].Value),clientId.Value,vehicleId.Value,_scheduled.Value,_service.Text.Trim(),_notes.Text.Trim(),_status.Text);DialogResult=DialogResult.OK;}
    }

    internal sealed class WorkOrderEditor : Form
    {
        private readonly LocalDatabase _database; private readonly DataGridViewRow _row;
        private readonly ComboBox _client = new ComboBox(); private readonly ComboBox _vehicle = new ComboBox(); private readonly ComboBox _status = new ComboBox();
        private readonly DateTimePicker _expected = new DateTimePicker(); private readonly NumericUpDown _warrantyDays = new NumericUpDown(); private readonly TextBox _mileage = new TextBox(); private readonly TextBox _fuel = new TextBox(); private readonly TextBox _complaint = new TextBox(); private readonly TextBox _diagnosis = new TextBox(); private readonly TextBox _notes = new TextBox();
        private long? _loadedClientId; private long? _loadedVehicleId;
        private bool _changingLookup;

        private sealed class LookupOption
        {
            public long Id { get; private set; }
            public string Text { get; private set; }
            public LookupOption(long id, string text) { Id = id; Text = text; }
            public override string ToString() { return Text; }
        }

        public WorkOrderEditor(LocalDatabase database, DataGridViewRow row)
        {
            _database = database; _row = row; Text = row == null ? "Nova ordem de serviço" : "Editar ordem de serviço"; StartPosition = FormStartPosition.CenterParent; Size = new Size(620, 700); MinimumSize = new Size(620, 700); Build();
        }

        private void Build()
        {
            _client.DropDownStyle = ComboBoxStyle.DropDownList; _vehicle.DropDownStyle = ComboBoxStyle.DropDownList;
            _client.SelectedIndexChanged += (s,e) => { if (!_changingLookup) LoadVehicles(null); };
            _status.Items.AddRange(new object[] { "Aberta", "Em diagnóstico", "Aguardando orçamento", "Aguardando aprovação", "Aprovada", "Em manutenção", "Aguardando peça", "Serviço concluído", "Aguardando pagamento", "Pronta para retirada", "Finalizada", "Cancelada" }); _status.Text = "Aberta";
            _expected.Format = DateTimePickerFormat.Short; _expected.ShowCheckBox = true; _expected.Checked = false;
            _warrantyDays.Minimum=0;_warrantyDays.Maximum=3650;_warrantyDays.DecimalPlaces=0;Add("Cliente *", _client, 18); _client.Size=new Size(320,24); var findClient=new Button{Text="Pesquisar cliente",Location=new Point(355,37),Size=new Size(120,27)};findClient.Click+=(s,e)=>SearchClient();Controls.Add(findClient);var newClient=new Button{Text="Novo cliente",Location=new Point(485,37),Size=new Size(85,27)};newClient.Click+=(s,e)=>NewClient();Controls.Add(newClient); Add("Equipamento *", _vehicle, 72); _vehicle.Size=new Size(390,24);var newVehicle=new Button{Text="Novo equipamento",Location=new Point(425,91),Size=new Size(145,27)};newVehicle.Click+=(s,e)=>NewVehicle();Controls.Add(newVehicle); Add("Status *", _status, 126); Add("Previsão de entrega", _expected, 180);_expected.Size=new Size(250,24);Controls.Add(new Label{Text="Garantia (dias)",Location=new Point(300,180),AutoSize=true});_warrantyDays.Location=new Point(300,199);_warrantyDays.Size=new Size(130,24);Controls.Add(_warrantyDays); Add("Estado de entrada", _mileage, 234); Add("Acessórios recebidos", _fuel, 288);
            AddLong("Problema relatado *", _complaint, 342); AddLong("Diagnóstico", _diagnosis, 424); AddLong("Observações", _notes, 506);
            if (_row != null)
            {
                var data = _database.GetWorkOrderForEdit(Convert.ToInt64(_row.Cells["Código"].Value));
                if (data.Rows.Count == 0) { MessageBox.Show("Ordem de Serviço não encontrada."); Close(); return; }
                var order = data.Rows[0];
                _loadedClientId=Convert.ToInt64(order["client_id"]); _loadedVehicleId=Convert.ToInt64(order["vehicle_id"]); _status.Text = Convert.ToString(order["status"]); DateTime date; if (DateTime.TryParse(Convert.ToString(order["expected_at"]), out date)) { _expected.Value = date; _expected.Checked = true; } _warrantyDays.Value=Convert.ToDecimal(order["warranty_days"]); _mileage.Text = Convert.ToString(order["mileage"]); _fuel.Text = Convert.ToString(order["fuel_level"]); _complaint.Text = Convert.ToString(order["complaint"]); _diagnosis.Text = Convert.ToString(order["diagnosis"]); _notes.Text = Convert.ToString(order["notes"]);
            }
            LoadClients(_loadedClientId);
            LoadVehicles(_loadedVehicleId);
            var save = new Button { Text = "Salvar OS", Location = new Point(440, 608), Size = new Size(130, 34) }; save.Click += Save; Controls.Add(save);
        }

        private void Add(string label, Control control, int y) { Controls.Add(new Label { Text = label, Location = new Point(22, y), AutoSize = true }); control.Location = new Point(22, y + 19); control.Size = new Size(548, 24); Controls.Add(control); }
        private void AddLong(string label, TextBox text, int y) { Controls.Add(new Label { Text = label, Location = new Point(22, y), AutoSize = true }); text.Location = new Point(22, y + 19); text.Size = new Size(548, 52); text.Multiline = true; text.ScrollBars = ScrollBars.Vertical; Controls.Add(text); }
        private static long? SelectedId(ComboBox combo) { var option = combo.SelectedItem as LookupOption; return option == null ? (long?)null : option.Id; }
        private static void SelectId(ComboBox combo, long? id) { if (!id.HasValue) return; for (var index = 0; index < combo.Items.Count; index++) { var option = combo.Items[index] as LookupOption; if (option != null && option.Id == id.Value) { combo.SelectedIndex = index; return; } } }
        private void LoadClients(long? selectedId)
        {
            _changingLookup = true; _client.Items.Clear(); var clients = _database.GetClientLookup();
            foreach (DataRow client in clients.Rows) _client.Items.Add(new LookupOption(Convert.ToInt64(client["id"]), Convert.ToString(client["name"])));
            SelectId(_client, selectedId); if (_client.SelectedIndex < 0 && _client.Items.Count > 0) _client.SelectedIndex = 0; _changingLookup = false;
        }
        private void LoadVehicles(long? selectedId)
        {
            _changingLookup = true; _vehicle.Items.Clear(); var clientId = SelectedId(_client);
            if (clientId.HasValue) { var vehicles = _database.GetVehicleLookupByClient(clientId.Value); foreach (DataRow vehicle in vehicles.Rows) _vehicle.Items.Add(new LookupOption(Convert.ToInt64(vehicle["id"]), Convert.ToString(vehicle["display_name"]))); }
            SelectId(_vehicle, selectedId); if (_vehicle.SelectedIndex < 0 && _vehicle.Items.Count > 0) _vehicle.SelectedIndex = 0; _changingLookup = false;
        }
        private void SearchClient(){using(var form=new ClientSearchForm(_database)){if(form.ShowDialog(this)==DialogResult.OK){LoadClients(form.SelectedId);LoadVehicles(null);}}}
        private void NewClient(){using(var form=new ClientEditor(_database,null)){if(form.ShowDialog(this)==DialogResult.OK){var selectedClient=SelectedId(_client);var selectedVehicle=SelectedId(_vehicle);LoadClients(selectedClient);LoadVehicles(selectedVehicle);}}}
        private void NewVehicle(){var clientId=SelectedId(_client);if(!clientId.HasValue){MessageBox.Show("Selecione primeiro o cliente.");return;}using(var form=new VehicleEditor(_database,null,clientId)){if(form.ShowDialog(this)==DialogResult.OK)LoadVehicles(form.SavedId);}}
        private void Save(object sender, EventArgs e)
        {
            var clientId = SelectedId(_client); var vehicleId = SelectedId(_vehicle);
            if (!clientId.HasValue || !vehicleId.HasValue || string.IsNullOrWhiteSpace(_status.Text) || string.IsNullOrWhiteSpace(_complaint.Text)) { MessageBox.Show("Informe cliente, equipamento, status e problema relatado."); return; }
            if (!_database.VehicleBelongsToClient(vehicleId.Value, clientId.Value)) { MessageBox.Show("O equipamento selecionado não pertence ao cliente escolhido."); return; }
            var id = _row == null ? (long?)null : Convert.ToInt64(_row.Cells["Código"].Value); var number = _row == null ? _database.NextWorkOrderNumber() : Convert.ToString(_row.Cells["OS"].Value);
            _database.SaveWorkOrder(id, number, clientId.Value, vehicleId.Value, _status.Text, _expected.Checked ? _expected.Value.ToString("dd/MM/yyyy") : string.Empty, _mileage.Text.Trim(), _fuel.Text.Trim(), _complaint.Text.Trim(), _diagnosis.Text.Trim(), _notes.Text.Trim(), Convert.ToInt32(_warrantyDays.Value));
            DialogResult = DialogResult.OK;
        }
    }
}
