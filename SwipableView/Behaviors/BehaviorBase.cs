using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace SmoDev.Swipable.Behaviors
{
    /// <summary>
    /// Classe de base pour les <see cref="Behavior{T}"/>.
    /// La classe expose en particulier une propriété <see cref="AssociatedObject"/> qui donne facilement accès à l'objet sur lequel est attaché le <see cref="Behavior"/>
    /// </summary>
    /// <typeparam name="T">Type de l'élément d'IHM sur lequel on attache le <see cref="Behavior"/></typeparam>
    public class BehaviorBase<T> : Behavior<T> where T : BindableObject
    {
        public T AssociatedObject { get; private set; }

        protected override void OnAttachedTo(T bindable)
        {
            base.OnAttachedTo(bindable);
            AssociatedObject = bindable;

            if (bindable.BindingContext != null)
            {
                BindingContext = bindable.BindingContext;
            }

            bindable.BindingContextChanged += OnBindingContextChanged;
        }

        protected override void OnDetachingFrom(T bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= OnBindingContextChanged;
            AssociatedObject = null;
        }

        void OnBindingContextChanged(object sender, EventArgs e)
        {
            OnBindingContextChanged();
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BindingContext = AssociatedObject.BindingContext;
        }
    }
}
