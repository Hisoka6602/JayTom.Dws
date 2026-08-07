using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace JayTom.Dws.Client.Extensions
{

    public class ListBoxMultiSelectionBehavior : Behavior<ListBox>
    {

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(ListBoxMultiSelectionBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += ListBox_SelectionChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= ListBox_SelectionChanged;
            base.OnDetaching();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var removedItem in e.RemovedItems)
            {
                SelectedItems.Remove(removedItem);
            }

            foreach (var addedItem in e.AddedItems)
            {
                SelectedItems.Add(addedItem);
            }
        }
    }
}