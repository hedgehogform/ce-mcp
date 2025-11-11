using System;
using CESDK;
using CESDK.Classes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Tools
{
    public static class ConversionTool
    {
        /// <summary>
        /// Maps conversion API endpoints
        /// </summary>
        public static void MapConversionApi(this WebApplication app)
        {
            // POST /api/convert - Convert string between formats
            app.MapPost("/api/convert", (ConversionRequest request) =>
            {
                try
                {
                    var result = Convert(request.Input ?? "", request.ConversionType ?? "");
                    return Results.Ok(new { success = true, result });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, error = ex.Message });
                }
            })
            .WithName("ConvertString")
            .WithDescription("Convert string between formats (md5, ansitoutf8, utf8toansi)")
            .WithOpenApi();
        }

        private static string Convert(string input, string conversionType)
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

    public record ConversionRequest(string? Input, string? ConversionType);
}
