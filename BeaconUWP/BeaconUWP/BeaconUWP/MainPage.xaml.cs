using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniversalBeaconLibrary.Beacon;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeaconUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Bluetooth Beacons
        private readonly BluetoothLEAdvertisementWatcher _watcher;

        private readonly BeaconManager _beaconManager;

        private const int rssiValidity = 10;

        public MainPage()
        {
            // Construct the Universal Bluetooth Beacon manager
            _beaconManager = new BeaconManager();

            // Create & start the Bluetooth LE watcher from the Windows 10 UWP
            _watcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Passive};
            _watcher.Received += WatcherOnReceived;   
            _watcher.Start();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
           // WriteTask();
           while(true)
            {
                Message payload = new Message();
                payload.TimeStampMobile = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                payload.MapForSend = new List<Beacon>();
                foreach (var b in CommunicationHelper.Beacons)
                {
                    Debug.WriteLine($"Beacon {b.Device}, {b.Distance}");

                    if(b.Distance != 0)
                    {
                        payload.MapForSend.Add(new Beacon() { Device = b.Device, Distance = b.Distance });
                    }
                    if ((DateTime.Now - b.UpdatedAt).Seconds > rssiValidity)
                        //Remove value, and wait for the new one
                        b.Distance = 0;
                    
                }
                var payloadString = JsonConvert.SerializeObject(payload);
                await CommunicationHelper.CallEventHubHttpAsync(payloadString);
                await Task.Delay(2000);
            }
        }

        public async Task WriteTask()
        {
            var res = await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(false));
            Debug.WriteLine(res.Count());
        }

        private  void WatcherOnReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            //We need to create HEX representation of Mac Address split with ':'
            string addr = Regex.Replace(eventArgs.BluetoothAddress.ToString("X"), ".{2}", "$0:");
            addr = addr.Remove(addr.Length - 1, 1);
            var beacon = CommunicationHelper.Beacons.Where(b => b.Device == addr).FirstOrDefault();
            if (beacon != null)
            {
                beacon.Distance = eventArgs.RawSignalStrengthInDBm;
                beacon.UpdatedAt = DateTime.Now;
            }
        }
    }
}

