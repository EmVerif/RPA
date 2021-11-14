using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using RPA._20_Model;

namespace RPA._10_ViewModel
{
    class Button : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private bool _EnableFlag = true;

        public bool CanExecute(object parameter)
        {
            return _EnableFlag;
        }

        public void Execute(object parameter)
        {
            _EnableFlag = false;
            CanExecuteChanged?.Invoke(this, new EventArgs());
            Model.Instance.StartStopScript();
            _EnableFlag = true;
            CanExecuteChanged?.Invoke(this, new EventArgs());
        }
    }
}
