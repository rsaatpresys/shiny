using System;
using System.Text.Json;
using Shiny.Infrastructure;


namespace Shiny.Web.Infrastructure
{
    public class SystemTextJsonSerializer : ISerializer
    {
        static readonly JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public T Deserialize<T>(string value) 
            => JsonSerializer.Deserialize<T>(value, options);

        public object Deserialize(Type objectType, string value) 
            => JsonSerializer.Deserialize(value, objectType, options);

        public string Serialize(object value)
            => JsonSerializer.Serialize(value, value.GetType(), options);
    }
}
