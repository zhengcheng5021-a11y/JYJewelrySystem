using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZR.Model.JingYue
{
    /// <summary>
    /// 雪花 Id 统一序列化为字符串。
    /// JS 的 Number 只能安全表达 2^53 以内的整数，2113865529822420992 这样的文件 Id
    /// 直接以数字输出会在前端被四舍五入，回传后后端查不到 sys_file，误报“货品图片不存在”。
    /// 读取时兼容字符串与数字两种写法。
    /// </summary>
    public class IdToStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(long) || objectType == typeof(long?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType is JsonToken.Null or JsonToken.Undefined) return null;
            var token = JToken.Load(reader);
            if (token.Type is JTokenType.Null or JTokenType.Undefined) return null;
            long value;
            if (token.Type == JTokenType.String)
            {
                var text = token.Value<string>();
                if (string.IsNullOrWhiteSpace(text)) return null;
                if (!long.TryParse(text, out value)) throw new JsonSerializationException("Id 格式不正确");
            }
            else
            {
                value = token.Value<long>();
            }
            if (value <= 0) return null;
            return objectType == typeof(long?) ? (long?)value : value;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(Convert.ToInt64(value).ToString());
        }
    }
}
