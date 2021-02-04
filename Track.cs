using Gma.System.MouseKeyHook;
using Microsoft.Win32.TaskScheduler;
using MonitorTrackerForm.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using WebApiAgent.Controllers;


namespace MonitorTrackerForm
{
    public struct POINT
    {
        public int X;
        public int Y;
    }

    public partial class Track : Form
    {
        [DllImport("User32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        [DllImport("User32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);
        private readonly System.Timers.Timer _Tmonitor;
        private readonly System.Timers.Timer _TmonitorCapture;
        static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string appName, prevvalue;
        public static Stack applnames;
        public static Hashtable applhash;
        public static DateTime applfocustime;
        public static List<AutomaticTakeTimeModel> lmt;
        public static string appltitle;
        public TimeSpan applfocusinterval;
        public static int _x, _y;
        /* para definir tiempos en los cuales se haran cosas*/

        public static DateTime LastActivity;
        public static double InactivityPeriod;
        public static double InactivityCounter;
        public static double InactivityTime;
        public static double UploadFrecuency;
        public static double CaptureFrecuency;
        public static DateTime LastUpload;
        public static DateTime LastUCaptureupload;
        public static DateTime LastValidateCredentials;
        public static string UserIp = string.Empty;

        /***************************************************/
        /*para la api*/
        static HttpClient client = new HttpClient();
        static string token;
        static bool sending = false;
        static bool sendingcapture = false;

        static void ShowMousePosition()
        {
            POINT point;
            if (GetCursorPos(out point) && point.X != _x && point.Y != _y)
            {
                if (_x != point.X)
                {
                    _x = point.X;
                    LastActivity = DateTime.Now;
                }

                if (_y != point.Y)
                {
                    _y = point.Y;
                    LastActivity = DateTime.Now;
                }

                _x = point.X;
                _y = point.Y;
            }
        }
        public void Credentials()
        {
            LastValidateCredentials = DateTime.Now;
            GetCredentialsApi();
            writelog("INICIANDO Credentials()", "");
        }
        public Track()
        {
            InitializeComponent();
            try
            {
                //Getting the PC IP
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ju in host.AddressList)
                {
                    if (ju.AddressFamily == AddressFamily.InterNetwork)
                    {
                        UserIp = ju.ToString();
                    }
                }

                CreateTask();
                ValidatorRun();
                applnames = new Stack();
                applhash = new Hashtable();
                lmt = new List<AutomaticTakeTimeModel>();
                LastActivity = DateTime.Now;
                LastUpload = DateTime.Now;
                LastUCaptureupload = DateTime.Now;

                _Tmonitor = new System.Timers.Timer(1000) { AutoReset = true };
                _Tmonitor.Elapsed += TmonitorElapsed;

                _TmonitorCapture = new System.Timers.Timer(1000) { AutoReset = true };
                _TmonitorCapture.Elapsed += TmonitorCaptureElapsedAsync;

                Credentials();
            }
            catch (Exception ex)
            {
                writelog(ex.Message, "Error Track()");
            }
        }
        private static void StartKey()
        {
            KeyLogger.Start();
        }

        private async void TmonitorCaptureElapsedAsync(object sender, ElapsedEventArgs e)
        {
            try
            {
                var Cfrecuency = (DateTime.Now - LastUCaptureupload).TotalSeconds;
                if (CaptureFrecuency < Cfrecuency)
                {
                    LastUCaptureupload = DateTime.Now;
                    /**capturando un print screen de la pantalla activa*/
                    if (!sendingcapture)
                    {
                        sendingcapture = true;
                        CaptureModel Capture = new CaptureModel();
                        Capture = CaptureWind();

                        Response rep = new Response();
                        rep = await CallApiCapture("Capture/WindowsCapture", Capture, token);
                        sendingcapture = false;
                    }
                }
            }
            catch (Exception ex)
            {
                writelog(ex.Message, "Error TmonitorCaptureElapsed()");
            }
        }
        private void TmonitorElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                //here we validate if mouse move, and if move, we reset last activity variable
                ShowMousePosition();

                //validate if is time to validate credentials
                double hours = (DateTime.Now - LastValidateCredentials).TotalHours;
                if (hours >= 12)
                    Credentials();

                //calculate Inactivity
                InactivityTime = (DateTime.Now - LastActivity).TotalSeconds;
                bool isNewAppl = false;
                IntPtr hwnd = APIFuncs.getforegroundWindow();
                Int32 pid = APIFuncs.GetWindowProcessID(hwnd);
                Process p = Process.GetProcessById(pid);
                appName = p.ProcessName;

                appltitle = APIFuncs.ActiveApplTitle().Trim().Replace("\0", "");

                //se valida la frecuencia de subida
                var frecuency = (DateTime.Now - LastUpload).TotalSeconds;
                string newvalue = (appltitle + "-" + appName);
                if ((prevvalue != newvalue) || (UploadFrecuency <= frecuency))
                {
                    double prevseconds = 0;
                    applfocusinterval = DateTime.Now.Subtract(applfocustime);

                    if (InactivityPeriod <= InactivityTime)
                    {
                        addelement(appName, appltitle, (applfocusinterval.TotalSeconds + prevseconds), InactivityTime);
                    }
                    else
                    {
                        addelement(appName, appltitle, (applfocusinterval.TotalSeconds + prevseconds), 0);
                    }

                    LastActivity = DateTime.Now;

                    prevvalue = appltitle + "-" + appName;
                    applfocustime = DateTime.Now;
                    if (UploadFrecuency <= frecuency)
                    {
                        if (!sending)
                        {
                            sending = true;
                            SendData();
                            LastUpload = DateTime.Now;
                        }
                    }
                }
                //si es una app que no estaba en la lista, se reunicia el contador para que inicie a sumar tiempois
                if (isNewAppl)
                    applfocustime = DateTime.Now;
            }
            catch (Exception ex)
            {
                writelog(ex.Message, "Error TmonitorElapsed()");
            }
        }

        private void addelement(string app, string title, double time, double notime)
        {
            try
            {
                ///si el titulo no esta en blanco y los segundos no son mas d 24 horas, esto es po un bug
                if (title != string.Empty && time < 86400)
                {
                    AutomaticTakeTimeModel mt = new AutomaticTakeTimeModel(app.Trim(), title.Trim(), time, notime, UserIp, Environment.MachineName, System.Security.Principal.WindowsIdentity.GetCurrent().Name);
                    lmt.Add(mt);
                }
            }
            catch (Exception ex)
            {
                writelog("error addelement: --- " + ex.Message, "Error addelement");
            }
        }

        public void writelog(string msg, string module)
        {
            string appdirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
            string directory = "C:\\Logs";
            string path = "C:\\Logs\\log.txt";


            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }


            if (!File.Exists(path))
            {
                StreamWriter sw = new StreamWriter(path);
                sw.WriteLine("Iniciando");
                sw.WriteLine(appdirectory);

                sw.WriteLine(msg);
                sw.Close();
            }
            else
            {
                List<string> lines = new List<string>();
                lines.Add(msg + " --/ " + module);
                File.AppendAllLines(path, lines);
            }
        }

        private async Task<Response> CallApi(string rute, object obj, string token)
        {
            try
            {
                client = new HttpClient();
                //local
                client.BaseAddress = new Uri("http://localhost:50221/Api/");

                //prod
                //client.BaseAddress = new Uri("http://25.73.18.157:8085/api/");


                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var json = new JavaScriptSerializer().Serialize(obj);
                    response = await client.PostAsJsonAsync(rute, obj);
                    //response = await client.PostAsJsonAsync(rute, json);                
                }
                else
                {
                    response = await client.PostAsJsonAsync(rute, obj);
                    var cont = response.Content;
                }
                var data = await response.Content.ReadAsStringAsync();
                JObject job = JObject.Parse(data.ToString());
                return JsonConvert.DeserializeObject<Response>(job.ToString());
            }
            catch (Exception ex)
            {
                writelog(ex.Message, "Error CallApi()");
            }
            return null;
        }
        private async Task<Response> CallApiCapture(string rute, object obj, string token)
        {
            try
            {
                HttpClient clientcapture = new HttpClient();
                //local
                clientcapture.BaseAddress = new Uri("http://localhost:50221/Api/");

                //prod
                //client.BaseAddress = new Uri("http://25.73.18.157:8085/api/");


                clientcapture.DefaultRequestHeaders.Accept.Clear();
                clientcapture.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage response;

                if (!string.IsNullOrEmpty(token))
                {
                    clientcapture.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    var json = new JavaScriptSerializer().Serialize(obj);
                    response = await clientcapture.PostAsJsonAsync(rute, obj);
                    //response = await client.PostAsJsonAsync(rute, json);                
                }
                else
                {
                    response = await clientcapture.PostAsJsonAsync(rute, obj);
                    var cont = response.Content;
                }
                var data = await response.Content.ReadAsStringAsync();
                JObject job = JObject.Parse(data.ToString());
                return JsonConvert.DeserializeObject<Response>(job.ToString());
            }
            catch (Exception ex)
            {
                writelog(ex.Message, "Error CallApiCapture()");
            }
            return null;
        }
        private async void GetCredentialsApi()
        {
            try
            {
                string key = "D04181F8-9DA0-4FC0-B810-17A04C019906";
                Response rep = new Response();
                rep = await CallApi("Security/Login", key, string.Empty);

                if (rep != null)
                {
                    if (!string.IsNullOrEmpty(rep.data.token))
                    {
                        token = rep.data.token;
                        //he we set the configuration times that came from the API
                        InactivityPeriod = rep.data.InactivityPeriod;
                        UploadFrecuency = rep.data.UploadFrecuency;
                        CaptureFrecuency = rep.data.CaptureFrecuency;

                        //sending the hardware installed on the pc
                        rep = await CallApi("UserHardware/Hardware", SendHardware(), token);

                        //sending intalled programs on thre pc
                        rep = await CallApi("UserPrograms/Installed", SendInstalledPrograms(), token);

                        System.Threading.Thread tKeyloger = new System.Threading.Thread(StartKey);

                        tKeyloger.Start();

                        this.ShowInTaskbar = false;
                        System.Threading.Thread.Sleep(5000);
                        _Tmonitor.Start();
                        _TmonitorCapture.Start();
                    }
                    else
                    {
                        _Tmonitor.Stop();
                        _TmonitorCapture.Stop();
                        System.Threading.Thread.Sleep(60000);
                        GetCredentialsApi();
                    }
                }
                else
                {
                    _Tmonitor.Stop();
                    _TmonitorCapture.Stop();
                    System.Threading.Thread.Sleep(60000);
                    GetCredentialsApi();
                }
            }
            catch (Exception ex)
            {
                //if (ex.InnerException != null)
                //{
                //    WebExceptionStatus exs = ((System.Net.WebException)ex.InnerException).Status;
                //    if (exs.ToString() == "ConnectFailure")
                //    {

                //    }
                //}
                writelog(ex.Message, "Error GetCredentialsApi()");
                System.Threading.Thread.Sleep(60000);
                GetCredentialsApi();
            }
        }

        private async void SendData()
        {
            //WebExceptionStatus exs = new WebExceptionStatus();
            try
            {
                TrackerModel tm = new TrackerModel();
                tm.token = token;
                tm.AutomaticTakeTimeModel = lmt;
                Response rep = new Response();
                rep = await CallApi("TimeTracker/CreateTracker", tm, token);

                if (rep != null)
                    if (rep.response_code == "SaveOk")
                        lmt = new List<AutomaticTakeTimeModel>();

                sending = false;
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    WebExceptionStatus exs = ((WebException)ex.InnerException).Status;

                }
                writelog(ex.Message, "Error SendData()");
                sending = false;
            }
        }

        private InstalledModel SendInstalledPrograms()
        {
            InstalledModel im = new InstalledModel();
            im.token = token;
            im.InstalledProgramsViewModel = InstalledPrograms.GetInstalledPrograms();
            return im;
        }

        private HardwareModel SendHardware()
        {

            List<InstalledHardwareViewModel> InstalledHardwareViewModel = new List<InstalledHardwareViewModel>();
            InstalledHardwareViewModel ih = new InstalledHardwareViewModel();

            //video card
            ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (ManagementObject obj in myVideoObject.Get())
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = obj["Name"].ToString();
                ih.Type = HardwareType.VideoCard.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);

                break;
            }
            //procesor
            ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (ManagementObject obj in myProcessorObject.Get())
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = obj["Name"].ToString();
                ih.Type = HardwareType.Procesor.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);
                break;
            }

            //SO
            ManagementObjectSearcher myOperativeSystemObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject obj in myOperativeSystemObject.Get())
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = obj["Caption"].ToString();
                ih.Type = HardwareType.OperativeSystem.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);
                break;
            }

            //sound card
            ManagementObjectSearcher myAudioObject = new ManagementObjectSearcher("select * from Win32_SoundDevice");
            foreach (ManagementObject obj in myAudioObject.Get())
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = obj["Name"].ToString();
                ih.Type = HardwareType.SoundCard.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);
                break;
            }
            //hard drive (just primary)
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo d in allDrives)
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = "Name: " + d.Name + " Size: " + SizeSuffix(d.TotalSize);
                ih.Type = HardwareType.HardDrive.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);
            }

            //memory
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();
            foreach (ManagementObject result in results)
            {
                ih = new InstalledHardwareViewModel();
                ih.Hardware = SizeSuffix(Int64.Parse(result["TotalVisibleMemorySize"].ToString()) * 1024);
                ih.Type = HardwareType.TotalMemory.ToString();
                ih.Pc = Environment.MachineName;
                ih.User = Environment.UserName;
                InstalledHardwareViewModel.Add(ih);
                break;
            }

            HardwareModel hm = new HardwareModel();
            hm.token = token;
            hm.InstalledHardwareViewModel = InstalledHardwareViewModel;

            return hm;
        }

        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private CaptureModel CaptureWind()
        {
            string history = "";
            CaptureModel Captures = new CaptureModel();
            try
            {
                //Creating a new Bitmap object
                Bitmap captureBitmap = new Bitmap(1024, 768, PixelFormat.Format32bppArgb);
                MemoryStream ms = new MemoryStream();
                Rectangle captureRectangle = Screen.AllScreens[0].Bounds;
                Graphics captureGraphics = Graphics.FromImage(captureBitmap);
                captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);
                //Guid gname = Guid.NewGuid();
                //captureBitmap.Save("~\\Captures\\" + gname + ".jpg", ImageFormat.Jpeg);
                //captureBitmap.Save("D:\\Proyectos\\Personales\\Andres Villa Proyectos\\ADA\\Monitor\\MonitorTrackerForm\\monitortracker\\Captures\\" + gname + ".jpg", ImageFormat.Jpeg);
                captureBitmap.Save(ms, ImageFormat.Jpeg);
                captureBitmap.Dispose();
                history = history + "captura";
                byte[] byteImage = ms.ToArray();
                ms.Dispose();
                var SigBase64 = Convert.ToBase64String(byteImage); // Get Base64

                history = history + "base 64";

                Captures.token = token;
                Captures.Image = byteImage;
                Captures.Pc = Environment.MachineName;
                Captures.UserName = Environment.UserName;
                Captures.Ip = UserIp;

                history = history + "llamando api";
                return Captures;

            }
            catch (Exception ex)
            {
                writelog(ex.Message + history, "Error CaptureWind()");
            }
            return Captures;
        }


        private void CreateTask()
        {
            using (TaskService service = new TaskService())
            {
                if (!service.RootFolder.AllTasks.Any(t => t.Name == "MonitorTrackerTask"))
                {
                    var task = service.NewTask();

                    task.RegistrationInfo.Description = "Varify Monitor Tracker Execution";
                    task.RegistrationInfo.Author = "GearSoft";
                    string path = Directory.GetCurrentDirectory() + "\\MonitorTrackerForm.exe";
                    task.Actions.Add(new ExecAction(path, null, null));

                    var hourlyTrigger = new DailyTrigger { StartBoundary = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.AddMinutes(3).Minute, 0) };
                    hourlyTrigger.Repetition.Interval = TimeSpan.FromMinutes(10);
                    task.Triggers.Add(hourlyTrigger);

                    service.RootFolder.RegisterTaskDefinition("MonitorTrackerTask", task);
                }
            }
        }

        private void ValidatorRun()
        {
            Process[] pname = Process.GetProcessesByName("MonitorTrackerForm");
            writelog(pname.Length.ToString(), "ValidatorRun()");
            if (pname.Length > 1)
            {
                Environment.Exit(1023534368);
            }
        }
    }

    enum HardwareType
    {
        VideoCard,
        SoundCard,
        TotalMemory,
        AvaibleMemory,
        HardDrive,
        Procesor,
        OperativeSystem
    }
}
