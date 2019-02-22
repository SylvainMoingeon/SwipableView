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
        #region Constantes
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
        /// Translation threshold to validate swipe action (fraction of the view width)
        /// </summary>
        private const double _validationThreshold = 1d / 3d;

        /// <summary>
        /// Maximum translation allowed (fraction of the view width)
        /// </summary>        
        private const double _maximumTranslationAllowed = 2d / 3d;

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

            // Par défaut les panels ne sont pas visibles
            LeftPanel.IsVisible = false;
            RightPanel.IsVisible = false;

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
            // Resize the panels according to the width of the view and the maximum translation allowed
            if (Math.Abs(CenterPanel.Width - Width) > DOUBLE_COMPARAISON_EPSILON)
                CenterPanel.WidthRequest = Width;

            if (Math.Abs(LeftPanel.Width - (Width * _maximumTranslationAllowed)) > DOUBLE_COMPARAISON_EPSILON)
                LeftPanel.WidthRequest = Width * _maximumTranslationAllowed;

            if (Math.Abs(RightPanel.Width - (Width * _maximumTranslationAllowed)) > DOUBLE_COMPARAISON_EPSILON)
                RightPanel.WidthRequest = Width * _maximumTranslationAllowed;
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
                _panelsState = await ValidateSwipeAsync(CenterPanel.TranslationX, _validationThreshold, _swipingState);
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
        public void OnSwiping(View view, double translatedX)
        {
            if (_disablePanGesture)
                return;

            // Avoids false positives
            if (Math.Abs(translatedX) < 0.005)
                return;

            IsSwipping = true;

            _swipingState = GetSwipingState(CenterPanel.TranslationX, translatedX);
            SetPanelsVisibility();

            bool ok = ((_swipingState == SwipingState.ClosingLeftPanel || _swipingState == SwipingState.OpeningLeftPanel) && _hasLeftPanel)
                || ((_swipingState == SwipingState.ClosingRightPanel || _swipingState == SwipingState.OpeningRightPanel) && _hasRightPanel);

            if (ok)
            {
                ApplyTranslation(translatedX, _maximumTranslationAllowed, _translationOriginX);
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
        private void SetPanelsVisibility()
        {
            if (_hasLeftPanel)
                LeftPanel.IsVisible = CenterPanel.TranslationX > 0;

            if (_hasRightPanel)
                RightPanel.IsVisible = CenterPanel.TranslationX < 0;
        }
        #endregion

        #region Gestion de la translation
        /// <summary>
        /// Application de la translation sur la View.
        /// </summary>
        /// <param name="translatedX">Quantité dont l'utilisateur a swipé</param>
        /// <param name="translationLimit">Limite au delà de laquelle on stop le swipe. Exprimée en fraction de la largeur disponible</param>
        /// <returns>L'état courant du swipe : en train d'ouvrir/fermer à droite/gauche...</returns>
        private void ApplyTranslation(double translatedX, double translationLimit, double translationOriginX)
        {
            double translationMax = GetTranslationMax(GetSwipeDirection(translatedX), translationLimit);
            double translationToApply = GetTranslationToApply(translatedX, translationOriginX);

            CenterPanel.TranslationX = Math.Abs(translationToApply) >= Math.Abs(translationMax) ? translationMax : translationToApply;
        }

        /// <summary>
        /// Obtient la quantité de translation à appliquer à la View
        /// </summary>
        /// <param name="translatedX">Quantité swipée par l'utilisateur</param>
        /// <returns>Quantité de translation à appliquer à la View</returns>
        /// <remarks>Cette méthode est nécessaire car Android se comporte différemment d'iOS et UWP</remarks>
        private double GetTranslationToApply(double translatedX, double translationOriginX)
        {
            // Sous Android, on translate par rapport à la position courante de la View à translater
            //if (Device.RuntimePlatform == Device.Android)
            //    return CenterPanel.TranslationX + translatedX;

            return translationOriginX + translatedX;
        }

        /// <summary>
        /// Obtient la translation maximum autorisée
        /// </summary>
        /// <param name="swipeDirection">Direction du swipe</param>
        /// <param name="translationLimit">Limite de translation en fraction de la largeur disponible</param>
        /// <returns>Translation maximum autorisée</returns>
        private double GetTranslationMax(SwipeDirection swipeDirection, double translationLimit)
        {
            int direction = swipeDirection == SwipeDirection.FromLeftToRight ? 1 : -1;
            return direction * Width * translationLimit;
        }

        /// <summary>
        /// Get Swipe direction : from left to right, or from right to left
        /// </summary>
        /// <param name="translatedX">Translated X</param>
        /// <returns></returns>
        private SwipeDirection GetSwipeDirection(double translatedX)
        {
            return Math.Sign(translatedX) > 0 ? SwipeDirection.FromLeftToRight : SwipeDirection.FromRightToLeft;
        }

        /// <summary>
        /// Obtient l'état courant du swipe en fonction du dernier état connu pour les panneaux et de la direction de swipe
        /// </summary>
        private SwipingState GetSwipingState(double viewTranslation, double translatedX)
        {
            switch (GetSwipeDirection(translatedX))
            {
                case SwipeDirection.FromLeftToRight:
                    return (viewTranslation >= 0) ? SwipingState.OpeningLeftPanel : SwipingState.ClosingRightPanel;

                case SwipeDirection.FromRightToLeft:
                    return (viewTranslation <= 0) ? SwipingState.OpeningRightPanel : SwipingState.ClosingLeftPanel;

                default:
                    return SwipingState.NotSwiping;
            }
        }
        #endregion

        #region Validation du Swipe
        /// <summary>
        /// Détermine si la translation passée en paramètre est au-delà du seuil définit pour valider un swipe
        /// </summary>
        /// <param name="translationX">Translation effectuée</param>
        /// <returns>Vrai si la translation a dépassé le seuil</returns>
        private bool HasReachValidationThreshold(double translationX, double validationThreshold)
        {
            return Math.Sign(translationX) > 0 ? translationX > Width * validationThreshold : translationX < -Width * validationThreshold;
        }

        /// <summary>
        /// Valide le swipe en fonction de la translation effectuée et du seuil défini.
        /// Si le swipe est validé, le panel est translaté jusqu'à  la limité fixée. Sinon, il retourne à sa place de départ.
        /// </summary>
        /// <param name="translationX">Translation effectuée par le swipe</param>
        /// <param name="validationThreshold">Seuil à partir duquel le swipe est validé</param>
        private async Task<PanelsState> ValidateSwipeAsync(double translationX, double validationThreshold, SwipingState swipingState)
        {
            // Annule le swipe => referme le panneau
            if (swipingState == SwipingState.ClosingLeftPanel || swipingState == SwipingState.ClosingRightPanel || !HasReachValidationThreshold(translationX, validationThreshold))
            {
                _translationOriginX = 0;
                await CenterPanel.TranslateTo(0, 0);
                return PanelsState.Closed;
            }

            // On valide le swipe en cours => ouvre complètement le panneau
            int sign = Math.Sign(translationX);
            _translationOriginX = sign * Width * _maximumTranslationAllowed;
            await CenterPanel.TranslateTo(_translationOriginX, 0);
            return GetSwipeDirection(translationX) == SwipeDirection.FromLeftToRight ? PanelsState.LeftPanelOpened : PanelsState.RightPanelOpened;
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

        #region ParentScrollView
        // Bindable property
        public static readonly BindableProperty ParentScrollViewProperty =
          BindableProperty.Create(
             propertyName: nameof(ParentScrollView),
             declaringType: typeof(SwipableView),
             returnType: typeof(Xamarin.Forms.ScrollView),
             defaultValue: null,
             defaultBindingMode: BindingMode.OneWay,
             propertyChanged: (bindable, oldValue, newValue) =>
             { });

        // Gets or sets value of this BindableProperty
        public Xamarin.Forms.ScrollView ParentScrollView
        {
            get => (Xamarin.Forms.ScrollView)GetValue(ParentScrollViewProperty);
            set => SetValue(ParentScrollViewProperty, value);
        }
        #endregion   
        #endregion
    }
}