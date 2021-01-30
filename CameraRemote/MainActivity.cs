using Android.App;
using Android.OS;
using Android.Widget;
using System.Net.Sockets;
using Android.Content;
using System;
using Android.Support.V7.App;
using Android.Views;
using Java.Lang;
using System.Drawing;
using Android.Graphics;
using System.IO;
using Android;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using Android.Runtime;
using System.Collections.Generic;

namespace CameraRemote
{
    //https://www.c-sharpcorner.com/article/creating-a-camera-app-in-xamarin-android-app-using-visual-studio-2015/
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button btCamera;Button btGetCon;
        TextView tvStatus;
        ListView lvDevices;
        const string SERVER_IP = "192.168.1.28";
        const int SERVER_PORT = 8820;
        const int CHUNK = 1024;
        const string SEPERATOR = "###";
        string device_ip = "";
        List<string> AllDevices;
        string[] IpRole;
        NetworkStream ServerStream;
        TcpClient ServerTCP;
        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestPermissions();
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            ServerTCP = new TcpClient(SERVER_IP, SERVER_PORT);
            ServerTCP.ReceiveTimeout = 500;
            ServerStream = ServerTCP.GetStream();
            InitWidgets();
            SendData(GetDeviceName(), ServerStream);
            GetAllDevices(ServerStream);
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, AllDevices);
            lvDevices.SetAdapter(arrayAdapter);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            string s = "";
            try{s = ReceiveData(ServerStream);}
            catch { }
            if (s.StartsWith("ADDD"))
            {
                AllDevices.Add(s.Substring(7));
                ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, AllDevices);
                lvDevices.SetAdapter(arrayAdapter);
            }
            else if (s.StartsWith("DADR"))
            {

            }

        }

        private void RequestPermissions()
        {
            ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.Camera }, 1);
        }

        #region ButtonsClick
        [Obsolete]
        private void LvDevices_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            SendData("COND" + SEPERATOR + AllDevices[(int)e.Id], ServerStream);
            string s = "";
            while(s=="")
                s = ReceiveData(ServerStream);
            if (s.Substring(0, 4) == "DADR")
            {
                IpRole = GetDeviceIpRole(s).Split(SEPERATOR);
                if (IpRole[1] == "server")
                {
                    TcpListener tcp_device = new TcpListener(6666);
                    tcp_device.Start();
                    TcpClient client = tcp_device.AcceptTcpClient();
                    NetworkStream stream_device = client.GetStream();
                    SendData("ilan", stream_device);
                    tvStatus.Text = "i am the server, sent -----> ilan";
                }
            }
        }

        private void BtGetCon_Click(object sender, EventArgs e)
        {
            string s = "";
            while(s=="")
                s = ReceiveData(ServerStream);
            if (s.Substring(0, 4) == "DADR")
            {
                IpRole = GetDeviceIpRole(s).Split(SEPERATOR);
                if (IpRole[1] == "client")
                {
                    TcpClient tcp_device = new TcpClient(device_ip, 6666);
                    NetworkStream stream_device = tcp_device.GetStream();
                    string t = "";
                    while(t=="")
                        t = ReceiveData(ServerStream);
                    tvStatus.Text = "received------>" + t;
                }
            }
        }
        
        private void BtCamera_Click(object sender, EventArgs e)
        {
            CameraClick();
        }
        #endregion

        #region GetMethod
        private void GetAllDevices(NetworkStream stream)
        {
            string d = "";
            while (!d.EndsWith("ENDD"))
                d += ReceiveData(stream);
            string[] a = d.Substring(0,d.Length-4).Split(SEPERATOR);
            AllDevices = new List<string>();
            for (int i = 0; i < a.Length; i++)
                AllDevices.Add(a[i]);
        }
        public static string GetDeviceName()
        {
            string manufacturer = Build.Manufacturer;
            string model = Build.Model;
            if (model.StartsWith(manufacturer))
                return model;
            return manufacturer + " " + model;
        }
        private string GetDeviceIpRole(string s)
        {
            string[] data = s.Split(SEPERATOR);
            for (int i = 2; data[1][i] != "'"[0]; i++)
                device_ip += data[1][i];
            return data[0] + SEPERATOR + data[2];
        }
        #endregion

        [Obsolete]
        private void InitWidgets()
        {
            btCamera = (Button)FindViewById(Resource.Id.camera);
            btGetCon = (Button)FindViewById(Resource.Id.getCon);
            tvStatus = (TextView)FindViewById(Resource.Id.tvStatus);
            lvDevices = (ListView)FindViewById(Resource.Id.lvDeviecs);
            btCamera.Click += BtCamera_Click;
            btGetCon.Click += BtGetCon_Click;
            lvDevices.ItemClick += LvDevices_ItemClick;
        }
        private void CameraClick()
        {
            Intent intent = new Intent("android.media.action.IMAGE_CAPTURE");
            StartActivity(intent);
        }
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
    }
}