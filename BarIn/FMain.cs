using BarCodes;
using System;
using System.ServiceModel;
using System.Windows.Forms;

namespace BarIn
{
    public partial class FMain : Form
    {
        public FMain()
        {
            InitializeComponent();
        }

        private const int QrVersion = 2;
        private const QRCode.CorrectionLevel QrLevel= QRCode.CorrectionLevel.LevelM;
        private const int QrSize = 8;

        private Worker Work;

        private void FMain_Load(object sender, EventArgs e)
        {
            Work = new Worker();
            QrBox.Image = QRCode.CreateQR(Work.CompId.ToByteArray(), QrVersion, QrLevel, -1, QrSize);
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
