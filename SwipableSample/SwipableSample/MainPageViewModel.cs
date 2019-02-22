using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SwipableSample
{
    public class MainPageViewModel : ObservableObject
    {
        private ObservableCollection<String> _Budos;

        public ObservableCollection<String> Budos
        {
            get { return _Budos; }
            set { RaisePropertyChanged(ref _Budos, value); }
        }

        public MainPageViewModel()
        {
            Budos = new ObservableCollection<string>
            {
                "Yoseikan Budo",
                "Judo",
                "Aikido",
                "Gyokusin Ryu Aikido",
                "Aikido Yoshikan",
                "Iaido",
                "Kendo",
                "Kobudo",
                "Karatedo",
                "Nihon Jujutsu",
                "Aikibudo",
                "Dakaito Ryu"
            };
        }
    }
}
