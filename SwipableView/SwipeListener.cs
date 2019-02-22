using System;
using Xamarin.Forms;

namespace SmoDev.Swipable
{
    public class SwipeListener : PanGestureRecognizer
    {
        private readonly ISwipeCallBack mISwipeCallback;
        private double translatedX;

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
                    translatedX = e.TotalX;
                    mISwipeCallback.OnSwiping(Content, e.TotalX);
                    break;

                case GestureStatus.Completed:
                    mISwipeCallback.OnSwipeCompleted(Content);
                    break;
            }
        }
    }
}
