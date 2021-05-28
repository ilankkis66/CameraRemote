using Android.App;
using Android.OS;
using Android.Widget;
using System.Net.Sockets;
using Android.Content;
using System;
using Android.Support.V7.App;
using Android.Graphics;
using System.IO;
using Android;
using Android.Support.V4.App;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using static Android.Graphics.Bitmap;
using System.Net;
using Android.Media;
using Java.IO;
using Android.Content.PM;
using Android.Webkit;
using System.Windows;
using System.Threading;

namespace CameraRemote
{
    //https://www.c-sharpcorner.com/article/creating-a-camera-app-in-xamarin-android-app-using-visual-studio-2015/
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button btCamera, btGetCon, btSearch, btGetPic;
        ListView lvDevices;
        ImageView iv;
        private const string SERVER_IP = "192.168.1.28";
        private const int SERVER_PORT = 8820; private const int CHUNK = 1024;
        private const string SEPERATOR = "###";
        private const int devicePort = 6666;
        private string device_ip = "";
        private List<string> AllDevices = new List<string>(); private int PhotosNumber=0;
        private string[] IpRole;
        private TcpClient ServerTCP; private NetworkStream ServerStream;
        private TcpClient DeviceTcp; private NetworkStream DeviceStream; private string DeviceName="";

        //bool mExternalStorageAvailable = false; bool mExternalStorageWriteable = false;
        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            InitWidgets();
            setPermissitios();
            CheckInstallIpWebcam();
            //Search();
        }

        public void setPermissitios()
        {
            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage, Manifest.Permission.ReadExternalStorage, Manifest.Permission.Camera, }, 1);
            string state = Android.OS.Environment.ExternalStorageState;
            if (Android.OS.Environment.MediaMounted.Equals(state))
            {
                //We can read and write the media
                //mExternalStorageAvailable = mExternalStorageWriteable = true;
                Toast.MakeText(this, "We can read and write the media", ToastLength.Long).Show();
            }

            else if (Android.OS.Environment.MediaMountedReadOnly.Equals(state))
            {
                //We can only read the media
                //mExternalStorageAvailable = true; mExternalStorageWriteable = false;
                Toast.MakeText(this, "We can only read the media", ToastLength.Long).Show();
            }
            else
            {
                //Something else is wrong. we can neither read nor write
                //mExternalStorageAvailable = mExternalStorageWriteable = false;
                Toast.MakeText(this, "Something else is wrong. we can neither read nor write", ToastLength.Long).Show();
            }

        }

        public void CheckInstallIpWebcam()
        {
            PackageManager pm = this.PackageManager;
            Intent intent = pm.GetLaunchIntentForPackage("com.pas.webcam");
            if (intent == null)
            {
                var uri = Android.Net.Uri.Parse("https://play.google.com/store/apps/details?hl=en&id=com.pas.webcam");
                var i = new Intent(Intent.ActionView, uri);
                StartActivity(i);
            }
        }


        #region ButtonsClick
        [Obsolete]
        private void LvDevices_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            SendData("COND" + SEPERATOR + AllDevices[(int)e.Id], ServerStream);
            DeviceName = AllDevices[(int)e.Id];
            string s = "";
            while (s == "") s = ReceiveData(ServerStream);
            IpRole = GetDeviceIpRole(s);
            if (s.StartsWith("DADR"))
                if (IpRole[1] == "server")
                {
                    //connect to the device
                    TcpListener tcp_device = new TcpListener(devicePort);
                    tcp_device.Start();
                    DeviceTcp = tcp_device.AcceptTcpClient();
                    DeviceStream = DeviceTcp.GetStream();

                    //open the stream
                    PackageManager pm = this.PackageManager;
                    Intent intent = pm.GetLaunchIntentForPackage("com.pas.webcam");
                    StartActivity(intent);
                }
        }

        [Obsolete]
        private void BtGetCon_Click(object sender, EventArgs e)
        {
            string s = "";
            while(s == "")  s = ReceiveData(ServerStream); 
            IpRole = GetDeviceIpRole(s);
            if (s.StartsWith("DADR"))
                if (IpRole[1] == "client")
                {
                    //connect to the device
                    DeviceTcp = new TcpClient(device_ip, devicePort);
                    DeviceStream = DeviceTcp.GetStream();
                    DeviceName = IpRole[2];

                    //connect to the stream
                    var uri = Android.Net.Uri.Parse("http://" + device_ip + ":8080/browserfs.html");
                    var intent = new Intent(Intent.ActionView, uri); 
                    StartActivity(intent);
                }
            ChangeLayout();
        }
        private void BtnTakePic_Click(object sender, EventArgs e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] dataArr = webClient.DownloadData("http://" + device_ip + ":8080/photo.jpg");
                //save file to local
                string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
                string fname = "number " + (PhotosNumber++).ToString() + " " + DeviceName + ".jpg";
                System.IO.File.WriteAllBytes(root + fname,dataArr);

                //show the image on the screen
                Bitmap bt = BitmapFactory.DecodeByteArray(dataArr, 0, dataArr.Length);
                iv.SetImageBitmap(bt);

                //send to the server to save picture
                string s = "SPIC" + SEPERATOR + DeviceName + SEPERATOR;
                SendData(s, ServerStream);
            }
        }
        [Obsolete]
        private void BtSearch_Click(object sender, EventArgs e)
        {
            ServerTCP = new TcpClient(SERVER_IP, SERVER_PORT);
            ServerStream = ServerTCP.GetStream();
            SendData(GetDeviceName() + " " + GetDeviceMacAddress(), ServerStream);
            GetAllDevices(ServerStream);
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, AllDevices);
            lvDevices.SetAdapter(arrayAdapter);
        }
        private void BtGetPic_Click(object sender, EventArgs e)
        {
            //using (WebClient webClient = new WebClient())
            //{
            //    byte[] dataArr = webClient.DownloadData("http://" + "192.168.1.13" + ":8080/photo.jpg");
            //    //save file to local
            //    string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
            //    Java.IO.File myDir = new Java.IO.File(root + "/saved_images");
            //    myDir.Mkdirs();
            //    Java.IO.File file = new Java.IO.File(myDir, "ilankiis.jpg");
            //    System.IO.File.WriteAllBytes(root + "/saved_images/ilankiis.jpg", dataArr);
            //    Bitmap bt = BitmapFactory.DecodeByteArray(dataArr, 0, dataArr.Length);
            //    iv.SetImageBitmap(bt);
            //    string s = "SPIC" + SEPERATOR + GetDeviceName() + " " + GetDeviceMacAddress() + SEPERATOR;
            //    SendData(s, ServerStream);
            //}
        }

        #endregion


        [Obsolete]
        private void Search()
        {
            //send to the server
            ServerTCP = new TcpClient(SERVER_IP, SERVER_PORT);
            ServerStream = ServerTCP.GetStream();
            SendData(GetDeviceName() + " " + GetDeviceMacAddress(), ServerStream);

            //get the connected devices from the server 
            GetAllDevices(ServerStream);
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, AllDevices);
            lvDevices.SetAdapter(arrayAdapter);
        }

        [Obsolete]
        private void ChangeLayout()
        {
            //change the text and the OnClick of the button
            btGetCon.SetX(150);
            btGetCon.Text = "Take Pic";
            btGetCon.Click -= BtGetCon_Click;
            btGetCon.Click += BtnTakePic_Click;

            //clear lvDevices items
            List<string> a = new List<string>();
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, a);
            lvDevices.SetAdapter(arrayAdapter);
        }

        #region GetMethod
        private void GetAllDevices(NetworkStream stream)
        {
            string d = "";
            while (!d.Contains("ENDD"))
                d += ReceiveData(stream);
            string[] a = d.Split(SEPERATOR);
            for (int i = 0; i < a.Length-3; i++)
                AllDevices.Add(a[i]);
            PhotosNumber = int.Parse(a[a.Length - 1]);
           
        }
        public static string GetDeviceName()
        {
            string manufacturer = Build.Manufacturer;
            string model = Build.Model;
            if (model.StartsWith(manufacturer))
                return model;
            return manufacturer + " " + model;
        }
        private string[] GetDeviceIpRole(string s)
        {
            string[] data = s.Split(SEPERATOR);
            for (int i = 2; data[1][i] != "'"[0]; i++)
                device_ip += data[1][i];
            if (data.Length == 4)
                return new string[] { data[0], data[2], data[3] };
            return new string[] { data[0], data[2]};
        }
        public static string GetDeviceMacAddress()
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    netInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    var address = netInterface.GetPhysicalAddress();
                    return BitConverter.ToString(address.GetAddressBytes());

                }
            }
            return "";
        } 
        
        #endregion

        [Obsolete]
        private void InitWidgets()
        {
            lvDevices = (ListView)FindViewById(Resource.Id.lvDeviecs);
            btCamera = (Button)FindViewById(Resource.Id.btnTakePic);
            btGetCon = (Button)FindViewById(Resource.Id.btnGetCon);
            btSearch = (Button)FindViewById(Resource.Id.btnSearch);
            btGetPic = (Button)FindViewById(Resource.Id.btnGetPic);
            iv = (ImageView)FindViewById(Resource.Id.iv);

            btCamera.Click += BtnTakePic_Click;
            btGetCon.Click += BtGetCon_Click;
            btSearch.Click += BtSearch_Click;
            btGetPic.Click += BtGetPic_Click;
            lvDevices.ItemClick += LvDevices_ItemClick;
        }

        #region sending
        private void SendData(string msg, NetworkStream stream)
        {
            byte[] data = new byte[CHUNK];
            for (int i = 0; i < msg.Length; i++)
                data[i] = (byte)msg[i];
            stream.Write(data, 0, msg.Length);
        }
        private string ReceiveData(NetworkStream stream)
        {
            byte[] data = new byte[CHUNK];
            string s = "";
            try{stream.Read(data);}
            catch{return "";}
            for (int i = 0; data[i] != 0; i++)
                s += (char)data[i];
            return s;
        }
        #endregion

    }
}