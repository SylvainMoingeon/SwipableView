using Android.Content;
using SmoDev.Swipable.Droid;
using SmoDev.Swipable;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using View = Android.Views.View;
using Android.Views;
using System;

[assembly: ExportRenderer(typeof(SwipableView), typeof(SwipableViewRenderer))]
namespace SmoDev.Swipable.Droid
{
    public class SwipableViewRenderer : ViewRenderer<SwipableView, View>
    {
        float _startX, _startY;

        public SwipableViewRenderer(Context context) : base(context)
        {
        }

        private bool _disallowed;

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Move:
                    if (!Element.IsSwipping && Math.Abs(_startX - e.RawX) > Math.Abs(_startY - e.RawY))
                    {
                        RequestDisallowInterceptTouchEvent(true);
                        _disallowed = true;
                    }

                    if (Element.IsSwipping && !_disallowed)
                    {
                        RequestDisallowInterceptTouchEvent(true);
                        _disallowed = true;
                        return true;
                    }
                    break;

                case MotionEventActions.Down:
                    _startX = e.RawX;
                    _startY = e.RawY;
                    break;

                case MotionEventActions.Cancel:
                case MotionEventActions.Up:
                    RequestDisallowInterceptTouchEvent(false);
                    _disallowed = false;
                    break;
            }

            return base.OnTouchEvent(e);
        }
    }
}
