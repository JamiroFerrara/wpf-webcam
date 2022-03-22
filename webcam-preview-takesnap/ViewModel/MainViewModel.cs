using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Linq;
using TakeSnapsWithWebcamUsingWpfMvvm.Video;
using TakeSnapsWithWebcamUsingWpfMvvm.Base;
using System;

namespace TakeSnapsWithWebcamUsingWpfMvvm.ViewModel
{
    public class MainViewModel : BaseViewModel
    {
        private int videoPreviewWidth;
        private int videoPreviewHeight;

        private MediaInformation selectedVideoDevice;
        public MediaInformation SelectedVideoDevice
        {
            get
            {
                return this.selectedVideoDevice;
            }
            set
            {
                this.selectedVideoDevice = value;
                //this.RaisePropertyChanged(() => this.SelectedVideoDevice);
            }
        }

        private ImageSource snapshotTaken;
        private Bitmap snapshotBitmap;
        private IEnumerable<MediaInformation> mediaDeviceList;
        private RelayCommand snapshotCommand;
        private string defaultCamName = "USB2.0 HD UVC WebCam";

        public MainViewModel()
        {
            this.MediaDeviceList = WebcamDevice.GetVideoDevices;
            this.VideoPreviewWidth = 320;
            this.VideoPreviewHeight = 240;
            this.SelectedVideoDevice = null;

            if (IsCameraAvailable())
            {
                SelectedVideoDevice = this.MediaDeviceList.FirstOrDefault(x => x.DisplayName == defaultCamName);
            }
        }
        public bool IsCameraAvailable()
        {
            var devices = WebcamDevice.GetVideoDevices;
            if (devices.Count(x => x.DisplayName == defaultCamName) != 0)
                return true;
            else
                return false;
        }

        public int VideoPreviewWidth
        {
            get
            {
                return this.videoPreviewWidth;
            }
            set
            {
                this.videoPreviewWidth = value;
                //this.RaisePropertyChanged(() => this.VideoPreviewWidth);
            }
        }
        public int VideoPreviewHeight
        {
            get
            {
                return this.videoPreviewHeight;
            }
            set
            {
                this.videoPreviewHeight = value;
                //this.RaisePropertyChanged(() => this.VideoPreviewHeight);
            }
        }
        public ImageSource SnapshotTaken
        {
            get
            {
                return this.snapshotTaken;
            }
            set
            {
                this.snapshotTaken = value;
                //this.RaisePropertyChanged(() => this.SnapshotTaken);
            }
        }
        public Bitmap SnapshotBitmap
        {
            get
            {
                return this.snapshotBitmap;
            }
            set
            {
                this.snapshotBitmap = value;
                //this.RaisePropertyChanged(() => this.SnapshotBitmap);
            }
        }
        public IEnumerable<MediaInformation> MediaDeviceList
        {
            get
            {
                return this.mediaDeviceList;
            }
            set
            {
                this.mediaDeviceList = value;
                //this.RaisePropertyChanged(() => this.MediaDeviceList);
            }
        }
        public RelayCommand SnapshotCommand
        {
            get
            {
                return this.snapshotCommand ?? (this.snapshotCommand = new RelayCommand((Action) => { OnSnapshot(); }));
            }
        }
        private void OnSnapshot()
        {
            this.SnapshotTaken = ConvertToImageSource(this.SnapshotBitmap);
        }
        public static ImageSource ConvertToImageSource(Bitmap bitmap)
        {
            var imageSourceConverter = new ImageSourceConverter();
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                var snapshotBytes = memoryStream.ToArray();
                return (ImageSource)imageSourceConverter.ConvertFrom(snapshotBytes); ;
            }
        }

    }
}