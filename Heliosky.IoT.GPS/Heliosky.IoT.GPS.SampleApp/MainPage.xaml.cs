using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Heliosky.IoT.GPS.SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SerialGPS gps;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);
            gps = new SerialGPS(dis[0]);
            gps.FixDataReceived += Gps_FixDataReceived;
            gps.Start();

            statusTextBox.Text = "GPS Started";
        }

        private void Gps_FixDataReceived(object sender, FixDataReceivedEventArgs e)
        {
            statusTextBox.Text = "GPS Data Received at " + DateTime.Now.ToString();

            StringBuilder bldr = new StringBuilder();
            bldr.AppendLine("GPS Information");
            bldr.AppendLine("Latitude: " + e.Data.Latitude);
            bldr.AppendLine("Longitude: " + e.Data.Longitude);
            bldr.AppendLine("Time: " + e.Data.CurrentTime);
            bldr.AppendLine("Sattelite Used: " + e.Data.SateliteUsed);

            contentTextBox.Text = bldr.ToString();
        }
    }
}
