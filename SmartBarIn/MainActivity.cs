using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Widget;
using System;
using System.Collections.Generic;
using ZXing.Mobile;

namespace SmartBarIn
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        /// <summary>
        /// Ссылка на веб-сервис
        /// </summary>
        private const string WebUri = "http://barin.somee.com/Service.svc/scan";
        /// <summary>
        /// Признак установки CompId
        /// </summary>
        private bool IsCompIdSet = false;
        /// <summary>
        /// Идентификатор компьютера
        /// </summary>
        private Guid CompId = Guid.Empty;       

        /// <summary>
        /// Кнопка регистрации
        /// </summary>
        private Button bSetCompId;
        /// <summary>
        /// Кнопка сканирования
        /// </summary>
        private Button bScan;

        /// <summary>
        /// Установка параметров контролов
        /// </summary>
        private void SetView()
        {
            bSetCompId.Text = (IsCompIdSet ? "Перерегистрация" : "Регистрация");
            bScan.Visibility = (IsCompIdSet ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Invisible);
        }

        /// <summary>
        /// Выдача сообщения об ошибке
        /// </summary>
        /// <param name="ex">Ошибка</param>
        private void DoError(Exception ex)
        {
            Toast.MakeText(ApplicationContext, ex.Message, ToastLength.Long).Show();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            if(savedInstanceState!=null)
            {
                IsCompIdSet = savedInstanceState.GetBoolean("IsCompIdSet");
                CompId = Guid.Parse(savedInstanceState.GetString("CompId"));
            }

            MobileBarcodeScanner.Initialize(Application);

            bSetCompId = FindViewById<Button>(Resource.Id.bSetCompId);
            bScan = FindViewById<Button>(Resource.Id.bScan);

            bSetCompId.Click += async(sender, e) => 
            {
                try
                {
                    MobileBarcodeScanner scanner = new ZXing.Mobile.MobileBarcodeScanner()
                    {
                        TopText = "Регистрация сканера"
                    };
                    ZXing.Result result = await scanner.Scan();
                    if (Guid.TryParse(result?.Text, out Guid newGuid))
                    {
                        CompId = newGuid;
                        IsCompIdSet = true;
                        SetView();
                    }
                }
                catch(Exception ex) { DoError(ex); }
            };
            bScan.Click += async(sender, e) => 
            {
                try
                {
                    MobileBarcodeScanner scanner = new ZXing.Mobile.MobileBarcodeScanner()
                    {
                        TopText = "Чтение кода"                         
                    };
                    MobileBarcodeScanningOptions opt = new MobileBarcodeScanningOptions()
                    {
                        PossibleFormats=new List<ZXing.BarcodeFormat>()
                        {
                             ZXing.BarcodeFormat.QR_CODE,
                             ZXing.BarcodeFormat.All_1D
                        }
                    };
                    ZXing.Result result = await scanner.Scan(opt);
                    string txt = (result?.Text ?? "").Trim();
                    if (txt != "")
                    {
                        using (BarInWeb.Service web = new BarInWeb.Service() { Url = WebUri })
                            web.Post(CompId.ToString("D"), txt);
                    }
                }
                catch(Exception ex) { DoError(ex); }
            };

            SetView();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean("IsCompIdSet", IsCompIdSet);
            outState.PutString("CompId", CompId.ToString());
            base.OnSaveInstanceState(outState);
        }

    }
}