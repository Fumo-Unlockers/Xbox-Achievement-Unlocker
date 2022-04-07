using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Memory;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Xbox_Achievement_Unlocker
{
    public partial class MainWindow : Form
    {
        public Mem m = new Mem();
        bool attached = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BGWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while(true)
            {
                if (!m.OpenProcess("XboxAppServices"))
                {
                    attached = false;
                    Thread.Sleep(1000);
                    continue;
                }
                attached = true;
                Thread.Sleep(1000);
                BGWorker.ReportProgress(0);
            }
        }

        private void MainWindow_Shown(object sender, EventArgs e)
        {
            BGWorker.RunWorkerAsync();
        }

        private void BGWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (attached)
            {
                LBL_Attached.Text = "Attached to: XboxAppServices.exe (" + m.GetProcIdFromName("XboxAppServices").ToString() + ")";
                LBL_Attached.ForeColor = Color.Green;
            }
            if (!attached)
            {
                LBL_Attached.Text = "Not attached to Xbox App";
                LBL_Attached.ForeColor = Color.Red;
            }
        }

        private void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BGWorker.RunWorkerAsync();
        }
        //define some xauth variables
        long XauthStartAddress;
        long XauthEndAddress;
        string XauthStartAddressHex;
        long XauthLength;
        string xauthtoken;
        private async void BTN_GrabXauth_Click(object sender, EventArgs e)
        {
            //scan for first part of xauth "Authorization: XBL3.0 x="
            XauthStartAddress = (await (m.AoBScan("41 75 74 68 6F 72 69 7A 61 74 69 6F 6E 3A 20 58 42 4C 33 2E 30 20 78 3D", true, true))).FirstOrDefault();
            XauthStartAddressHex = (XauthStartAddress + 15).ToString("X");
            //scan for the end of xauth "Content - Length: "
            IEnumerable<long> XauthEndScanList = await m.AoBScan("0D 0A 43 6F 6E 74 65 6E 74 2D 4C 65 6E 67 74 68 3A 20", true, true);
            foreach (var endaddr in XauthEndScanList.ToArray())
            {
                if (endaddr> XauthStartAddress)
                {
                    //find the closest end to the start of xauth to use as length
                    XauthEndAddress = endaddr;
                    XauthLength = (XauthEndAddress - XauthStartAddress-15);
                    break;
                }
            }
            //read the bytes into string
            xauthtoken = Encoding.ASCII.GetString(m.ReadBytes(XauthStartAddressHex,XauthLength));
            TXT_Xauth.Text ="xauth: " + xauthtoken;
        }

        private async void BTN_XUID_Click(object sender, EventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://peoplehub.xboxlive.com/users/me/people/search/decoration/detail,preferredColor?q=" + TXT_Gamertag.Text);
            request.Headers.Add("x-xbl-contract-version", "5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("accept-language", "en-GB");
            request.Headers.Add("Authorization", xauthtoken);
            request.Headers.Add("Host", "peoplehub.xboxlive.com");
            request.Headers.Add("Connection", "Keep-Alive");

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(reader.ReadToEnd().ToString());
                LBL_XUID.Text= "XUID: "+ Jsonresponse.people[0].xuid;
                Jsonresponse = null;
            }
            

        }
    }
}
