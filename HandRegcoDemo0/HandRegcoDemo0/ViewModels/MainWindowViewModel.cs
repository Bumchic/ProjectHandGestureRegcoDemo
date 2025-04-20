using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;



namespace HandRegcoDemo0.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; } = "Welcome to Avalonia!";
        public MainWindowViewModel()
        {

            Task<DeviceInformationCollection> collection = AsyncFindAllDevice();
            DeviceInformationCollection devices = collection.Result;
            PrintDevices(devices).Start();
        }
        public async Task PrintDevices(DeviceInformationCollection devices)
        {
            foreach(DeviceInformation device in devices)
            {
                Debug.WriteLine(device.ToString());
            }
        }
            
            //foreach (DeviceInformation device in devices)
            //{
            //    Debug.WriteLine(devices.ToString());
            //}
        public async Task<DeviceInformationCollection> AsyncFindAllDevice()
        {
             DeviceInformationCollection collection = await DeviceInformation.FindAllAsync();
            return collection;
        }
    }
}
