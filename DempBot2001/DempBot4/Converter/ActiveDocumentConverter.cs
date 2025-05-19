namespace Dempbot4.Converter
{
    using Dempbot4.ViewModel;
    using Dempbot4.ViewModel.Base;
    using System;
    using System.Windows.Data;

    class ActiveDocumentConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is DocumentTypePaneVM)
        return value;

      return Binding.DoNothing;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (value is DocumentTypePaneVM)
        return value;

      return Binding.DoNothing;
    }
  }
}
