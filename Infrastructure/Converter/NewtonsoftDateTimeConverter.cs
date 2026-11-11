using Newtonsoft.Json;
using System;
using System.Globalization;

namespace Infrastructure.Converter
{
    /// <summary>
    /// 兼容前端日期控件的日期时间转换器。
    /// 同时接受 yyyy-MM-dd、yyyy-MM-dd HH:mm:ss 与 ISO 8601 格式，
    /// 输出统一保持项目现有的 yyyy-MM-dd HH:mm:ss 格式。
    /// </summary>
    public sealed class NewtonsoftDateTimeConverter : JsonConverter
    {
        private const string OutputFormat = "yyyy-MM-dd HH:mm:ss";

        private static readonly string[] AcceptedFormats =
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy/MM/dd",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm"
        };

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return IsNullable(objectType) ? null : throw new JsonSerializationException($"日期字段不能为空。路径：{reader.Path}");
            }

            if (reader.TokenType == JsonToken.Date && reader.Value is DateTime dateToken)
            {
                return dateToken;
            }

            var text = Convert.ToString(reader.Value, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                if (IsNullable(objectType)) return null;
                throw new JsonSerializationException($"日期字段不能为空。路径：{reader.Path}");
            }

            if (DateTime.TryParseExact(text, AcceptedFormats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var exactDate))
            {
                return exactDate;
            }

            if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var parsedDate))
            {
                return parsedDate;
            }

            throw new JsonSerializationException(
                $"日期格式无效：'{text}'。请使用 yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss。路径：{reader.Path}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(((DateTime)value).ToString(OutputFormat, CultureInfo.InvariantCulture));
        }

        private static bool IsNullable(Type objectType)
        {
            return Nullable.GetUnderlyingType(objectType) != null || !objectType.IsValueType;
        }
    }
}