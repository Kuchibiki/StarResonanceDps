using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using StarResonanceDpsAnalysis.WPF.Models;
using StarResonanceDpsAnalysis.WPF.ViewModels;

namespace StarResonanceDpsAnalysis.WPF.Views;

public partial class SkillLogView : Window
{
    private readonly SkillLogViewModel _viewModel;

    public SkillLogView(SkillLogViewModel vm)
    {
        InitializeComponent();
        _viewModel = vm;
        DataContext = vm;
        
        // Monitor collection changes for auto-scroll
        _viewModel.Logs.CollectionChanged += Logs_CollectionChanged;
    }

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Auto-scroll to bottom when new items are added
        if (e.Action == NotifyCollectionChangedAction.Add || 
            e.Action == NotifyCollectionChangedAction.Reset)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (LogListBox.Items.Count > 0)
                {
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    /// <summary>
    /// Handle skill log item click to show detail popup
    /// </summary>
    private void SkillLogItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Grid grid)
            return;

        // Find the Popup in the visual tree
        var popup = FindVisualChild<Popup>(grid);
        if (popup != null)
        {
            // Toggle popup display
            popup.IsOpen = !popup.IsOpen;
            
            // If opening, set focus to the popup's child to enable proper closing behavior
            if (popup.IsOpen && popup.Child is UIElement child)
            {
                child.Focus();
            }
            
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handle popup losing focus - close it
    /// </summary>
    private void Popup_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    /// <summary>
    /// Handle mouse leaving popup border - close it after a short delay
    /// </summary>
    private void PopupBorder_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border border)
        {
            // Find the parent Popup
            var popup = FindParent<Popup>(border);
            if (popup != null)
            {
                // Use a short delay before closing to avoid accidental closes
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (popup != null && !popup.IsMouseOver)
                    {
                        popup.IsOpen = false;
                    }
                }), System.Windows.Threading.DispatcherPriority.Background, 
                System.Threading.CancellationToken.None, 
                TimeSpan.FromMilliseconds(100));
            }
        }
    }

    /// <summary>
    /// Find child element in visual tree
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    /// <summary>
    /// Find parent element in visual tree
    /// </summary>
    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        
        if (parent == null)
            return null;
            
        if (parent is T typedParent)
            return typedParent;
            
        return FindParent<T>(parent);
    }

    private void Header_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            this.DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
