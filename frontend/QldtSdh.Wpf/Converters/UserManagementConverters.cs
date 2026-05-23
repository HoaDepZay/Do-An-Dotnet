using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace QldtSdh.Wpf.Converters
{
    public class StatusBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool && (bool)value;
            // #052e16 (dark green) vs #450a0a (dark red)
            return isActive 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#052e16")) 
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#450a0a"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool && (bool)value;
            // #22c55e (green) vs #f87171 (red)
            return isActive 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e")) 
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool && (bool)value;
            return isActive ? "Đang hoạt động" : "Bị khóa";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ActionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isActive = value is bool && (bool)value;
            return isActive ? "Khóa" : "Kích hoạt";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? text = value as string;
            return !string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class FeedbackColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSuccess = value is bool && (bool)value;
            return isSuccess 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ade80")) // light green
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f87171")); // light red
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
