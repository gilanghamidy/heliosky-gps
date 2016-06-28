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
        private UBXSerialGPS gps;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string aqs = SerialDevice.GetDeviceSelector();
            var dis = await DeviceInformation.FindAllAsync(aqs);

            UBX.ConfigPort cfg_prt = new UBX.ConfigPort()
            {
                PortID = 1,
                StopBit = UBX.ConfigPort.StopBitType.OneStop,
                Parity = UBX.ConfigPort.ParityType.NoParity,
                CharacterLength = UBX.ConfigPort.CharacterLengthType.Bit8,
                BaudRate = 115200,
                InputProtocol = UBX.ConfigPort.Protocol.UBX,
                OutputProtocol = UBX.ConfigPort.Protocol.UBX
            };

            gps = new UBXSerialGPS(dis[0], cfg_prt);

            statusTextBox.Text = "GPS Started";

            await gps.Start();

            statusTextBox.Text = "GPS init completed";

            gps.Listen();

            UBX.ConfigMessage cfg_msg = new UBX.ConfigMessage()
            {
                ClassID = 0x01,
                MessageID = 0x02,
                Rate = 2
            };

            gps.TransmitMessage(cfg_msg);
            
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
