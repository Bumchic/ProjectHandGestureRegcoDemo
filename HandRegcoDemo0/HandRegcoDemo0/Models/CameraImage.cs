using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.Playback;

namespace HandRegcoDemo0.Models
{
    class CameraImage
    {
        public VideoDeviceController media { get; set; }
        public MediaCapture mediaCapture { get; set; }
        public DeviceInformationCollection devices { get; set; }
        public MediaFrameSource frameSource { get; set; }
        public MediaPlayer mediaPlayer { get; set; }
        public MediaFrameReader mediaFrameReader { get; set; }
        public Avalonia.Media.Imaging.WriteableBitmap image { get; set; }
    }
}
