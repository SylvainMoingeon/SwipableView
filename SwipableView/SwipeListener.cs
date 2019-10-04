using System;
using Xamarin.Forms;

namespace SmoDev.Swipable
{
    public class SwipeListener : PanGestureRecognizer
    {
        private readonly ISwipeCallBack mISwipeCallback;

        /// <summary>
        /// Swipelistener constructor
        /// </summary>
        /// <param name="view">View that is listened for swipe</param>
        /// <param name="iSwipeCallBack">Class where the swipecallback is implemented</param>
        internal SwipeListener(View view, ISwipeCallBack iSwipeCallBack)
        {
            mISwipeCallback = iSwipeCallBack;

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;

            var TapGesture = new TapGestureRecognizer
            {
                NumberOfTapsRequired = 1
            };

            TapGesture.Tapped += TapGesture_Tapped;

            view.GestureRecognizers.Add(panGesture);
            view.GestureRecognizers.Add(TapGesture);
        }

        private void TapGesture_Tapped(object sender, EventArgs e)
        {
            mISwipeCallback.OnTapped();
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var Content = (View)sender;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    mISwipeCallback.OnSwipeStarted(Content);
                    break;

                case GestureStatus.Running:
                    mISwipeCallback.OnSwiping(Content, e.TotalX, e.TotalY);
                    break;

                case GestureStatus.Completed:
                    mISwipeCallback.OnSwipeCompleted(Content);
                    break;
            }
        }
    }
}
