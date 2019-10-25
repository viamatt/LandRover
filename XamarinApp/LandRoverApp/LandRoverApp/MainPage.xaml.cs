using Acr.UserDialogs;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace LandRoverApp
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private readonly IUserDialogs _userDialogs;

        private IBluetoothLE Ble;
        private IAdapter BleAdapter;
        public bool IsRefreshing => BleAdapter.IsScanning;
        public bool IsStateOn => Ble.IsOn;
        public ObservableCollection<IDevice> Devices { get; set; } = new ObservableCollection<IDevice>();
        private CancellationTokenSource _cancellationTokenSource;
        private IDevice TargetDevice;

        public MainPage()
        {
            InitializeComponent();
            _userDialogs = UserDialogs.Instance;
            Ble = CrossBluetoothLE.Current;
            BleAdapter = CrossBluetoothLE.Current.Adapter;
            //  Ble.StateChanged += OnStateChanged;
            BleAdapter.DeviceDiscovered += OnDeviceDiscovered;
            BleAdapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
            BleAdapter.DeviceDisconnected += OnDeviceDisconnected;
            BleAdapter.DeviceConnectionLost += OnDeviceConnectionLost;

            StartLocationUpdates();
        }

        private async void StartLocationUpdates()
        {

            if (CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(5), 10, true);

            CrossGeolocator.Current.PositionChanged += PositionChanged;
            CrossGeolocator.Current.PositionError += PositionError;
        }

        private void PositionChanged(object sender, PositionEventArgs e)
        {

            //If updating the UI, ensure you invoke on main thread
            var position = e.Position;
            var output = "Full: Lat: " + position.Latitude + " Long: " + position.Longitude;
            output += "\n" + $"Time: {position.Timestamp}";
            output += "\n" + $"Heading: {position.Heading}";
            output += "\n" + $"Speed: {position.Speed}";
            output += "\n" + $"Accuracy: {position.Accuracy}";
            output += "\n" + $"Altitude: {position.Altitude}";
            output += "\n" + $"Altitude Accuracy: {position.AltitudeAccuracy}";
            Debug.WriteLine(output);
            int mph = (int)Math.Round(position.Speed * 2.23694);
            SpeedoAnnotationsLabel.Text = mph + " MPH";
            SpeedoGauge.Scales[0].Pointers[0].Value = mph;
        }

        private void PositionError(object sender, PositionErrorEventArgs e)
        {
            Debug.WriteLine(e.Error);
            //Handle event here for errors
        }

        async Task StopListening()
        {
            if (!CrossGeolocator.Current.IsListening)
                return;

            await CrossGeolocator.Current.StopListeningAsync();

            CrossGeolocator.Current.PositionChanged -= PositionChanged;
            CrossGeolocator.Current.PositionError -= PositionError;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            //  TryStartScanning(true);
        }


        public void ScanClicked(object sender, EventArgs e)
        {
            Console.WriteLine("Scan Clicked");
            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(() =>
            {
                TryStartScanning(true);

            });

        }

        private async void TryStartScanning(bool refresh = false)
        {
            //if (Xamarin.Forms.Device.RuntimePlatform == Device.Android)
            //{
            //    var status = await _permissions.CheckPermissionStatusAsync(Permission.Location);
            //    if (status != PermissionStatus.Granted)
            //    {
            //        var permissionResult = await _permissions.RequestPermissionsAsync(Permission.Location);

            //        if (permissionResult.First().Value != PermissionStatus.Granted)
            //        {
            //            await _userDialogs.AlertAsync("Permission denied. Not scanning.");
            //            _permissions.OpenAppSettings();
            //            return;
            //        }
            //    }
            //}

            if (IsStateOn && (refresh || !Devices.Any()) && !IsRefreshing)
            {
                ScanForDevices();
            }
        }

        private async void ScanForDevices()
        {
            Devices.Clear();

            foreach (var connectedDevice in BleAdapter.ConnectedDevices)
            {
                //update rssi for already connected evices (so tha 0 is not shown in the list)
                try
                {
                    await connectedDevice.UpdateRssiAsync();
                }
                catch (Exception ex)
                {
                    //  Trace.Message(ex.Message);
                    await _userDialogs.AlertAsync($"Failed to update RSSI for {connectedDevice.Name}");
                }

                AddOrUpdateDevice(connectedDevice);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            //  await RaisePropertyChanged(() => StopScanCommand);

            //  await RaisePropertyChanged(() => IsRefreshing);
            BleAdapter.ScanMode = ScanMode.LowLatency;
            await BleAdapter.StartScanningForDevicesAsync(null, null, true, _cancellationTokenSource.Token);
        }

        private void CleanupCancellationToken()
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            //  RaisePropertyChanged(() => StopScanCommand);
        }

        private void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {
            //  RaisePropertyChanged(() => IsRefreshing);

            CleanupCancellationToken();
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args)
        {
            AddOrUpdateDevice(args.Device);
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
            // Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();

            _userDialogs.HideLoading();
            _userDialogs.Toast($"Connection LOST {e.Device.Name}", TimeSpan.FromMilliseconds(6000));
        }
        private void OnDeviceDisconnected(object sender, DeviceEventArgs e)
        {
            // Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();
            _userDialogs.HideLoading();
            _userDialogs.Toast($"Disconnected {e.Device.Name}");

            Console.WriteLine($"Disconnected {e.Device.Name}");
        }

        private void AddOrUpdateDevice(IDevice device)
        {
            Xamarin.Essentials.MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (device.Name == "SH-HC-08")
                {

                    IService uartService = null;
                    ICharacteristic uartChar = null;
                    TargetDevice = device;
                    try
                    {
                        await BleAdapter.ConnectToDeviceAsync(TargetDevice);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    try
                    {
                        var services = await TargetDevice.GetServicesAsync();
                        foreach (var s in services)
                        {
                            Console.WriteLine(s.Id + " " + s.Name);
                        }
                        uartService = await TargetDevice.GetServiceAsync(Guid.Parse("0000ffe0-0000-1000-8000-00805F9B34FB"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    try
                    {
                        var chars = await uartService.GetCharacteristicsAsync();
                        foreach (var s in chars)
                        {
                            Console.WriteLine(s.Id + " " + s.Name);
                        }
                        uartChar = await uartService.GetCharacteristicAsync(Guid.Parse("0000ffe1-0000-1000-8000-00805F9B34FB"));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    if (uartChar == null)
                    {
                        Console.WriteLine("Char is null");
                        return;
                    }
                    try
                    {
                        uartChar.ValueUpdated += ValueUpdated;
                        await uartChar.StartUpdatesAsync();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                }

            });
        }

        private void ValueUpdated(object sender, CharacteristicUpdatedEventArgs args)
        {
            var bytes = args.Characteristic.Value;
            string str = System.Text.Encoding.UTF8.GetString(bytes);
            Console.WriteLine("BLE: " + str);
            var bits = str.Split(':').ToList();

            foreach (var result in bits)
            {
                switch (result.Substring(0, 1))
                {
                    case "r":

                        try
                        {
                            var rpm = double.Parse(result.Replace("r", ""));
                            TachoGaugeAnnotationsLabel.Text = rpm + " RPM";
                            TachoGauge.Scales[0].Pointers[0].Value = (rpm/100);
                        }
                        catch (Exception) { }
                        break;
                    case "v":
                        try
                        {
                            var volts = double.Parse(result.Replace("v", ""));
                            //double volts = (double)v / 1000;
                            VoltageGaugeAnnotationsLabel.Text = volts + " V";
                            VoltageGuage.Scales[0].Pointers[0].Value = volts;
                        }
                        catch (Exception) { }
                        break;
                }
            }

        }
    }
}
