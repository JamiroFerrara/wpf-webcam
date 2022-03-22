using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;
using System.Linq;
using System;
using WCamController.Base;

namespace WCamController
{
    public class WebcamController
    {
        public string defaultCamName = "";
        private RelayCommand snapshotCommand;

        public WebcamController()
        {
            MediaDeviceList = GetDevices();
        }

        #region Parameters
        public IEnumerable<MediaInformation> MediaDeviceList { get; set; }
        public MediaInformation SelectedVideoDevice { get; set; } = new MediaInformation();
        public string SelectedVideoId { get { return SelectedVideoDevice != null ? SelectedVideoDevice.UsbId : ""; } }
        public ImageSource SnapshotTaken { get; set; }
        public Bitmap SnapshotBitmap { get; set; }
        #endregion

        #region Public Helpers
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
        public IEnumerable<MediaInformation> GetDevices()
        {
            return WebcamDevice.GetVideoDevices;
        }
        public void SelectDevice(MediaInformation device)
        {
            SelectedVideoDevice = device;
        }
        #endregion
    }
}
