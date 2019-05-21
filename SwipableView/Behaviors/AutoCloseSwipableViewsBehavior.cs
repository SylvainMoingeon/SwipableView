using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace SmoDev.Swipable.Behaviors
{
    /// <summary>
    /// Behavior to use with <see cref="SwipableView"/>. 
    /// This behavior auto-close opened <see cref="SwipableView"/> inside a Layout when you open a <see cref="SwipableView"/>.
    /// </summary>
    public class AutoCloseSwipableViewsBehavior : BehaviorBase<Layout<View>>
    {
        protected override void OnAttachedTo(Layout<View> bindable)
        {
            base.OnAttachedTo(bindable);

            // A ce niveau, les enfants du Layout ne sont pas encore instanciés. 
            // On s'abonne donc à ChildAdded et ChildRemove pour être averti de l'ajout et suppression d'enfant dans le Layout.
            bindable.ChildAdded += Bindable_ChildAdded;
            bindable.ChildRemoved += Bindable_ChildRemoved;
        }

        private void Bindable_ChildAdded(object sender, ElementEventArgs e)
        {
            if (e.Element is SwipableView swipableView)
            {
                swipableView.Opening += SwipableView_Opening;
            }
        }

        private void SwipableView_Opening(object sender, EventArgs e)
        {
            AssociatedObject.Children
                .Where(child => child is SwipableView swipableView && swipableView != sender && !swipableView.IsClosed)?
                .Cast<SwipableView>()
                .ForEach(async swipableView => await swipableView.ClosePanel());
        }

        private void Bindable_ChildRemoved(object sender, ElementEventArgs e)
        {
            if (e.Element is SwipableView swipableView)
            {
                swipableView.Closing -= SwipableView_Opening;
            }
        }

        protected override void OnDetachingFrom(Layout<View> bindable)
        {
            base.OnDetachingFrom(bindable);

            bindable.Children
                    .Where(child => child is SwipableView)?
                    .Cast<SwipableView>()
                    .ForEach(expandableView => expandableView.Closing -= SwipableView_Opening);
        }
    }
}
