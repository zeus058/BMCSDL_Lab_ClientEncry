using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StudentManager.Helpers
{
    public static class PasswordBoxAssistant
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxAssistant), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));

        public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);
        public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);
        public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox box)
            {
                if ((bool)box.GetValue(UpdatingPasswordProperty)) return;
                box.Password = (string)e.NewValue;
            }
        }

        private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            if (dp is PasswordBox box)
            {
                if ((bool)e.OldValue) box.PasswordChanged -= HandlePasswordChanged;
                if ((bool)e.NewValue) box.PasswordChanged += HandlePasswordChanged;
            }
        }

        private static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox box)
            {
                box.SetValue(UpdatingPasswordProperty, true);
                SetBoundPassword(box, box.Password);
                box.SetValue(UpdatingPasswordProperty, false);
            }
        }
    }
}
