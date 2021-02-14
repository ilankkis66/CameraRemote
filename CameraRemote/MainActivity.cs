﻿using Android.App;
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
using Java.IO;
using Java.Nio;
using System.Net.NetworkInformation;

namespace CameraRemote
{
    //https://www.c-sharpcorner.com/article/creating-a-camera-app-in-xamarin-android-app-using-visual-studio-2015/
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button btCamera, btGetCon, btSearch;
        TextView tvStatus, tv;
        ListView lvDevices;
        ImageView iv;
        const string SERVER_IP = "192.168.1.18";
        const int SERVER_PORT = 20540; const int CHUNK = 1024;
        const string SEPERATOR = "###";
        const int devicePort = 6666;
        string device_ip = ""; 
        List<string> AllDevices;
        string[] IpRole;
        NetworkStream ServerStream; TcpClient ServerTCP;
        //bool mExternalStorageAvailable = false; bool mExternalStorageWriteable = false;
        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            setPermissitios();
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            InitWidgets();
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

        [Obsolete]
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 0)//coming from camera
            {
                if (resultCode == Result.Ok)
                {
                    Android.Graphics.Bitmap bitmap = (Android.Graphics.Bitmap)data.Extras.Get("data");

                    /*int width = bitmap.Width;
                    int height = bitmap.Height;

                    int size = bitmap.RowBytes * bitmap.Height;
                    ByteBuffer byteBuffer = ByteBuffer.Allocate(size);
                    bitmap.CopyPixelsToBuffer(byteBuffer);
                    byte[] byteArray= new byte[size];
                    for (int i = 0; i < size-1; i++)
                        byteArray[i]= (byte)byteBuffer.GetChar(i);
                    //int j = 0;
                    //byte[] bt = new byte[1024];
                    //while (j < size)
                    //{
                    //    for (int i = 0; i < 1024 && j<size; i++)
                    //    {
                    //        bt[i] = byteArray[j];
                    //        j -= -1;
                    //    }
                    //    ServerStream.Write(bt);
                    //}
                    // FileStream f = System.IO.File.Create("i.png");
                    // f.Write(byteArray);
                    // f.Close();

                    // ServerStream.Write((byte[])bitmap);*/
                    iv.SetImageBitmap(bitmap);
                    saveImageToExternalStorage_version1(bitmap);
                }
            }
        }
        private void saveImageToExternalStorage_version1(Android.Graphics.Bitmap finalBitmap)
        {
            string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
            Java.IO.File myDir = new Java.IO.File(root + "/saved_images");
            myDir.Mkdirs();
            Random generator = new Random();
            int n = 10000;
            n = generator.Next(n);
            string fname = "Image-" + n + ".jpg";
            Java.IO.File file = new Java.IO.File(myDir, fname);
            if (file.Exists())
                file.Delete();
            try
            {
                string path = System.IO.Path.Combine(myDir.AbsolutePath, fname);
                var fs = new FileStream(path, FileMode.Create);
                if (fs != null)
                {
                    finalBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 90, fs);
                    tv.Text = myDir.AbsolutePath;
                }
                fs.Flush();
                fs.Close();
            }
            catch (System.Exception e)
            {
                tv.Text = e.ToString();
                Toast.MakeText(this, e.ToString(), ToastLength.Long).Show();
            }

        }

        #region ButtonsClick
        [Obsolete]
        private void LvDevices_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            SendData("COND" + SEPERATOR + AllDevices[(int)e.Id], ServerStream);
            string s = "";
            while (s == "") s = ReceiveData(ServerStream);
            IpRole = GetDeviceIpRole(s);
            if (s.StartsWith("DADR"))
                if (IpRole[1] == "server")
                {
                    TcpListener tcp_device = new TcpListener(devicePort);
                    tcp_device.Start();
                    TcpClient client = tcp_device.AcceptTcpClient();
                    NetworkStream stream_device = client.GetStream();
                    SendData("ilan", stream_device);
                    tvStatus.Text = "i am the server, sent -----> ilan";
                }
        }
        private void BtGetCon_Click(object sender, EventArgs e)
        {
            string s = "";
            while(s == "")  s = ReceiveData(ServerStream); 
            IpRole = GetDeviceIpRole(s);
            if (s.StartsWith("DADR"))
                if (IpRole[1] == "client")
                {
                    TcpClient tcp_device = new TcpClient(device_ip, devicePort);
                    NetworkStream stream_device = tcp_device.GetStream();
                    string t = "";
                    while (t == "")
                        t = ReceiveData(stream_device);
                    tvStatus.Text = "received------>" + t;
                }
        }
        private void BtnTakePic_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent("android.media.action.IMAGE_CAPTURE");
            StartActivityForResult(intent,0);
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
        #endregion

        #region GetMethod
        private void GetAllDevices(NetworkStream stream)
        {
            string d = "";
            while (!d.EndsWith("ENDD"))
                d += ReceiveData(stream);
            string[] a = d.Substring(0,d.Length-4).Split(SEPERATOR);
            AllDevices = new List<string>();
            for (int i = 0; i < a.Length-1; i++)
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
        private string[] GetDeviceIpRole(string s)
        {
            string[] data = s.Split(SEPERATOR);
            for (int i = 2; data[1][i] != "'"[0]; i++)
                device_ip += data[1][i];
            return  new string[]{data[0],data[2]};
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
            return "ilan";
        }
        #endregion

                [Obsolete]
        private void InitWidgets()
        {
            tvStatus = (TextView)FindViewById(Resource.Id.tvStatus);
            lvDevices = (ListView)FindViewById(Resource.Id.lvDeviecs);
            btCamera = (Button)FindViewById(Resource.Id.btnTakePic);
            btGetCon = (Button)FindViewById(Resource.Id.btnGetCon);
            btSearch = (Button)FindViewById(Resource.Id.btnSearch);
            iv = (ImageView)FindViewById(Resource.Id.iv);
            tv = (TextView)FindViewById(Resource.Id.tvPathFile);
            btCamera.Click += BtnTakePic_Click;
            btGetCon.Click += BtGetCon_Click;
            btSearch.Click += BtSearch_Click;
            lvDevices.ItemClick += LvDevices_ItemClick;
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