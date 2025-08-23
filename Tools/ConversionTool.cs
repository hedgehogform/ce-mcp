using System;
using CESDK;
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

                string luaFunction = GetLuaFunction(request.ConversionType);

                if (string.IsNullOrEmpty(luaFunction))
                {
                    return new ConversionResponse
                    {
                        Success = false,
                        Error = $"Unsupported conversion type: {request.ConversionType}"
                    };
                }

                var converter = new StringConverter();
                string output = converter.ConvertString(request.Input, request.ConversionType);

                return new ConversionResponse
                {
                    Success = true,
                    Output = output
                };
            }
            catch (Exception ex)
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
            return conversionType.ToLower() switch
            {
                "ansitoutf8" => "ansiToUtf8",
                "utf8toansi" => "utf8ToAnsi",
                "md5" => "stringToMD5String",
                _ => null
            };
        }
    }
}