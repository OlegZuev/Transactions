﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Transactions.Converters {
    public class EventArgsConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return new[] {value, parameter};
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}