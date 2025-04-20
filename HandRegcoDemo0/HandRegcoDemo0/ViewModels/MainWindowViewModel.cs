using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Avalonia.Threading;



namespace HandRegcoDemo0.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string Greeting { get; set; }
        public DeviceInformationCollection devices { get; set; }
        public MainWindowViewModel()
        {
            AsyncFindAllDevice();
        }


        public async Task AsyncFindAllDevice()
        {
             DeviceInformationCollection collection = await DeviceInformation.FindAllAsync();
            foreach(DeviceInformation device in collection)
            {
                Debug.WriteLine(device.GetType());
            }
        }
    }
}
