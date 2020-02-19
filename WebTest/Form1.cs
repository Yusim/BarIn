using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Work w = new Work();

        private void Form1_Load(object sender, EventArgs e)
        {            
            w.BoxIn = ed1;
            w.BoxOut = ed2;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            w.Reg();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            w.UnReg();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            w.Post();
        }
    }

    public class Work : WebRef.IReciverCallback
    {
        public Work()
        {
            ReciverClient = new WebRef.ReciverClient(new System.ServiceModel.InstanceContext(this));
            PostmanClient = new WebRef.PostmanClient();
        }

        private readonly WebRef.ReciverClient ReciverClient;
        private readonly WebRef.PostmanClient PostmanClient;
        private readonly Guid CompId = Guid.Empty;

        public TextBox BoxIn { get; set; }
        public TextBox BoxOut { get; set; }

        public void Ping() { }
                
        public void Send(string Text) { BoxOut.Text = Text; }

        public void Reg() { ReciverClient.Register(CompId); }
        public void UnReg() { ReciverClient.UnRegister(CompId); }

        public void Post()
        {
            //System.Diagnostics.Process.Start("notepad");
            PostmanClient.Post(CompId, "xxx"+BoxIn.Text+"xxx");
        }
    }
}
