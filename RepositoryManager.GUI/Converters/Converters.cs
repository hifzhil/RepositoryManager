using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RepositoryManager.GUI.Converters
{

    /// <summary>Returns Visibility.Visible when the bound value is not null/empty.</summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();

        public object Convert(object? value, Type t, object? p, CultureInfo c) =>
            value is null || (value is string s && string.IsNullOrEmpty(s))
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>Returns Visibility.Visible when the bound value IS null/empty.</summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public sealed class NullToVisibilityInverseConverter : IValueConverter
    {
        public static readonly NullToVisibilityInverseConverter Instance = new();

        public object Convert(object? value, Type t, object? p, CultureInfo c) =>
            value is null || (value is string s && string.IsNullOrEmpty(s))
                ? Visibility.Visible
                : Visibility.Collapsed;

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>Returns Visibility.Visible when bool is true.</summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public static readonly BoolToVisibilityConverter Instance = new();

        public object Convert(object? value, Type t, object? p, CultureInfo c) =>
            value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>Checks whether a string contains a given substring (ConverterParameter).</summary>
    [ValueConversion(typeof(string), typeof(bool))]
    public sealed class StringContainsConverter : IValueConverter
    {
        public static readonly StringContainsConverter Instance = new();

        public object Convert(object? value, Type t, object? p, CultureInfo c) =>
            value is string s && p is string sub && s.Contains(sub, StringComparison.OrdinalIgnoreCase);

        public object ConvertBack(object? v, Type t, object? p, CultureInfo c) =>
            throw new NotSupportedException();
    }

    /// <summary>Inverts a bool value.</summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class InverseBoolConverter : IValueConverter
    {
        public static readonly InverseBoolConverter Instance = new();

        public object Convert(object? value, Type t, object? p, CultureInfo c) =>
            value is bool b && !b;

        public object ConvertBack(object? value, Type t, object? p, CultureInfo c) =>
            value is bool b && !b;
    }

    [ValueConversion(typeof(int), typeof(Visibility))]
    public sealed class IntZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            return value is int i && i == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
        {
            throw new NotSupportedException();
        }
    }
}
