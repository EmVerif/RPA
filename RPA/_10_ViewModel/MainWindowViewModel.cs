using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using ICSharpCode.AvalonEdit.Document;

using RPA._20_Model;

namespace RPA._10_ViewModel
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Button Button { get; } = new Button();
        public string ButtonDisplay
        {
            get
            {
                if (Model.Instance.IsScriptRunning)
                {
                    return "Script 終了";
                }
                else
                {
                    return "Script 開始";
                }
            }
        }
        public TextDocument Script
        {
            get
            {
                return Model.Instance.Script;
            }
        }
        public Boolean IsReadOnly
        {
            get
            {
                return Model.Instance.IsReadOnly;
            }
        }
        public string Log
        {
            get
            {
                return Model.Instance.Log;
            }
        }

        public MainWindowViewModel()
        {
            Model.Instance.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(Model.Instance.IsScriptRunning):
                    OnPropertyChanged(nameof(this.ButtonDisplay));
                    break;
                case nameof(Model.Instance.Log):
                    OnPropertyChanged(nameof(this.Log));
                    break;
                case nameof(Model.Instance.Script):
                    OnPropertyChanged(nameof(this.Script));
                    break;
                case nameof(Model.Instance.IsReadOnly):
                    OnPropertyChanged(nameof(this.IsReadOnly));
                    break;
                default:
                    break;
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
