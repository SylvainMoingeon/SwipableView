using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Xamarin.Forms.Xaml;
using Application = Xamarin.Forms.Application;
using ListView = Xamarin.Forms.ListView;

namespace SmoDev.Swipable
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SwipableView : ContentView, ISwipeCallBack
    {
        #region Constants
        private const double DOUBLE_COMPARAISON_EPSILON = 0.01;
        #endregion

        #region enums
        /// <summary>
        /// Swiping State. Are panels opening or closing. Or not swiping.
        /// </summary>
        private enum SwipingState
        {
            [Description("Not swiping")]
            NotSwiping = 0,

            [Description("Opening the right panel (swiping from right to left)")]
            OpeningRightPanel,

            [Description("Opening the left panel (swiping from left to right)")]
            OpeningLeftPanel,

            [Description("Closing the right panel (swiping from left to right)")]
            ClosingRightPanel,

            [Description("Closing the left panel (swiping from right to left)")]
            ClosingLeftPanel,
        }

        /// <summary>
        /// Panels state. Are they opened or closed
        /// </summary>
        private enum PanelsState
        {
            [Description("left and right Panels closed")]
            Closed = 0, // valeur par défaut, ne pas modifier !

            [Description("Right panel opened")]
            RightPanelOpened,

            [Description("Left panel opened")]
            LeftPanelOpened
        }

        /// <summary>
        /// Swipe direction
        /// </summary>
        private enum SwipeDirection
        {
            FromLeftToRight,
            FromRightToLeft
        }
        #endregion

        #region Fields
        /// <summary>
        /// Starting X coordinate of translation
        /// </summary>
        private double _translationOriginX;

        private bool _ParentListView_IsPullToRefreshEnabled;

        /// <summary>
        /// Has user defined a left panel
        /// </summary>
        private bool _hasLeftPanel => LeftPanelView != null;

        /// <summary>
        /// Has user defined a right panel
        /// </summary>
        private bool _hasRightPanel => RightPanelView != null;

        /// <summary>
        /// Current swiping state
        /// </summary>
        private SwipingState _swipingState;

        /// <summary>
        /// Panels state
        /// </summary>
        private PanelsState _panelsState;

        /// <summary>
        ///  Indicates whether user is allowed to pan (for instance, disallowed is panel is self-closing or self-opening)
        /// </summary>
        private bool _disablePanGesture;
        #endregion

        #region Platform specific
        /// <summary>
        /// Android renderer need it to prevent scrollview from interfering with swipe
        /// </summary>
        public bool IsSwipping { get; private set; }

        /// <summary>
        /// Ensure that specific ios configuration is set only once
        /// </summary>
        private static bool _specificiOsConfigurationSet;
        #endregion

        public SwipableView()
        {
            InitializeComponent();

            var Swipelistener = new SwipeListener(this, this);
            LayoutChanged += SwipableView_LayoutChanged;

            // Avoid bugs on iOS when swiping inside a scrollview. Set only once (static method)
            if (!_specificiOsConfigurationSet)
            {
                _specificiOsConfigurationSet = true;
                Application.Current.On<Xamarin.Forms.PlatformConfiguration.iOS>().SetPanGestureRecognizerShouldRecognizeSimultaneously(true);
            }
        }

        private void SwipableView_LayoutChanged(object sender, EventArgs e)
        {
            // Resize the panels according to the width of the view and the swipe offset
            if (Math.Abs(CenterPanel.Width - Width) > DOUBLE_COMPARAISON_EPSILON)
                CenterPanel.WidthRequest = Width;

            if (Math.Abs(LeftPanel.Width - SwipeOffset) > DOUBLE_COMPARAISON_EPSILON)
                LeftPanel.WidthRequest = SwipeOffset;

            if (Math.Abs(RightPanel.Width - SwipeOffset) > DOUBLE_COMPARAISON_EPSILON)
                RightPanel.WidthRequest = SwipeOffset;
        }

        #region ISwipeCallBack implementation
        /// <summary>
        /// Swipe is completed.
        /// </summary>
        /// <param name="view">View on wich user swiped</param>
        public async void OnSwipeCompleted(View view)
        {
            try
            {
                _disablePanGesture = true;
                _panelsState = await ValidateSwipeAsync(CenterPanel.TranslationX, SwipeValidationThreshold, SwipeOffset, _swipingState);
                _swipingState = SwipingState.NotSwiping;

                IsSwipping = false;

                if (ParentListView != null)
                {
                    ParentListView.IsPullToRefreshEnabled = _ParentListView_IsPullToRefreshEnabled;
                }

                _disablePanGesture = false;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

        /// <summary>
        /// Swiping
        /// </summary>
        /// <param name="view">View on wich user is swiping</param>
        /// <param name="translatedX"></param>
        public void OnSwiping(View view, double translatedX, double translatedY)
        {
            if (_disablePanGesture)
                return;

            // Avoids false positives
            if (Math.Abs(translatedX) < 0.005)
                return;

            IsSwipping = true;

            _swipingState = GetSwipingState(CenterPanel.TranslationX, translatedX);

            SetPanelsVisibility(_swipingState);

            if ((_hasLeftPanel && (_swipingState == SwipingState.ClosingLeftPanel || _swipingState == SwipingState.OpeningLeftPanel))
                || (_hasRightPanel && (_swipingState == SwipingState.ClosingRightPanel || _swipingState == SwipingState.OpeningRightPanel)))
            {
                CenterPanel.TranslationX = GetTranslationToApply(translatedX, SwipeOffset, _translationOriginX);
            }
        }

        public void OnSwipeStarted(View view)
        {
            if (_disablePanGesture)
                return;

            _translationOriginX = CenterPanel.TranslationX;

            // Avoid bugs with listView when IsPullToRefreshEnabled == true
            if (ParentListView != null)
            {
                _ParentListView_IsPullToRefreshEnabled = ParentListView.IsPullToRefreshEnabled;
                ParentListView.IsPullToRefreshEnabled = false;
                ParentListView.Unfocus();
            }
        }

        /// <summary>
        /// Show or hide left/right panels depending on swipe state
        /// </summary>
        private void SetPanelsVisibility(SwipingState swipingState)
        {
            RightPanel.IsVisible = _hasRightPanel && (swipingState == SwipingState.ClosingRightPanel || swipingState == SwipingState.OpeningRightPanel);
            LeftPanel.IsVisible = _hasLeftPanel && (swipingState == SwipingState.ClosingLeftPanel || swipingState == SwipingState.OpeningLeftPanel);
        }
        #endregion

        #region Translation Management
        /// <summary>
        /// Apply translation to the Center Panel
        /// </summary>
        /// <param name="translatedX"></param>
        /// <param name="swipeOffset">Maximum translation allowed.</param> 
        /// <returns>Translation to apply to center panel, according to translatedX and maximum translation allowed</returns>
        private double GetTranslationToApply(double translatedX, double swipeOffset, double translationOriginX)
        {
            double translationMax = Math.Sign(translatedX) * swipeOffset;
            double translationToApply = translationOriginX + translatedX;

            return Math.Abs(translationToApply) >= Math.Abs(translationMax) ? translationMax : translationToApply;
        }

        /// <summary>
        /// Get current swiping state
        /// </summary>
        private SwipingState GetSwipingState(double viewTranslation, double translatedX)
        {
            switch (Math.Sign(translatedX))
            {
                case 1:
                    return (viewTranslation >= 0) ? SwipingState.OpeningLeftPanel : SwipingState.ClosingRightPanel;

                case -1:
                    return (viewTranslation <= 0) ? SwipingState.OpeningRightPanel : SwipingState.ClosingLeftPanel;

                default:
                    return SwipingState.NotSwiping;
            }
        }
        #endregion

        #region Swipe validation
        /// <summary>
        /// Indicates if panel translation has reached the validation threshold
        /// </summary>
        /// <param name="translationX"></param>
        /// <returns><see langword="true"/> if translationX has reached validation threshold. <see langword="false"/> otherwise</returns>
        private bool HasReachValidationThreshold(double translationX, double validationThreshold)
        {
            return Math.Abs(translationX) >= Math.Abs(validationThreshold);
        }

        /// <summary>
        /// Activate or cancel swipe according to translation and activation threshold.        
        /// </summary>
        /// <param name="panelTranslation">Panel translation</param>
        /// <param name="validationThreshold">Threshold from which swipe action is validated</param>
        private async Task<PanelsState> ValidateSwipeAsync(double panelTranslation, double validationThreshold, double swipeOffset, SwipingState swipingState)
        {
            // Cancel swipe => Closing panel
            if (swipingState == SwipingState.ClosingLeftPanel || swipingState == SwipingState.ClosingRightPanel || !HasReachValidationThreshold(panelTranslation, validationThreshold))
            {
                await CenterPanel.TranslateTo(0, 0);
                return PanelsState.Closed;
            }

            // Activated swipe => Opening panel
            int sign = Math.Sign(panelTranslation);
            await CenterPanel.TranslateTo(sign * swipeOffset, 0);
            return sign == 1 ? PanelsState.LeftPanelOpened : PanelsState.RightPanelOpened;
        }
        #endregion

        #region Bindable Properties
        #region LeftPanelView
        // Bindable property
        public static readonly BindableProperty LeftPanelViewProperty =
          BindableProperty.Create(
             propertyName: nameof(LeftPanelView),
             declaringType: typeof(SwipableView),
             returnType: typeof(View),
             defaultBindingMode: BindingMode.OneWay,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                ((SwipableView)bindable).LeftPanel.Content = (View)newValue;
            });

        // Gets or sets value of this BindableProperty
        public View LeftPanelView
        {
            get => (View)GetValue(LeftPanelViewProperty);
            set => SetValue(LeftPanelViewProperty, value);
        }
        #endregion

        #region RightPanelView
        // Bindable property
        public static readonly BindableProperty RightPanelViewProperty =
          BindableProperty.Create(
             propertyName: nameof(RightPanelView),
             declaringType: typeof(SwipableView),
             returnType: typeof(View),
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             {
                 ((SwipableView)bindable).RightPanel.Content = (View)newValue;
             }
        );

        // Gets or sets value of this BindableProperty
        public View RightPanelView
        {
            get => (View)GetValue(RightPanelViewProperty);
            set => SetValue(RightPanelViewProperty, value);
        }
        #endregion

        #region CenterPanelView
        // Bindable property
        public static readonly BindableProperty CenterPanelViewProperty =
          BindableProperty.Create(
             propertyName: nameof(CenterPanelView),
             declaringType: typeof(SwipableView),
             returnType: typeof(View),
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             {
                 ((SwipableView)bindable).CenterPanel.Content = (View)newValue;
             });

        // Gets or sets value of this BindableProperty
        public View CenterPanelView
        {
            get => (View)GetValue(CenterPanelViewProperty);
            set => SetValue(CenterPanelViewProperty, value);
        }
        #endregion

        #region ParentListView
        // Bindable property
        public static readonly BindableProperty ParentListViewProperty =
          BindableProperty.Create(
             propertyName: nameof(ParentListView),
             declaringType: typeof(SwipableView),
             returnType: typeof(ListView),
             defaultValue: null,
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             { });

        // Gets or sets value of this BindableProperty
        public ListView ParentListView
        {
            get => (ListView)GetValue(ParentListViewProperty);
            set => SetValue(ParentListViewProperty, value);
        }
        #endregion

        #region SwipeValidationThreshold
        // Bindable property
        public static readonly BindableProperty SwipeValidationThresholdProperty =
          BindableProperty.Create(
             propertyName: nameof(SwipeValidationThreshold),
             declaringType: typeof(SwipableView),
             returnType: typeof(double),
             defaultValue: 50d,
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             { });

        // Gets or sets value of this BindableProperty
        public double SwipeValidationThreshold
        {
            get => (double)GetValue(SwipeValidationThresholdProperty);
            set => SetValue(SwipeValidationThresholdProperty, value);
        }
        #endregion

        #region SwipeOffset
        // Bindable property
        public static readonly BindableProperty SwipeOffsetProperty =
          BindableProperty.Create(
             propertyName: nameof(SwipeOffset),
             declaringType: typeof(SwipableView),
             returnType: typeof(double),
             defaultValue: 100d,
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             { });

        // Gets or sets value of this BindableProperty
        public double SwipeOffset
        {
            get => (double)GetValue(SwipeOffsetProperty);
            set => SetValue(SwipeOffsetProperty, value);
        }
        #endregion
        #endregion
    }
}