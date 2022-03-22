using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCamController;

namespace WebcamControllerWPF.ViewModel
{
    public class MainViewModel
    {
        public WebcamController WebcamController { get; set; } = new WebcamController();
        public MainViewModel()
        {
            var devices = WebcamController.GetDevices().ToList();
            WebcamController.SelectDevice(devices[0]);
        }
    }
}
