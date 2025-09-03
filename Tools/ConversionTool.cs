using System;
using CESDK;
using CESDK.Classes;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class ConversionTool
    {
        public ConversionResponse Convert(ConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Input))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = "Input parameter is required"
                    };
                }

                if (string.IsNullOrWhiteSpace(request.ConversionType))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = "ConversionType parameter is required"
                    };
                }

                string luaFunction = GetLuaFunction(request.ConversionType!);

                if (string.IsNullOrEmpty(luaFunction))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = $"Unsupported conversion type: {request.ConversionType}"
                    };
                }

                string output = request.ConversionType?.ToLower() switch
                {
                    "md5" => Converter.StringToMD5(request.Input!),
                    "ansitoutf8" => Converter.AnsiToUtf8(request.Input!),
                    "utf8toansi" => Converter.Utf8ToAnsi(request.Input!),
                    _ => throw new NotSupportedException($"Conversion type {request.ConversionType} not supported")
                };

                return new ConversionResponse
                {
                    Success = true,
                    Output = output
                };
            }
            catch (ConverterException ex)
            {
                return new ConversionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private static string GetLuaFunction(string conversionType)
        {
            string lowerType = conversionType.ToLower();
            if (lowerType == "ansitoutf8")
                return "ansiToUtf8";
            else if (lowerType == "utf8toansi")
                return "utf8ToAnsi";
            else if (lowerType == "md5")
                return "stringToMD5String";
            else
                return null!;
        }
    }
}