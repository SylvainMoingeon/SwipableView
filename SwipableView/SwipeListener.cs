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

            view.GestureRecognizers.Add(panGesture);
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
                    mISwipeCallback.OnSwiping(Content, e.TotalX);
                    break;

                case GestureStatus.Completed:
                    mISwipeCallback.OnSwipeCompleted(Content);
                    break;
            }
        }
    }
}
