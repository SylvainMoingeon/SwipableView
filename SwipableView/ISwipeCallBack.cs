using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace SmoDev.Swipable
{
    internal interface ISwipeCallBack
    {
        /// <summary>
        /// Swipe just start
        /// </summary>
        /// <param name="view">View on wich user begin to swipe</param>
        void OnSwipeStarted(View view);

        /// <summary>
        /// Swipe is completed.
        /// </summary>
        /// <param name="view">View on wich user swiped</param>
        void OnSwipeCompleted(View view);

        /// <summary>
        /// Swiping
        /// </summary>
        /// <param name="view">View on wich user is swiping</param>
        /// <param name="translatedX"></param>
        void OnSwiping(View view, double translatedX, double translatedY);
    }
}
