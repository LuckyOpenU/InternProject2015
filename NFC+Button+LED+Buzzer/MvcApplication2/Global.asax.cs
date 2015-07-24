using System;
using System.Collections.Generic;
using System.Data.Entity;
// using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using MvcApplication2.SignalR;
using System.Diagnostics;
using SharpNFC;
using MvcApplication2.PInvoke;
using System.Runtime.InteropServices;


namespace MvcApplication2
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            RouteTable.Routes.MapHubs();

            AreaRegistration.RegisterAllAreas();

            // Use LocalDB for Entity Framework by default
            // Database.DefaultConnectionFactory = new SqlConnectionFactory(@"Data Source=(localdb)\v11.0; Integrated Security=True; MultipleActiveResultSets=True");

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);

            InitiGPIO();

            Thread threadSmartCard = new Thread(new ThreadStart(ThreadSmartCard));
            threadSmartCard.Start();

            Thread threadButton = new Thread(new ThreadStart(ThreadButton));
            threadButton.Start();

            return;
        }

        private void ThreadButton()
        {
            CurrentState = new GPIOState();

            while (true)
            {
                var sw1 = GPIO24_Sw1.Value != 0;

                lock (CurrentState)
                {
                    if (CurrentState.sw1 != sw1)
                    {
                        CurrentState.sw1 = sw1;
                        CurrentState.led1 = GPIO25_LED1.Value != 0;

                        if (CurrentState.sw1 == true)
                        {
                            GPIO.Instance.UpdateButtonStatus("Button Pressed!!");
                            Console.WriteLine("Button Pressed");
                        }
                        else
                        {
                            GPIO.Instance.UpdateButtonStatus("Button Not Pressed!!");
                            Console.WriteLine("Button Not Pressed!");
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        public static TinyGPIO GPIO24_Sw1 { get; set; }
        public static TinyGPIO GPIO23_buz { get; set; }
        public static GPIOState CurrentState { get; set; }
        public static TinyGPIO GPIO25_LED1 { get; set; }
        public static NFCState CurrentNFC { get; set; }
        public static string Arguments { get; set; }
        public static string FileName { get; set; }
        public static bool RedirectStandardOutput { get; set; }
        public static bool UseShellExecute { get; set; }

        private static void InitiGPIO()
        {
            // init GPIO 24 for Switch1
            GPIO24_Sw1 = TinyGPIO.Export(24);
            GPIO24_Sw1.Direction = GPIODirection.In;

            // init GPIO 25 for LED1
            GPIO25_LED1 = TinyGPIO.Export(25);
            GPIO25_LED1.Direction = GPIODirection.Out;

            // init GPIO 23 for Buzzer
            GPIO23_buz = TinyGPIO.Export(23);
            GPIO23_buz.Direction = GPIODirection.Out;
        }

        static private void ThreadSmartCard()
        {
            var gpio23 = TinyGPIO.Export(23);
            gpio23.Direction = (GPIODirection)GPIODirection.Out;

            List<string> deviceNameList = new List<string>();

            NFCContext nfcContext = new NFCContext();

            NFCDevice nfcDevice = nfcContext.OpenDevice(null);

            deviceNameList = nfcContext.ListDeviceNames();

            Console.WriteLine("Device Count: " + deviceNameList.Count());

            foreach (string deviceName in deviceNameList)
            {
                Console.WriteLine("Device Name: " + deviceName);
            }

            int rtn = nfcDevice.initDevice();
            if (rtn < 0)
            {
                Console.WriteLine("Context init failed");
            }

            nfc_target nfcTarget = new nfc_target();
            List<nfc_modulation> nfc_modulationList = new List<nfc_modulation>();
            nfc_modulation nfcModulation = new nfc_modulation();
            nfcModulation.nbr = nfc_baud_rate.NBR_106;
            nfcModulation.nmt = nfc_modulation_type.NMT_ISO14443A;

            nfc_modulationList.Add(nfcModulation);

            string currentSignalRStr = null;
            string currentConsoleStr = null;
            string signalRStr;
            string consoleStr;
            string state = "---";

            for (; ; )
            {
                gpio23.Value = 0;
                Thread.Sleep(100);
                rtn = nfcDevice.Pool(nfc_modulationList, 1, 2, out nfcTarget);

                if (rtn < 0)
                {
                    consoleStr = "NFC-Poll Targert Not Found!";
                    signalRStr = "---";
                    gpio23.Value = 0;

                }
                else
                {
                    signalRStr = string.Join(
                        separator: "",
                        values: nfcTarget.nti.abtUid.Take((int)nfcTarget.nti.szUidLen).Select(b => b.ToString("X2").ToLower())
                     );

                    signalRStr = "0x" + signalRStr;

                    consoleStr = string.Format("NFC-Poll Target Found: uid is [{0}]", signalRStr);
                }
                if (signalRStr != state)
                {
                    if (signalRStr != currentSignalRStr)
                    {
                        NFC.Instance.UpdateNFCStatus(signalRStr);
                        currentSignalRStr = signalRStr;
                        gpio23.Value = 1;
                        Thread.Sleep(100);
                    }
                    else
                    {
                        gpio23.Value = 0;
                    }
                }
                else
                {
                    gpio23.Value = 0;
                    NFC.Instance.UpdateNFCStatus(signalRStr);
                    currentSignalRStr = signalRStr;
                }

                if (consoleStr != currentConsoleStr)
                {
                    Console.WriteLine(consoleStr);
                    currentConsoleStr = consoleStr;
                }
            }
        }
    }
}

