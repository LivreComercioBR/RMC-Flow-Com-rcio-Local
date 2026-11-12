using System;
using System.Drawing;
using System.Windows.Forms;
using RMCFlowComercioLocal.Infrastructure;
namespace RMCFlowComercioLocal
{
    internal sealed class PricingFields : UserControl
    {
        private readonly NumericUpDown[] _fields=new NumericUpDown[9];
        private readonly Label _summary=new Label();
        public event EventHandler ApplyPrice;
        public decimal SuggestedPrice {get;private set;}
        public decimal[] Values {get {var v=new decimal[9];for(int i=0;i<9;i++)v[i]=_fields[i].Value;return v;}}
        public PricingFields(bool service = false)
        {
            Size=new Size(540,480);
            Controls.Add(new Label{Text="Percentuais sobre o custo base. Lucro é acréscimo sobre custo.\nInforme apenas encargos ainda não incluídos no custo.",Location=new Point(8,5),Size=new Size(520,42)});
            var names=new[]{"Custo base (R$)","ICMS (%)","IPI (%)","II — importação (%)","IE — exportação (%)","ISS — serviços (%)","Custo operacional (%)","Lucro sobre custo (%)","Outros custos (R$)"};
            for(int i=0;i<9;i++) {
                Controls.Add(new Label{Text=service && i==0?"Custo de execução (R$)":names[i],Location=new Point(8,55+i*35),Size=new Size(280,25)});
                var f=new NumericUpDown{DecimalPlaces=2,Maximum=i==0||i==8?100000000M:10000M,Location=new Point(300,52+i*35),Size=new Size(190,26),ThousandsSeparator=true};
                _fields[i]=f;Controls.Add(f);f.ValueChanged+=(sender,e)=>Calculate();
            }
            _summary.Location=new Point(8,380);_summary.Size=new Size(500,40);Controls.Add(_summary);
            var apply=new Button{Text="Usar preço sugerido",Location=new Point(300,430),Size=new Size(190,32)};apply.Click+=(sender,e)=>{if(ApplyPrice!=null)ApplyPrice(this,EventArgs.Empty);};Controls.Add(apply);
        }
        public void LoadValues(decimal[] values){for(int i=0;i<9;i++)_fields[i].Value=Math.Min(_fields[i].Maximum,Math.Max(0,values[i]));Calculate();}
        private void Calculate(){foreach(var f in _fields)if(f==null)return;decimal percent=0;for(int i=1;i<8;i++)percent+=_fields[i].Value;SuggestedPrice=Math.Round(_fields[0].Value*(1+percent/100M)+_fields[8].Value,2,MidpointRounding.AwayFromZero);_summary.Text="Preço sugerido: "+SuggestedPrice.ToString("C2")+"\nUse o botão abaixo ou informe seu preço de venda.";}
    }
    internal sealed class PricingForm : Form
    {
        private readonly LocalDatabase _db; private readonly long _id;
        private readonly NumericUpDown[] _fields=new NumericUpDown[9];
        private readonly Label _result=new Label(); private decimal _price;
        public PricingForm(LocalDatabase db,long id)
        {
            _db=db;_id=id;Text="Formação de preço — produto "+id;Size=new Size(650,680);MinimumSize=Size;StartPosition=FormStartPosition.CenterParent;
            Controls.Add(new Label{Text="Formação de preço",Location=new Point(24,18),AutoSize=true,Font=new Font("Segoe UI",16,FontStyle.Bold)});
            Controls.Add(new Label{Text="Modelo de estimativa: todos os percentuais incidem sobre o custo informado.\nLucro é acréscimo sobre custo, não margem sobre venda.\nInforme somente encargos ainda não incluídos no custo. Não calcula obrigações fiscais.",Location=new Point(24,58),Size=new Size(580,65)});
            var labels=new[]{"Custo base (R$)","ICMS (%)","IPI (%)","II — importação (%)","IE — exportação (%)","ISS — serviços (%)","Custo operacional (%)","Lucro sobre custo (%)","Outros custos (R$)"};
            var values=db.GetPricing(id);
            for(int i=0;i<9;i++)
            {
                var y=135+i*37;
                Controls.Add(new Label{Text=labels[i],Location=new Point(24,y+4),Size=new Size(285,26)});
                var field=new NumericUpDown{Location=new Point(325,y),Size=new Size(200,26),DecimalPlaces=2,Maximum=i==0||i==8?100000000M:10000M,ThousandsSeparator=true};
                field.Value=Math.Min(field.Maximum,Math.Max(0,values[i]));_fields[i]=field;Controls.Add(field);
            }
            _result.Location=new Point(24,480);_result.Size=new Size(580,85);Controls.Add(_result);
            foreach(var field in _fields)field.ValueChanged+=(sender,e)=>Calculate();
            var save=new Button{Text="Salvar preço no produto",Location=new Point(325,580),Size=new Size(220,36)};
            save.Click+=(sender,e)=>{try{var input=new decimal[9];for(int i=0;i<9;i++)input[i]=_fields[i].Value;_db.SavePricing(_id,input,_price);MessageBox.Show("Preço e composição salvos.");DialogResult=DialogResult.OK;}catch(Exception ex){MessageBox.Show(ex.Message);}};
            Controls.Add(save);Calculate();
        }
        private void Calculate()
        {
            decimal percent=0;for(int i=1;i<=7;i++)percent+=_fields[i].Value;
            var charges=_fields[0].Value*percent/100M;
            _price=Math.Round(_fields[0].Value+charges+_fields[8].Value,2,MidpointRounding.AwayFromZero);
            _result.Text="Base: "+_fields[0].Value.ToString("C2")+" + percentuais: "+charges.ToString("C2")+" + outros: "+_fields[8].Value.ToString("C2")+"\nPreço sugerido: "+_price.ToString("C2")+"\nISS deve permanecer zero quando não aplicável ao item.";
        }
    }
}
