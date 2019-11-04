using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Foundation;
using SmoDev.Swipable;
using SmoDev.Swipable.iOS;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(SwipableView), typeof(SwipableViewRenderer))]
namespace SmoDev.Swipable.iOS
{
    public class SwipableViewRenderer : ViewRenderer<SwipableView, UIView>
    {
        private ScrollViewRenderer _ScrollViewRenderer;

        public ScrollViewRenderer ScrollViewRenderer
        {
            get => _ScrollViewRenderer;
            set
            {
                bool subscribeEvent = _ScrollViewRenderer == null && value != null;
                bool unsubscribeEvent = _ScrollViewRenderer != null && value != _ScrollViewRenderer;

                _ScrollViewRenderer = value;

                if (unsubscribeEvent)
                {
                    ScrollViewRenderer.DraggingEnded -= ScrollViewRenderer_DraggingEnded;
                    ScrollViewRenderer.DraggingStarted -= ScrollViewRenderer_DraggingStarted;
                }

                if (subscribeEvent)
                {
                    ScrollViewRenderer.DraggingEnded += ScrollViewRenderer_DraggingEnded;
                    ScrollViewRenderer.DraggingStarted += ScrollViewRenderer_DraggingStarted;
                }
            }
        }

        public static void Initialize()
        {
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            // We need to get root scrollview if exists. SuperViews are not initialized during OnElementChanged, we try to get it here.
            // It's ok when (X, Y or Width) SwipableView properties changed, we can get scrollview            
            if (sender is SwipableView swipableView)
            {
                if (ScrollViewRenderer == null)
                    ScrollViewRenderer = GetParentScrollView(NativeView);

                // Cannot scroll while swiping
                if (ScrollViewRenderer != null && e.PropertyName.Equals(nameof(SwipableView.IsSwipping)))
                {
                    ScrollViewRenderer.ExclusiveTouch = swipableView.IsSwipping;
                    ScrollViewRenderer.ScrollEnabled = !swipableView.IsSwipping;
                }
            }
        }

        #region Cannot swipe while scrolling
        private void ScrollViewRenderer_DraggingStarted(object sender, EventArgs e)
        {
            Element.DisallowSwipe = true;
        }

        private void ScrollViewRenderer_DraggingEnded(object sender, DraggingEventArgs e)
        {
            Element.DisallowSwipe = false;
        }
        #endregion

        private ScrollViewRenderer GetParentScrollView(UIView view)
        {
            UIView t = view.Superview;

            while (t != null)
            {
                if (t.GetType() == typeof(ScrollViewRenderer))
                    return (ScrollViewRenderer)t;

                t = t.Superview;
            }

            return null;
        }
    }
}