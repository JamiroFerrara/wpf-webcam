using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using AForge.Video.DirectShow;

using Drawing = System.Drawing;
using Point = System.Windows.Point;

namespace WCamController
{
    public partial class WebcamDevice : UserControl
    {
        #region Variable declaration

        public static readonly DependencyProperty VideoPreviewWidthProperty = DependencyProperty.Register("VideoPreviewWidth", typeof(double), typeof(WebcamDevice), new PropertyMetadata(VideoPreviewWidthPropertyChangedCallback));
        public static readonly DependencyProperty VideoPreviewHeightProperty = DependencyProperty.Register("VideoPreviewHeight", typeof(double), typeof(WebcamDevice), new PropertyMetadata(VideoPreviewHeightPropertyChangedCallback));
        public static readonly DependencyProperty VideoSourceIdProperty = DependencyProperty.Register("VideoSourceId", typeof(string), typeof(WebcamDevice), new PropertyMetadata(string.Empty, VideoSourceIdPropertyChangedCallback, VideoSourceIdPropertyCoherceValueChanged));
        public static readonly DependencyProperty SnapshotBitmapProperty = DependencyProperty.Register("SnapshotBitmap", typeof(Bitmap), typeof(WebcamDevice), new PropertyMetadata(SnapshotBitmapPropertyChangedCallback));
        public static readonly DependencyProperty TakeSnapshotProperty = DependencyProperty.RegisterAttached("TakeSnapshot", typeof(ICommand), typeof(WebcamDevice), new PropertyMetadata(default(TakeSnapshotCommand)));

        private VideoCaptureDevice videoCaptureDevice;
        private bool isVideoSourceInitialized;
        #endregion

        public WebcamDevice()
        {
            this.InitializeComponent();

            //// Subcribe to dispatcher shutdown event and dispose all used resources gracefully.
            this.Dispatcher.ShutdownStarted += this.DispatcherShutdownStarted;

            //// Initialize take snapshot command.
            this.TakeSnapshot = new TakeSnapshotCommand(this.TakeSnapshotCallback);
        }

        #region Properties

        public static IEnumerable<MediaInformation> GetVideoDevices
        {
            get
            {
                var filterVideoDeviceCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return (from FilterInfo filterInfo in filterVideoDeviceCollection select new MediaInformation { DisplayName = filterInfo.Name, UsbId = filterInfo.MonikerString }).ToList();
            }
        }
        [TypeConverter(typeof(LengthConverter))]
        public double VideoPreviewWidth
        {
            get
            {
                return (double)GetValue(VideoPreviewWidthProperty);
            }

            set
            {
                this.SetValue(VideoPreviewWidthProperty, value);
            }
        }

        [TypeConverter(typeof(LengthConverter))]
        public double VideoPreviewHeight
        {
            get
            {
                return (double)GetValue(VideoPreviewHeightProperty);
            }

            set
            {
                this.SetValue(VideoPreviewHeightProperty, value);
            }
        }
        public string VideoSourceId
        {
            get
            {
                return (string)GetValue(VideoSourceIdProperty);
            }
            set
            {
                this.SetValue(VideoSourceIdProperty, value);
            }
        }
        public Bitmap SnapshotBitmap
        {
            get
            {
                return (Bitmap)this.GetValue(SnapshotBitmapProperty);
            }
            set
            {
                this.SetValue(SnapshotBitmapProperty, value);
            }
        }
        public TakeSnapshotCommand TakeSnapshot
        {
            get
            {
                return (TakeSnapshotCommand)GetValue(TakeSnapshotProperty);
            }
            set
            {
                this.SetValue(TakeSnapshotProperty, value);
            }
        }
        #endregion

        #region Methods
        public void TakeSnapshotCallback()
        {
            try
            {
                var playerPoint = new Drawing.Point();

                //// Get the position of the source video device player.
                if (string.IsNullOrWhiteSpace(this.VideoSourceId))
                {
                    var noVideoDeviceSourcePoint = this.NoVideoSourceGrid.PointToScreen(new Point(0, 0));
                    playerPoint.X = (int)noVideoDeviceSourcePoint.X;
                    playerPoint.Y = (int)noVideoDeviceSourcePoint.Y;
                }
                else
                {
                    playerPoint = this.VideoSourcePlayer.PointToScreen(new Drawing.Point(this.VideoSourcePlayer.ClientRectangle.X, this.VideoSourcePlayer.ClientRectangle.Y));
                }

                if (double.IsNaN(this.VideoPreviewWidth) || double.IsNaN(this.VideoPreviewHeight))
                {
                    using (var bitmap = new Bitmap((int)this.VideoSourceWindowsFormsHost.ActualWidth, (int)this.VideoSourceWindowsFormsHost.ActualHeight))
                    {
                        using (var graphicsFromImage = Graphics.FromImage(bitmap))
                        {
                            graphicsFromImage.CopyFromScreen(playerPoint, Drawing.Point.Empty, new Drawing.Size((int)this.VideoSourceWindowsFormsHost.ActualWidth, (int)this.VideoSourceWindowsFormsHost.ActualHeight));
                        }

                        this.SnapshotBitmap = new Bitmap(bitmap);
                    }
                }
                else
                {
                    using (var bitmap = new Bitmap((int)this.VideoPreviewWidth, (int)this.VideoPreviewHeight))
                    {
                        using (var graphicsFromImage = Graphics.FromImage(bitmap))
                        {
                            graphicsFromImage.CopyFromScreen(playerPoint, Drawing.Point.Empty, new Drawing.Size((int)this.VideoPreviewWidth, (int)this.VideoPreviewHeight));
                        }

                        this.SnapshotBitmap = new Bitmap(bitmap);
                    }
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Error occurred while trying to take snapshot from currently selected source video device", exception);
            }
        }
        private static void VideoSourceIdPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var oldValue = eventArgs.OldValue as string;
            var newValue = eventArgs.NewValue as string;
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(newValue))
            {
                if (!string.IsNullOrWhiteSpace(oldValue))
                {
                    webCamDevice.InitializeVideoDevice(oldValue);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(oldValue))
                {
                    webCamDevice.InitializeVideoDevice(newValue);
                }
                else
                {
                    if (oldValue != newValue)
                    {
                        webCamDevice.isVideoSourceInitialized = false;
                    }

                    webCamDevice.InitializeVideoDevice(oldValue.Equals(newValue) ? oldValue : newValue);
                }
            }
        }
        private static object VideoSourceIdPropertyCoherceValueChanged(DependencyObject dependencyObject, object basevalue)
        {
            var baseValueStringFormat = Convert.ToString(basevalue, CultureInfo.InvariantCulture);
            var availableMediaList = GetVideoDevices;
            var mediaInfos = availableMediaList as IList<MediaInformation> ?? availableMediaList.ToList();
            if (string.IsNullOrEmpty(baseValueStringFormat) || !mediaInfos.Any())
            {
                return null;
            }

            var filteredVideoDevice = mediaInfos.FirstOrDefault(item => item.UsbId.Equals(baseValueStringFormat));
            return null != filteredVideoDevice ? filteredVideoDevice.UsbId : baseValueStringFormat;
        }
        private static void VideoPreviewWidthPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            var newValue = (double)eventArgs.NewValue;
            if (double.IsNaN(newValue))
            {
                var parentControl = (webCamDevice.VisualParent as Grid);
                webCamDevice.SetVideoPlayerWidth(null != parentControl ? parentControl.Width : newValue);
            }
            else
            {
                webCamDevice.SetVideoPlayerWidth(newValue);
            }
        }
        private static void VideoPreviewHeightPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            var webCamDevice = sender as WebcamDevice;
            if (null == webCamDevice)
            {
                return;
            }

            if (null == eventArgs.NewValue)
            {
                return;
            }

            var newValue = (double)eventArgs.NewValue;
            if (double.IsNaN(newValue))
            {
                var parentControl = (webCamDevice.VisualParent as Grid);
                webCamDevice.SetVideoPlayerHeight(null != parentControl ? parentControl.Height : newValue);
            }
            else
            {
                webCamDevice.SetVideoPlayerHeight(newValue);
            }
        }
        private static void SnapshotBitmapPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            //// NOTE: Created to make the dependency property bindable from view-model.
        }
        private void InitializeVideoDevice(string videoDeviceSourceId)
        {
            if (this.isVideoSourceInitialized)
            {
                return;
            }

            var errorAction = new Action(() => this.SetVideoPlayer(false, "Unable to set video device source"));
            this.ReleaseVideoDevice();
            if (string.IsNullOrEmpty(videoDeviceSourceId))
            {
                return;
            }

            if (videoDeviceSourceId.StartsWith("Message:", StringComparison.OrdinalIgnoreCase))
            {
                var splitString = videoDeviceSourceId.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length == 2)
                {
                    this.SetVideoPlayer(false, splitString[1]);
                }
                else
                {
                    this.SetVideoPlayer(false);
                }
            }
            else
            {
                try
                {
                    if (!GetVideoDevices.Any(item => item.UsbId.Equals(videoDeviceSourceId)))
                    {
                        return;
                    }

                    this.videoCaptureDevice = new VideoCaptureDevice(videoDeviceSourceId);
                    this.VideoSourcePlayer.VideoSource = this.videoCaptureDevice;
                    this.VideoSourcePlayer.Start();
                    this.isVideoSourceInitialized = true;
                    this.SetVideoPlayer(true);
                }
                catch (ArgumentNullException)
                {
                    errorAction();
                }
                catch (ArgumentException)
                {
                    errorAction();
                }
            }
        }
        private void SetVideoPlayerWidth(double newWidth)
        {
            this.NoVideoSourceGrid.Width = newWidth;
            this.VideoSourceWindowsFormsHost.Width = newWidth;
        }
        private void SetVideoPlayerHeight(double newHeight)
        {
            this.NoVideoSourceGrid.Height = newHeight;
            this.VideoSourceWindowsFormsHost.Height = newHeight;
        }
        private void WebcamDeviceOnLoaded(object sender, RoutedEventArgs eventArgs)
        {
            //// Set controls width / height based on VideoPreviewWidth / VideoPreviewHeight binding properties.
            this.NoVideoSourceGrid.Width = this.VideoPreviewWidth;
            this.VideoSourceWindowsFormsHost.Width = this.VideoPreviewWidth;
            this.NoVideoSourceGrid.Height = this.VideoPreviewHeight;
            this.VideoSourceWindowsFormsHost.Height = this.VideoPreviewHeight;
            this.InitializeVideoDevice(this.VideoSourceId);
        }
        private void ReleaseVideoDevice()
        {
            this.isVideoSourceInitialized = false;
            this.SetVideoPlayer(false);
            if (null == this.videoCaptureDevice)
            {
                return;
            }

            this.videoCaptureDevice.SignalToStop();
            this.videoCaptureDevice.WaitForStop();
            this.videoCaptureDevice.Stop();
            this.videoCaptureDevice = null;
        }
        private void SetVideoPlayer(bool isVideoSourceFound, string noVideoSourceMessage = "")
        {
            //// If video source found is true show the video source player or else show no video source message.
            if (isVideoSourceFound)
            {
                this.VideoSourceWindowsFormsHost.Visibility = Visibility.Visible;
                this.NoVideoSourceGrid.Visibility = Visibility.Hidden;
                this.NoVideoSourceMessage.Text = string.Empty;
            }
            else
            {
                this.VideoSourceWindowsFormsHost.Visibility = Visibility.Hidden;
                this.NoVideoSourceGrid.Visibility = Visibility.Visible;
                this.NoVideoSourceMessage.Text = string.IsNullOrWhiteSpace(noVideoSourceMessage) ? "No video source device found" : noVideoSourceMessage;
            }
        }
        private void DispatcherShutdownStarted(object sender, EventArgs eventArgs)
        {
            this.ReleaseVideoDevice();
        }
        private void WebcamDeviceOnUnloaded(object sender, RoutedEventArgs eventArgs)
        {
            this.ReleaseVideoDevice();
        }

        #endregion
    }

    public sealed class MediaInformation
    {
        public string DisplayName
        {
            get;
            set;
        }
        public string UsbId
        {
            get;
            set;
        }
    }
    public class MediaInformationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = string.Empty;
            if (null == value)
            {
                return result;
            }

            var filterInfo = value as MediaInformation;
            if (null != filterInfo)
            {
                result = filterInfo.UsbId;
            }

            return result;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TakeSnapshotCommand : ICommand
    {
        #region Variable declaration

        private readonly Action takeSnapshotAction;

        #endregion

        #region Constructor

        public TakeSnapshotCommand(Action takeSnapshotAction)
        {
            this.takeSnapshotAction = takeSnapshotAction;
        }
        #endregion

        #region Events

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
        #endregion

        #region Methods
        public bool CanExecute(object parameter)
        {
            return null != this.takeSnapshotAction;
        }
        public void Execute(object parameter)
        {
            if (null != this.takeSnapshotAction)
            {
                this.takeSnapshotAction();
            }
        }
        #endregion
    }
    public enum CameraStatus
    {
        Connected,
        Disconnected
    }
}
