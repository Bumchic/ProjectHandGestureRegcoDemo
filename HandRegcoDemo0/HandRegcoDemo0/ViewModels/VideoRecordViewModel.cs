using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using PixelFormat = Avalonia.Platform.PixelFormat;

namespace HandRegcoDemo0.ViewModels
{
    public partial class VideoRecorderViewModel : ViewModelBase, IDisposable
    {
        private VideoCapture _videoCapture;
        private VideoWriter _videoWriter;
        private CancellationTokenSource _previewCts;
        private CancellationTokenSource _recordCts;
        private bool _isRecording;

        [ObservableProperty]
        private WriteableBitmap previewBitmapImage;

        [ObservableProperty]
        private bool startButtonEnabled = true;

        [ObservableProperty]
        private bool stopButtonEnabled = false;

        [ObservableProperty]
        private bool recordButtonEnabled = false;

        [ObservableProperty]
        private bool stopRecordButtonEnabled = false;

        [ObservableProperty]
        private string recordingStatus = "Sẵn sàng";

        [ObservableProperty]
        private string recordingDuration = "00:00:00";

        [ObservableProperty]
        private int selectedCameraIndex;

        public ObservableCollection<string> CameraOptions { get; } = new();

        public VideoRecorderViewModel()
        {
            for (int i = 0; i < 5; i++)
            {
                CameraOptions.Add($"Camera {i}");
            }

            SelectedCameraIndex = 0;
        }

        [RelayCommand]
        public void StartCamera()
        {
            try
            {
                _videoCapture = new VideoCapture(SelectedCameraIndex);
                _previewCts = new CancellationTokenSource();
                Task.Run(() => CapturePreview(_previewCts.Token));

                StartButtonEnabled = false;
                StopButtonEnabled = true;
                RecordButtonEnabled = true;
                RecordingStatus = "Camera đã khởi động";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"Lỗi khởi tạo camera: {ex.Message}";
            }
        }

        private async Task CapturePreview(CancellationToken token)
        {
            var bitmap = new Mat();
            while (!token.IsCancellationRequested)
            {
                _videoCapture?.Read(bitmap);
                if (!bitmap.IsEmpty)
                {
                    var image = bitmap.ToImage<Bgra, byte>();
                    var bytes = image.Bytes;

                    var wb = new WriteableBitmap(
                        new PixelSize(image.Width, image.Height),
                        new Vector(96, 96),
                        PixelFormat.Bgra8888,
                        AlphaFormat.Premul);

                    using (var fb = wb.Lock())
                    {
                        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, fb.Address, bytes.Length);
                    }

                    PreviewBitmapImage = wb;
                }
                await Task.Delay(30);
            }
        }

        [RelayCommand]
        public void StopCamera()
        {
            try
            {
                if (_isRecording)
                {
                    StopRecording();
                }

                _previewCts?.Cancel();
                _videoCapture?.Dispose();
                _videoCapture = null;
                PreviewBitmapImage = null;

                ResetButtons();
                RecordingStatus = "Camera đã dừng";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"Lỗi dừng camera: {ex.Message}";
            }
        }

        [RelayCommand]
        public void StartRecording()
        {
            try
            {
                if (_videoCapture == null)
                {
                    RecordingStatus = "Camera chưa được bật";
                    return;
                }

                string filename = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.avi");

                int width = (int)_videoCapture.Get(CapProp.FrameWidth);
                int height = (int)_videoCapture.Get(CapProp.FrameHeight);
                double fps = _videoCapture.Get(CapProp.Fps);
                if (fps == 0) fps = 30;

                _videoWriter = new VideoWriter(filename, VideoWriter.Fourcc('X', 'V', 'I', 'D'), fps, new System.Drawing.Size(width, height), true);

                _recordCts = new CancellationTokenSource();
                _isRecording = true;
                Task.Run(() => RecordLoop(_recordCts.Token));
                Task.Run(() => StartRecordingTimer());

                RecordButtonEnabled = false;
                StopRecordButtonEnabled = true;
                RecordingStatus = "Đang ghi hình...";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"Lỗi ghi hình: {ex.Message}";
            }
        }

        private async Task RecordLoop(CancellationToken token)
        {
            var frame = new Mat();
            while (!token.IsCancellationRequested && _videoCapture != null)
            {
                _videoCapture.Read(frame);
                if (!frame.IsEmpty)
                {
                    _videoWriter?.Write(frame);
                }
                await Task.Delay(30);
            }
        }

        private async Task StartRecordingTimer()
        {
            var startTime = DateTime.Now;
            while (_isRecording)
            {
                await Task.Delay(1000);
                var elapsed = DateTime.Now - startTime;
                RecordingDuration = elapsed.ToString(@"hh\:mm\:ss");
            }
        }

        [RelayCommand]
        public void StopRecording()
        {
            try
            {
                _recordCts?.Cancel();
                _videoWriter?.Dispose();
                _videoWriter = null;
                _isRecording = false;

                RecordingDuration = "00:00:00";
                StopRecordButtonEnabled = false;
                RecordButtonEnabled = true;
                RecordingStatus = "Đã dừng ghi hình";
            }
            catch (Exception ex)
            {
                RecordingStatus = $"Lỗi dừng ghi hình: {ex.Message}";
            }
        }

        private void ResetButtons()
        {
            StartButtonEnabled = true;
            StopButtonEnabled = false;
            RecordButtonEnabled = false;
            StopRecordButtonEnabled = false;
        }

        public void Dispose()
        {
            StopRecording();
            StopCamera();
        }
    }
}
