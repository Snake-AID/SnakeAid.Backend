using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Text.Json;
using SnakeAid.Core.Domains;
namespace SnakeAid.Core.Converters
{
    public static class SettingConverter
    {
        public static T? CastValue<T>(string? value, SettingValueType type)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default(T);

            try
            {
                return type switch
                {
                    // Xử lý các kiểu cơ bản (int, decimal, bool...)
                    SettingValueType.Int or
                    SettingValueType.Decimal or
                    SettingValueType.Boolean => ConvertPrimitive<T>(value),

                    // Xử lý JSON (Cho danh sách hoặc object phức tạp)
                    SettingValueType.Json => ConvertJson<T>(value),

                    // Mặc định là String
                    _ => (T)(object)value
                };
            }
            catch
            {
                return default(T);
            }
        }

        private static T? ConvertPrimitive<T>(string value)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter != null && converter.CanConvertFrom(typeof(string)))
            {
                var result = converter.ConvertFromInvariantString(value);
                return result != null ? (T)result : default(T);
            }
            return default(T);
        }

        private static T? ConvertJson<T>(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return default(T);

            try
            {
                return JsonSerializer.Deserialize<T>(value);
            }
            catch (JsonException)
            {
                return default(T);
            }
        }

        // Overload method với default value
        public static T CastValue<T>(string? value, SettingValueType type, T defaultValue)
        {
            var result = CastValue<T>(value, type);
            return result ?? defaultValue;
        }
    }
}