using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
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
        private MapIcon myLocation;
        private RandomAccessStreamReference mapIconStreamReference;

        public MainPage()
        {
            this.InitializeComponent();
            mapIconStreamReference = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/MapPin.png"));
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
            gps.MessageReceived += Gps_MessageReceived;
            

            statusTextBox.Text = "GPS Started";

            await gps.Start();

            statusTextBox.Text = "GPS init completed";

            UBX.ConfigMessage cfg_msg = new UBX.ConfigMessage()
            {
                ClassID = 0x01,
                MessageID = 0x02,
                Rate = 1
            };

            bool res = await gps.WriteConfigAsync(cfg_msg);

            if(res)
            {
                statusTextBox.Text = "Success configuring message";
                await Task.Delay(5000);
            }
            else
            {
                statusTextBox.Text = "Failed configuring message";
                await Task.Delay(5000);
            }

            statusTextBox.Text = "Polling message Monitor Receiver Status";
            UBX.NavigationClock resp = await gps.PollMessageAsync<UBX.NavigationClock>();

            if(resp != null)
            {
                statusTextBox.Text = "Poll message success: " + resp.TimeMillisOfWeek;
            }
            else
            {
                statusTextBox.Text = "Poll message failed";
            }
        }

        private void Gps_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            UBX.NavigationGeodeticPosition pos = e.ReceivedMessage as UBX.NavigationGeodeticPosition;

            if(pos != null)
            {
                statusTextBox.Text = "GPS Data Received at " + DateTime.Now.ToString();

                StringBuilder bldr = new StringBuilder();
                bldr.AppendLine("GPS Information");
                bldr.AppendLine("Latitude: " + pos.Latitude);
                bldr.AppendLine("Longitude: " + pos.Longitude);
                bldr.AppendLine("Time: " + pos.TimeMillisOfWeek);
                bldr.AppendLine("MSL: " + pos.HeightAboveSeaLevel);

                contentTextBox.Text = bldr.ToString();

                if(myLocation == null)
                {
                    mapView.Center = new Windows.Devices.Geolocation.Geopoint(pos.GetGeoposition());
                    mapView.ZoomLevel = 18;

                    myLocation = new MapIcon();
                    myLocation.Location = mapView.Center;
                    myLocation.NormalizedAnchorPoint = new Point(0.5, 1.0);
                    myLocation.Image = mapIconStreamReference;
                    myLocation.ZIndex = 0;
                    mapView.MapElements.Add(myLocation);
                }
                else
                {
                    myLocation.Location = new Windows.Devices.Geolocation.Geopoint(pos.GetGeoposition());
                }

                
                
            }
        }
    }
}
