using TakeSnapsWithWebcamUsingWpfMvvm.ViewModel;

namespace TakeSnapsWithWebcamUsingWpfMvvm
{
    /// <summary>
    /// Interaction logic for main window XAML.
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = new MainViewModel();
        }
    }
}