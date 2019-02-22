using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SwipableSample
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void RightButton_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine("RIGHT BUTTON CLICKED");
        }

        private void LeftButton_Clicked(object sender, EventArgs e)
        {
            Console.WriteLine("LEFT BUTTON CLICKED");
        }
    }
}
