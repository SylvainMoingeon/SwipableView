using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace SwipableSample
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged<T>(ref T prop, T val, [CallerMemberName] String propName = "")
        {
            if ((prop == null && val != null) || (prop != null && !prop.Equals(val)))
            {
                prop = val;
                RaisePropertyChanged(propName);
            }
        }

        public void RaisePropertyChanged(String propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
