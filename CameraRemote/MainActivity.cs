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
        Button btGetCon, btGetPic;
        ListView lvDevices, lvPictures; ImageView iv;

        private const string SERVER_IP = "192.168.1.28";
        private const int SERVER_PORT = 8820; private const int CHUNK = 1024;
        private const string SEPERATOR = "###"; private const int SIZE_OF_SIZE = 10;

        private string device_ip = ""; private string DeviceName = "";
        private List<string> AllDevices = new List<string>(); private int PhotosNumber=0;
        private TcpClient ServerTCP; private NetworkStream ServerStream;
        private new string[] FileList;

        [Obsolete]
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            InitWidgets();
            setPermissitios();
            CheckInstallIpWebcam();
            Search();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            SendData("DSCT", ServerStream);
            ServerStream.Close();
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
            //send wich device you want to connect to and get its details from server
            SendData("COND" + SEPERATOR + AllDevices[(int)e.Id], ServerStream);
            DeviceName = AllDevices[(int)e.Id];
            string s = "";
            while (s == "") s = ReceiveData(ServerStream);
            string[] IpRole = GetDeviceIpRole(s);

            if (IpRole[0] == "DADR" && IpRole[1] == "server")
            {
                //open the stream
                PackageManager pm = PackageManager;
                Intent intent = pm.GetLaunchIntentForPackage("com.pas.webcam");
                StartActivity(intent);
            }
            else if(IpRole[0] == "DUAA")
                Toast.MakeText(this, "the requested device is unavailable", ToastLength.Long).Show();

        }

        [Obsolete]
        private void LvPictures_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            SendData("GPIC" + SEPERATOR + FileList[(int)e.Id], ServerStream);
            byte[] data = new byte[SIZE_OF_SIZE + SEPERATOR.Length];
            ServerStream.Read(data);

            //get the len of the image
            string s = "";
            for (int i = 0; i < SIZE_OF_SIZE; i++)
                s += (char)data[i];
            int len = int.Parse(s);

            //read the bytes
            byte[] ByteData = new byte[len];
            for (int i = 0; i < len;)
                i += ServerStream.Read(ByteData,i,len-i);
            
            //save file to local
            string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
            System.IO.File.WriteAllBytes(root + FileList[(int)e.Id], ByteData);
            Toast.MakeText(this, "image was saved in the gallery", ToastLength.Long).Show();

            //show the image on the screen
            //Bitmap bt = BitmapFactory.DecodeByteArray(ByteData, 0, ByteData.Length);
            //iv.SetImageBitmap(bt);

            //covering the file list
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>(ApplicationContext, Android.Resource.Layout.SimpleListItem1, new string[0]);
            lvPictures.SetAdapter(arrayAdapter);

            //showing the buttons
            btGetCon.Visibility = Android.Views.ViewStates.Visible;
            btGetPic.Visibility = Android.Views.ViewStates.Visible;
        }
        [Obsolete]
        private void BtGetCon_Click(object sender, EventArgs e)
        {
            //get device details from server
            string s = "";
            while(s == "")  s = ReceiveData(ServerStream); 
            string[] IpRole = GetDeviceIpRole(s);

            if (s.StartsWith("DADR") && IpRole[1] == "client")
            {
                //connect to the stream
                var uri = Android.Net.Uri.Parse("http://" + device_ip + ":8080/browserfs.html");
                var intent = new Intent(Intent.ActionView, uri);
                StartActivity(intent);
            }

            ChangeLayout();
        }
        [Obsolete]
        private void BtGetPic_Click(object sender, EventArgs e)
        {
            if(FileList == null)
            {
                SendData("GPCL", ServerStream);

                //receive the file list
                string data = ReceiveData(ServerStream);
                FileList = data.Split(", ");
                for (int i = 0; i < FileList.Length; i++)
                    FileList[i] = FileList[i].Substring(1, FileList[i].Length - 2);

            }

            //show the list on the screen
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>(ApplicationContext, Android.Resource.Layout.SimpleListItem1, FileList);
            lvPictures.SetAdapter(arrayAdapter);

            //covering the buttons
            btGetCon.Visibility = Android.Views.ViewStates.Invisible;
            btGetPic.Visibility = Android.Views.ViewStates.Invisible;
        }
        private void BtnTakePic_Click(object sender, EventArgs e)
        {
            using (WebClient webClient = new WebClient())
            {
                //download image data from google
                byte[] dataArr = webClient.DownloadData("http://" + device_ip + ":8080/photo.jpg");

                //save file to local
                string root = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryPictures).ToString();
                string fname = "number " + (PhotosNumber++).ToString() + " " + DeviceName + ".jpg";
                System.IO.File.WriteAllBytes(root + fname, dataArr);

                //show the image on the screen
                Bitmap bt = BitmapFactory.DecodeByteArray(dataArr, 0, dataArr.Length);
                iv.SetImageBitmap(bt);

                //send to the server to save picture
                string s = "SPIC" + SEPERATOR + DeviceName + SEPERATOR;
                SendData(s, ServerStream);
            }
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


        #region GetMethods
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
            if (data[0] == "DADR")
            {
                device_ip = data[1];
                if (data.Length == 4)
                    return new string[] { data[0], data[2], data[3] };
                return new string[] { data[0], data[2] };
            }
            return data;
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
            lvPictures = (ListView)FindViewById(Resource.Id.lvPictures);
            btGetCon = (Button)FindViewById(Resource.Id.btnGetCon);
            btGetPic = (Button)FindViewById(Resource.Id.btnGetPic);
            iv = (ImageView)FindViewById(Resource.Id.ivImage);

            btGetCon.Click += BtGetCon_Click;
            btGetPic.Click += BtGetPic_Click;
            lvDevices.ItemClick += LvDevices_ItemClick;
            lvPictures.ItemClick += LvPictures_ItemClick;

        }


        [Obsolete]
        private void ChangeLayout()
        {
            //change the text and the OnClick of the button
            btGetCon.Text = "Take Picture";
            btGetCon.Click -= BtGetCon_Click;
            btGetCon.Click += BtnTakePic_Click;

            //clear lvDevices items
            List<string> a = new List<string>();
            ArrayAdapter<string> arrayAdapter = new ArrayAdapter<string>
                            (ApplicationContext, Android.Resource.Layout.SimpleListItem1, a);
            lvDevices.SetAdapter(arrayAdapter);
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
            for (int i = 0; i < data.Length && data[i] != 0; i++)
                s += (char)data[i];
            return s;
        }
        #endregion

    }
}