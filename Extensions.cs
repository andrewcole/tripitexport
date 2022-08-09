using System;
using System.Linq;
using System.Text.Json;

namespace TripItExport
{
    public static class Extensions
    {
        public static bool TryGetPropertyAsString(this JsonElement e, string propertyName, out string result)
        {
            result = string.Empty;

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            string? r = propertyElement.GetString();
            if (r is null) return false;
            result = r;

            return true;
        }

        public static bool TryGetPropertyAsULong(this JsonElement e, string propertyName, out ulong result)
        {
            result = ulong.MinValue;

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            if (!ulong.TryParse(propertyElement.GetString(), out result)) return false;

            return true;
        }

        public static bool TryGetPropertyAsDouble(this JsonElement e, string propertyName, out double result)
        {
            result = double.MinValue;

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            if (!double.TryParse(propertyElement.GetString(), out result)) return false;

            return true;
        }

        public static bool TryGetPropertyAsInt(this JsonElement e, string propertyName, out int result)
        {
            result = int.MinValue;

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            if (!int.TryParse(propertyElement.GetString(), out result)) return false;

            return true;
        }

        public static bool TryGetPropertyAsGuid(this JsonElement e, string propertyName, out Guid result)
        {
            result = Guid.Empty;

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            if (!Guid.TryParse(propertyElement.GetString(), out result)) return false;

            return true;
        }

        public static bool TryGetPropertyAsArray(this JsonElement e, string propertyName, out JsonElement[] result)
        {
            result = Array.Empty<JsonElement>();

            if (!e.TryGetProperty(propertyName, out JsonElement propertyElement)) return false;

            switch (propertyElement.ValueKind)
            {
                case JsonValueKind.Array:
                    result = propertyElement.EnumerateArray().ToArray();
                    return true;

                case JsonValueKind.Object:
                    result = new JsonElement[] { propertyElement };
                    return true;
            }

            return false;
        }
    }
}