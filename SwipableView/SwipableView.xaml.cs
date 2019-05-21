using System;
using System.ComponentModel;
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

        #region Events
        public event EventHandler<EventArgs> Opening;

        protected virtual void OnOpening(EventArgs e)
        {
            Opening?.Invoke(this, e);
        }

        public event EventHandler<EventArgs> Closing;

        protected virtual void OnClosing(EventArgs e)
        {
            Closing?.Invoke(this, e);
        }
        #endregion

        #region enums
        /// <summary>
        /// Swiping State. Are panels opening or closing. Or not swiping.
        /// </summary>
        private enum SwipeAction
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
        private enum PanelState
        {
            [Description("left and right Panels closed")]
            Closed = 0, // valeur par défaut, ne pas modifier !

            [Description("Right panel opened")]
            RightPanelOpened,

            [Description("Left panel opened")]
            LeftPanelOpened
        }
        #endregion

        #region Properties
        public bool IsClosed => _panelState == PanelState.Closed;
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
        private SwipeAction _swipeAction;

        /// <summary>
        /// Panels state
        /// </summary>
        private PanelState _panelState;

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

            new SwipeListener(this, this);

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

                await ValidateSwipeAsync(CenterPanel.TranslationX, SwipeValidationThreshold, SwipeOffset, _swipeAction);

                _swipeAction = SwipeAction.NotSwiping;
                IsSwipping = false;

                if (ParentListView != null)
                {
                    // Avoid bugs with listView when IsPullToRefreshEnabled == true
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
        /// <param name="swipeX">Amount of swipe on X axis</param>
        /// <param name="swipeY">Amount of swipe on Y axis</param>
        public void OnSwiping(View view, double swipeX, double swipeY)
        {
            if (_disablePanGesture)
                return;

            // Avoids false positives
            if (Math.Abs(swipeX) < 0.005)
                return;

            IsSwipping = true;

            _swipeAction = GetSwipingState(CenterPanel.TranslationX, swipeX);

            SetPanelsVisibility(_swipeAction);

            if ((_hasLeftPanel && (_swipeAction == SwipeAction.ClosingLeftPanel || _swipeAction == SwipeAction.OpeningLeftPanel))
                || (_hasRightPanel && (_swipeAction == SwipeAction.ClosingRightPanel || _swipeAction == SwipeAction.OpeningRightPanel)))
            {
                CenterPanel.TranslationX = GetTranslationToApply(swipeX, SwipeOffset, _translationOriginX);
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
        private void SetPanelsVisibility(SwipeAction swipeAction)
        {
            RightPanel.IsVisible = _hasRightPanel && (swipeAction == SwipeAction.ClosingRightPanel || swipeAction == SwipeAction.OpeningRightPanel);
            LeftPanel.IsVisible = _hasLeftPanel && (swipeAction == SwipeAction.ClosingLeftPanel || swipeAction == SwipeAction.OpeningLeftPanel);
        }
        #endregion

        #region Translation Management
        /// <summary>
        /// Apply translation to the Center Panel
        /// </summary>
        /// <param name="swipeX">Amount of swipe on X axis</param>
        /// <param name="swipeOffset">Maximum translation allowed.</param>
        /// <param name="translationOriginX"></param> 
        /// <returns>Translation to apply to center panel, according to amount of swipe and maximum translation allowed</returns>
        private double GetTranslationToApply(double swipeX, double swipeOffset, double translationOriginX)
        {
            double translationMax = Math.Sign(swipeX) * swipeOffset;
            double translationToApply = translationOriginX + swipeX;

            return (!_hasLeftPanel && translationToApply > 0) || (!_hasRightPanel && translationToApply < 0)
                ? 0
                : Math.Abs(translationToApply) >= Math.Abs(translationMax) ? translationMax : translationToApply;
        }

        /// <summary>
        /// Get current swiping state
        /// </summary>
        private SwipeAction GetSwipingState(double viewTranslation, double swipeX)
        {
            return Math.Sign(swipeX) > 0 ?
                ((viewTranslation >= 0) ? SwipeAction.OpeningLeftPanel : SwipeAction.ClosingRightPanel) :
                ((viewTranslation <= 0) ? SwipeAction.OpeningRightPanel : SwipeAction.ClosingLeftPanel);
        }
        #endregion

        #region Swipe validation
        /// <summary>
        /// Indicates if panel translation has reached the validation threshold
        /// </summary>
        /// <param name="panelTranslation">Current panel translation</param>
        /// <returns><see langword="true"/> if translationX has reached validation threshold. <see langword="false"/> otherwise</returns>
        private bool HasReachValidationThreshold(double panelTranslation, double validationThreshold)
        {
            return Math.Abs(panelTranslation) >= Math.Abs(validationThreshold);
        }

        /// <summary>
        /// Activate or cancel swipe according to panel translation and activation threshold.        
        /// </summary>
        /// <param name="panelTranslation">Panel translation</param>
        /// <param name="validationThreshold">Threshold from which swipe action is validated</param>
        /// <param name="swipeOffset">Amount of swipe allowed for the panel</param>
        /// <param name="swipeAction">Current action of the swipe</param>
        private async Task ValidateSwipeAsync(double panelTranslation, double validationThreshold, double swipeOffset, SwipeAction swipeAction)
        {
            // Cancel swipe => Closing panel
            if (swipeAction == SwipeAction.ClosingLeftPanel || swipeAction == SwipeAction.ClosingRightPanel || !HasReachValidationThreshold(panelTranslation, validationThreshold))
            {
                await ClosePanel();
            }
            // Activated swipe => Opening panel
            else
            {
                await OpenPanel(panelTranslation, swipeOffset);
            }
        }

        /// <summary>
        /// Open panel and fire PanelStateChanged event
        /// </summary>
        private async Task OpenPanel(double panelTranslation, double swipeOffset)
        {
            OnOpening(EventArgs.Empty);

            int sign = Math.Sign(panelTranslation);
            await CenterPanel.TranslateTo(sign * swipeOffset, 0);
            _panelState = sign == 1 ? PanelState.LeftPanelOpened : PanelState.RightPanelOpened;
        }

        /// <summary>
        /// Close panel and fire PanelStateChanged event
        /// </summary>
        public async Task ClosePanel()
        {
            OnClosing(EventArgs.Empty);

            await CenterPanel.TranslateTo(0, 0);
            _panelState = PanelState.Closed;
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