using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;

namespace SwipableSample
{
    public class MainPageViewModel : ObservableObject
    {
        private ObservableCollection<string> _Budos;

        public ObservableCollection<string> Budos
        {
            get { return _Budos; }
            set { RaisePropertyChanged(ref _Budos, value); }
        }

        public MainPageViewModel()
        {

            HelloCommand = new Command<string>(SayHello);
            MessageCommand = new Command<string>(SaySomething);

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

        private void SaySomething(string message)
        {
            MessagingCenter.Send(message, MessagingCenterMessages.SendMessage);
        }

        private void SayHello(string name)
        {
            SaySomething($"Hello {name} !");
        }

        public ICommand HelloCommand { get; set; }


        public ICommand MessageCommand { get; set; }
    }
}
