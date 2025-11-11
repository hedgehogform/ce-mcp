using System;
using CESDK;
using CESDK.Classes;

namespace Tools
{
    public static class ConversionTool
    {
        public static string Convert(string input, string conversionType)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input is required.", nameof(input));

            if (string.IsNullOrWhiteSpace(conversionType))
                throw new ArgumentException("Conversion type is required.", nameof(conversionType));

            try
            {
                return conversionType.ToLower() switch
                {
                    "md5" => Converter.StringToMD5(input),
                    "ansitoutf8" => Converter.AnsiToUtf8(input),
                    "utf8toansi" => Converter.Utf8ToAnsi(input),
                    _ => throw new NotSupportedException($"Unsupported conversion type: {conversionType}")
                };
            }
            catch (ConverterException ex)
            {
                throw new InvalidOperationException($"Conversion failed: {ex.Message}", ex);
            }
        }
    }
}
