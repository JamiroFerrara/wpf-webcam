using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WebcamControllerWPF.Base
{
    public class BaseViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };
        public event EventHandler OnResumeEvent = (sender, e) => { };

        public void OnPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
