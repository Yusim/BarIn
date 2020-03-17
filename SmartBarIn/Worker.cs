using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using ZXing.Mobile;

namespace SmartBarIn
{
    /// <summary>
    /// Главный рабочий класс
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Начальные установки
        /// </summary>
        public Worker(Application App)
        {
            this.App = App;
            Scan = new Scanner(App);
            Cfg = new Config();
            Web = new WebSrv(Cfg.WebAdr);
            IsCompIdSet = false;
            CompId = Guid.Empty;
        }

        /// <summary>
        /// Сканер
        /// </summary>
        private readonly Scanner Scan;
        /// <summary>
        /// Конфигурация
        /// </summary>
        private readonly Config Cfg;
        /// <summary>
        /// Веб-служба
        /// </summary>
        private readonly WebSrv Web;

        private readonly Application App;

        /// <summary>
        /// Признак установки CompId
        /// </summary>
        public bool IsCompIdSet { get; private set; }
        /// <summary>
        /// Идентификатор компьютера
        /// </summary>
        public Guid CompId { get; private set; }

        /// <summary>
        /// Установка нового CompId со сканера
        /// </summary>
        public void ScanCompId()
        {
            string sCompId = Scan.GetString();
            if (Guid.TryParse(sCompId, out Guid newGuid))
            {
                CompId = newGuid;
                IsCompIdSet = true;
            }
        }
        /// <summary>
        /// Сканирование и отсыл текста
        /// </summary>
        public void ScanText()
        {
            string txt = (Scan.GetString() ?? "").Trim();
            if (txt != "")
                Web.Post(CompId, txt);
        }
        /// <summary>
        /// Изменение конфигурации
        /// </summary>
        public void SetConfig()
        {
            Cfg.SetConfig();
        }
    }

    /// <summary>
    /// Работа со сканером кодов
    /// </summary>
    public class Scanner
    {
        public Scanner(Application App)
        {
            this.App = App;
        }

        private readonly Application App;

        /// <summary>
        /// Считывание строки из кода
        /// </summary>
        /// <returns>Строка (или null, если отмена)</returns>
        public string GetString()
        {
            MobileBarcodeScanner.Initialize(App);
            MobileBarcodeScanner scanner = new ZXing.Mobile.MobileBarcodeScanner()
            {
                TopText = "asdfgsdfgsdfg"
            };
            ZXing.Result result = scanner.Scan().Result;
            return result.Text;
        }
    }

    /// <summary>
    /// Работа с конфигурацией
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Созданиеи класса
        /// </summary>
        public Config()
        {
            WebAdr = "http://barin.somee.com/Service.svc/scan";
        }

        /// <summary>
        /// Адрес веб-службы
        /// </summary>
        public string WebAdr { get; set; }

        /// <summary>
        /// Изменение конфигурации
        /// </summary>
        public void SetConfig()
        {
            //todo
        }

        //todo
    }

    /// <summary>
    /// Работа с веб-службой
    /// </summary>
    public class WebSrv
    {
        /// <summary>
        /// Создание класса
        /// </summary>
        /// <param name="WebAdr">Адрес службы</param>
        public WebSrv(string WebAdr)
        {
            this.WebAdr = WebAdr;
        }

        /// <summary>
        /// Адрес службы
        /// </summary>
        public string WebAdr { get; private set; }

        /// <summary>
        /// Отправка сообщения
        /// </summary>
        /// <param name="CompId">Идентификатор компьютера</param>
        /// <param name="Text">Текст</param>
        public void Post(Guid CompId, string Text)
        {
            //todo
        }
    }
}