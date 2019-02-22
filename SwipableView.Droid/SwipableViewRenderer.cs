using Android.Content;
using SmoDev.Swipable.Droid;
using SmoDev.Swipable;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using View = Android.Views.View;
using Android.Views;

[assembly: ExportRenderer(typeof(SwipableView), typeof(SwipableViewRenderer))]
namespace SmoDev.Swipable.Droid
{
    public class SwipableViewRenderer : ViewRenderer<SwipableView, View>
    {
        public SwipableViewRenderer(Context context) : base(context)
        {
        }

        private bool _disallowed;

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Move:
                case MotionEventActions.Down:
                    if (Element.IsSwipping && !_disallowed)
                    {
                        RequestDisallowInterceptTouchEvent(true);
                        _disallowed = true;
                        return true;
                    }
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
