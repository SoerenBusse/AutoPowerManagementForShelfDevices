using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace AutoPowerManagementForShelfDevices.Utils
{
    public static class RegistryUtils
    {
        public static T ParseRegistryKey<T>(RegistryKey baseKey, string requestedValue, T defaultValue)
        {
            try
            {
                object value = baseKey.GetValue(requestedValue);

                if (value == null)
                    return defaultValue;

                TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
                return (T) converter.ConvertFromString(value.ToString());
            }
            catch (NotSupportedException)
            {
                throw new ArgumentException(requestedValue,
                    $"The registry key {requestedValue} cannot be converted to {nameof(T)}");
            }
        }
    }
}