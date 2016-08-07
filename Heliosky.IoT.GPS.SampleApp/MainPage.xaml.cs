/* The MIT License (MIT)
 * MainPage.xaml.cs
 * Copyright (c) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;



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

            Configuration.Port cfg_prt = new Configuration.Port()
            {
                PortID = 1,
                StopBit = Configuration.Port.StopBitType.OneStop,
                Parity = Configuration.Port.ParityType.NoParity,
                CharacterLength = Configuration.Port.CharacterLengthType.Bit8,
                BaudRate = 115200,
                InputProtocol = Configuration.Port.Protocol.UBX,
                OutputProtocol = Configuration.Port.Protocol.UBX
            };

            gps = new UBXSerialGPS(dis[0], cfg_prt);
            gps.MessageReceived += Gps_MessageReceived;
            

            statusTextBox.Text = "GPS Started";

            await gps.Start();

            statusTextBox.Text = "GPS init completed";

            Configuration.Message cfg_msg = Configuration.Message.GetConfigurationForType<Navigation.GeodeticPosition>();

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
            Navigation.Clock resp = await gps.PollMessageAsync<Navigation.Clock>();

            if(resp != null)
            {
                statusTextBox.Text = "Poll message success: " + resp.TimeMillisOfWeek;
            }
            else
            {
                statusTextBox.Text = "Poll message failed";
            }

            var status = await gps.PollMessageAsync<Navigation.Status>();

            if (resp != null)
            {
                statusTextBox.Text = "Poll status success- Time to first fix: " + status.TimeToFirstFix;
            }
            else
            {
                statusTextBox.Text = "Poll status failed";
            }
        }

        private void Gps_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Navigation.GeodeticPosition pos = e.ReceivedMessage as Navigation.GeodeticPosition;

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
