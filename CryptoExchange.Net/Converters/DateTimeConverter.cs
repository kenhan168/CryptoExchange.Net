﻿using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Globalization;

namespace CryptoExchange.Net.Converters
{
    /// <summary>
    /// Datetime converter
    /// </summary>
    public class DateTimeConverter: JsonConverter
    {
        private static DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const decimal ticksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;
        private const decimal ticksPerNanosecond = TimeSpan.TicksPerMillisecond / 1000m / 1000;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        /// <inheritdoc />
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return null;

            if(reader.TokenType is JsonToken.Integer)
            {
                var longValue = (long)reader.Value;
                if (longValue == 0)
                    return null;
                if (longValue < 1999999999)
                    return ConvertFromSeconds(longValue);
                if (longValue < 1999999999999)
                    return ConvertFromMilliseconds(longValue);
                if (longValue < 1999999999999999)
                    return ConvertFromMicroseconds(longValue);
                
                return ConvertFromNanoseconds(longValue);
            }
            else if (reader.TokenType is JsonToken.Float)
            {
                var doubleValue = (double)reader.Value;
                if (doubleValue < 1999999999)
                    return ConvertFromSeconds(doubleValue);
                
                return ConvertFromMilliseconds(doubleValue);
            }
            else if(reader.TokenType is JsonToken.String)
            {
                var stringValue = (string)reader.Value;
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;

                if (stringValue.Length == 8)
                {
                    // Parse 20211103 format
                    if (!int.TryParse(stringValue.Substring(0, 4), out var year)
                        || !int.TryParse(stringValue.Substring(4, 2), out var month)
                        || !int.TryParse(stringValue.Substring(6, 2), out var day))
                    {
                        Debug.WriteLine("Unknown DateTime format: " + reader.Value);
                        return default;
                    }
                    return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                }

                if (stringValue.Length == 6)
                {
                    // Parse 211103 format
                    if (!int.TryParse(stringValue.Substring(0, 2), out var year)
                        || !int.TryParse(stringValue.Substring(2, 2), out var month)
                        || !int.TryParse(stringValue.Substring(4, 2), out var day))
                    {
                        Debug.WriteLine("Unknown DateTime format: " + reader.Value);
                        return default;
                    }
                    return new DateTime(year + 2000, month, day, 0, 0, 0, DateTimeKind.Utc);
                }

                if (double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    // Parse 1637745563.000 format
                    if (doubleValue < 1999999999)
                        return ConvertFromSeconds(doubleValue);
                    return ConvertFromMilliseconds(doubleValue);
                }

                if(stringValue.Length == 10)
                {
                    // Parse 2021-11-03 format
                    var values = stringValue.Split('-');
                    if(!int.TryParse(values[0], out var year)
                        || !int.TryParse(values[1], out var month)
                        || !int.TryParse(values[2], out var day))
                    {
                        Debug.WriteLine("Unknown DateTime format: " + reader.Value);
                        return default;
                    }

                    return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
                }

                return JsonConvert.DeserializeObject(stringValue);
            }
            else if(reader.TokenType == JsonToken.Date)
            {
                return (DateTime)reader.Value;
            }
            else
            {
                Debug.WriteLine("Unknown DateTime format: " + reader.Value);
                return default;
            }
        }

        /// <summary>
        /// Convert a seconds since epoch (01-01-1970) value to DateTime
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static DateTime ConvertFromSeconds(double seconds) => _epoch.AddSeconds(seconds);
        /// <summary>
        /// Convert a milliseconds since epoch (01-01-1970) value to DateTime
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public static DateTime ConvertFromMilliseconds(double milliseconds) => _epoch.AddTicks((long)Math.Round(milliseconds * TimeSpan.TicksPerMillisecond));
        /// <summary>
        /// Convert a microseconds since epoch (01-01-1970) value to DateTime
        /// </summary>
        /// <param name="microseconds"></param>
        /// <returns></returns>
        public static DateTime ConvertFromMicroseconds(long microseconds) => _epoch.AddTicks((long)Math.Round(microseconds * ticksPerMicrosecond));
        /// <summary>
        /// Convert a nanoseconds since epoch (01-01-1970) value to DateTime
        /// </summary>
        /// <param name="nanoseconds"></param>
        /// <returns></returns>
        public static DateTime ConvertFromNanoseconds(long nanoseconds) => _epoch.AddTicks((long)Math.Round(nanoseconds * ticksPerNanosecond));
        /// <summary>
        /// Convert a DateTime value to seconds since epoch (01-01-1970) value
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long? ConvertToSeconds(DateTime? time) => time == null ? null: (long)Math.Round((time.Value - _epoch).TotalSeconds);
        /// <summary>
        /// Convert a DateTime value to milliseconds since epoch (01-01-1970) value
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long? ConvertToMilliseconds(DateTime? time) => time == null ? null : (long)Math.Round((time.Value - _epoch).TotalMilliseconds);
        /// <summary>
        /// Convert a DateTime value to microseconds since epoch (01-01-1970) value
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long? ConvertToMicroseconds(DateTime? time) => time == null ? null : (long)Math.Round((time.Value - _epoch).Ticks / ticksPerMicrosecond);
        /// <summary>
        /// Convert a DateTime value to nanoseconds since epoch (01-01-1970) value
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long? ConvertToNanoseconds(DateTime? time) => time == null ? null : (long)Math.Round((time.Value - _epoch).Ticks / ticksPerNanosecond);


        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var datetimeValue = (DateTime?)value;
            if (datetimeValue == null)
                writer.WriteValue((DateTime?)null);
            if(datetimeValue == default(DateTime))
                writer.WriteValue((DateTime?)null);
            else
                writer.WriteValue((long)Math.Round(((DateTime)value - new DateTime(1970, 1, 1)).TotalMilliseconds));
        }
    }
}
