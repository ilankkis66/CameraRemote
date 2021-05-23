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
        TextView tvStatus, tv;
        ListView lvDevices;
        ImageView iv;
        const string SERVER_IP = "192.168.1.28";
        const int SERVER_PORT = 8820; const int CHUNK = 1024;
        const string SEPERATOR = "###";
        const int devicePort = 6666;
        string device_ip = ""; 
        List<string> AllDevices; int PhotosNumber=0;
        string[] IpRole;
        TcpClient ServerTCP; NetworkStream ServerStream;
        TcpClient DeviceTcp; NetworkStream DeviceStream; string DeviceName="";

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

        }


        [Obsolete]
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 102)//coming from camera
            {
                if (resultCode == Result.Ok)
                {
                    Android.Graphics.Bitmap bitmap = (Android.Graphics.Bitmap)data.Extras.Get("data");
                    iv.SetImageBitmap(bitmap);
                    // ivCheck.SetImageBitmap(bitmap);
                    saveImageToExternalStorage_version1(bitmap);
                    if (ServerStream != null)
                    {
                        #region Get bytes
                        MemoryStream stream = new MemoryStream();
                        bitmap.Compress(CompressFormat.Jpeg, 100, stream);
                        byte[] ba = stream.ToArray();
                        #endregion
                        #region Send photo to server
                        string s = "SPIC" + SEPERATOR + DeviceName + SEPERATOR;
                        byte[] b = new byte[ba.Length + s.Length];
                        for (int i = 0; i < s.Length; i++)
                            b[i] = (byte)s[i];
                        for (int i = 0; i < ba.Length; i++)
                            b[i + s.Length] = ba[i];
                        SendData(b, ServerStream);
                        #endregion 
                        if (DeviceStream != null)
                            SendData(ba, DeviceStream);
                    }
                }
            }
            else if (requestCode == 101)
            {
                Android.Net.Uri videoUri = data.Data;
                string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString();
                string t = root + videoUri.Path;
                byte[] videoBytes = convertVideoToBytes(this, videoUri);
                byte[] adir = convetVideotoBytes(t);
                int i = 5;
            }
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
        private void saveImageToExternalStorage_version1(Android.Graphics.Bitmap finalBitmap)
        {
            string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
            Java.IO.File myDir = new Java.IO.File(root + "/saved_images");
            myDir.Mkdirs();
            System.Random generator = new System.Random();
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
                    finalBitmap.Compress(CompressFormat.Png, 90, fs);
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
            DeviceName = AllDevices[(int)e.Id];
            string s = "";
            while (s == "") s = ReceiveData(ServerStream);
            IpRole = GetDeviceIpRole(s);
            if (s.StartsWith("DADR"))
                if (IpRole[1] == "server")
                {
                    TcpListener tcp_device = new TcpListener(devicePort);
                    tcp_device.Start();
                    DeviceTcp = tcp_device.AcceptTcpClient();
                    DeviceStream = DeviceTcp.GetStream();
                    SendData("ilan", DeviceStream);
                    tvStatus.Text = "i am the server,  sent -----> ilan \nconnected to: " + DeviceName;
                    PackageManager pm = this.PackageManager;
                    Intent intent = pm.GetLaunchIntentForPackage("com.pas.webcam");
                    StartActivity(intent);
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
                    DeviceTcp = new TcpClient(device_ip, devicePort);
                    DeviceStream = DeviceTcp.GetStream();
                    DeviceName = IpRole[2];
                    string t = "";
                    while (t == "")
                        t = ReceiveData(DeviceStream);
                    tvStatus.Text = "connected to: "+ DeviceName + "\nreceived------>" + t;
                    var uri = Android.Net.Uri.Parse("http://" + device_ip + ":8080/browserfs.html");
                    var intent = new Intent(Intent.ActionView, uri); 
                    StartActivity(intent);
                }
        }
        private void BtnTakePic_Click(object sender, EventArgs e)
        {
            using (WebClient webClient = new WebClient())
            {
                byte[] dataArr = webClient.DownloadData("http://" + device_ip + ":8080/photo.jpg");
                //save file to local
                string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
                Java.IO.File myDir = new Java.IO.File(root + "/saved_images");
                myDir.Mkdirs();
                string fname = "number " + (PhotosNumber++).ToString() + " " + DeviceName + ".jpg";
                Java.IO.File file = new Java.IO.File(myDir, fname);
                System.IO.File.WriteAllBytes(root + "/saved_images" + fname, dataArr);
                string s = "SPIC" + SEPERATOR + DeviceName + SEPERATOR;
                byte[] send = new byte[dataArr.Length + s.Length];
                for (int i = 0; i < s.Length; i++)
                    send[i] = (byte)s[i];
                for (int i = 0; i < dataArr.Length; i++)
                    send[i + s.Length] = dataArr[i];
                SendData(send, ServerStream);
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
            using (WebClient webClient = new WebClient())
            {
                byte[] dataArr = webClient.DownloadData("http://" + "192.168.1.13" + ":8080/photo.jpg");
                //save file to local
                string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
                Java.IO.File myDir = new Java.IO.File(root + "/saved_images");
                myDir.Mkdirs();
                Java.IO.File file = new Java.IO.File(myDir, "ilankis.jpg");
                System.IO.File.WriteAllBytes(root + "/saved_images/ilankis.jpg", dataArr);
                string s = "SPIC" + SEPERATOR + GetDeviceName() + " " +GetDeviceMacAddress() + SEPERATOR;
                byte[] send = new byte[dataArr.Length + s.Length];
                for (int i = 0; i < s.Length; i++)
                    send[i] = (byte)s[i];
                for (int i = 0; i < dataArr.Length; i++)
                    send[i + s.Length] = dataArr[i];
                SendData(send, ServerStream);
            }
        }
        #endregion
        public static byte[] convertVideoToBytes(Context context, Android.Net.Uri uri)
        {
            byte[] videoBytes = null;
            try
            {//  w  w w  . j ava 2s . c  o m
                ByteArrayOutputStream baos = new ByteArrayOutputStream();

                var fis = new FileStream(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).ToString() + uri.Path, FileMode.Open);
                byte[] buf = new byte[1024];
                int n;
                while (-1 != (n = fis.Read(buf)))
                    baos.Write(buf, 0, n);

                videoBytes = baos.ToByteArray();
            }
            catch (System.IO.FileNotFoundException e)
            {
                Toast.MakeText(context, e.ToString(), ToastLength.Long).Show();
            }
            catch (System.IO.IOException e)
            {
                Toast.MakeText(context, e.ToString(), ToastLength.Long).Show();
            }
            return videoBytes;
        }

        public static byte[] convetVideotoBytes(string path)
        {
            byte[] myVideoByteArray = System.IO.File.ReadAllBytes(path);
            return myVideoByteArray;
        }

        #region GetMethod
        private void GetAllDevices(NetworkStream stream)
        {
            string d = "";
            while (!d.Contains("ENDD"))
                d += ReceiveData(stream);
            string[] a = d.Split(SEPERATOR);
            AllDevices = new List<string>();
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
            return "ilan";
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
            tvStatus = (TextView)FindViewById(Resource.Id.tvStatus);
            tv = (TextView)FindViewById(Resource.Id.tvPathFile);
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

        private void SendData(byte[] bytearray, NetworkStream stream)
        {
            stream.Write(bytearray);
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