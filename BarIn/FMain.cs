using BarCodes;
using System;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;

namespace BarIn
{
    public partial class FMain : Form
    {
        public FMain()
        {
            InitializeComponent();
        }

        private const int QrVersion = 3;
        private const QRCode.CorrectionLevel QrLevel= QRCode.CorrectionLevel.LevelM;
        private const int QrSize = 10;

        private Worker Work;

        private void FMain_Load(object sender, EventArgs e)
        {
            Work = new Worker();
            string IdStr = Work.CompId.ToString("D").ToUpperInvariant();
            byte[] buf = Encoding.UTF8.GetBytes(IdStr);
            QrBox.Image = QRCode.CreateQR(buf, QrVersion, QrLevel, -1, QrSize);
            Text = IdStr;
            Work.Start();
        }

        private void FMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Work.Stop();
        }
    }

    public class Worker : IReciverCallback
    {
        public Worker()
        {
            CompId = Guid.Empty; //todo GUID
            try
            {
                Web = new DuplexChannelFactory<IReciver>(this, "WSDualHttpBinding_IReciver").CreateChannel();
            }
            catch(Exception ex) { MessageBox.Show($"Не удалось создать подключение\r\n{ex.Message}"); }
        }

        public Guid CompId { get; private set; }

        private readonly IReciver Web;

        public void Start()
        {
            try { Web.Register(CompId); }
            catch(Exception ex) { MessageBox.Show($"Не удалось зарегистрироваться\r\n{ex.Message}"); }
        }
        public void Stop()
        {
            try { Web.UnRegister(CompId); }
            catch { }
        }

        void IReciverCallback.Ping() { }
        void IReciverCallback.Send(string Text) { SendKeys.Send(Text); }
    }
}
