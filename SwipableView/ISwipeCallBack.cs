using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace SmoDev.Swipable
{
    internal interface ISwipeCallBack
    {
        void OnSwipeStarted(View view);
        void OnSwipeCompleted(View view);
        void OnSwiping(View view, double translatedX);
    }
}
