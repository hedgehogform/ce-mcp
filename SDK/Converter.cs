using System;
using System.Collections.Generic;

namespace CESDK
{
    class StringConverter : CEObjectWrapper
    {
        private static string GetLuaFunction(string conversionType)
        {
            return conversionType.ToLower() switch
            {
                "ansitoutf8" => "ansiToUtf8",
                "utf8toansi" => "utf8ToAnsi",
                "stringtomd5string" or "stringtomd5" or "md5" => "stringToMD5String",
                _ => null
            };
        }

        public string CallLuaStringFunction(string functionName, string input)
        {
            lua.GetGlobal(functionName);   // push the Lua function onto the stack
            lua.PushString(input);         // push the argument
            lua.PCall(1, 1);               // call function with 1 argument, 1 result
            string result = lua.ToString(-1);
            lua.Pop(1);                    // remove the result from the stack
            return result;
        }

        public string ConvertString(string input, string conversionType)
        {
            string luaFunction = GetLuaFunction(conversionType);

            if (string.IsNullOrEmpty(luaFunction))
            {
                throw new ArgumentException($"Unsupported conversion type: {conversionType}");
            }

            return conversionType.ToLower() switch
            {
                "ansitoutf8" => CallLuaStringFunction("ansiToUtf8", input),
                "utf8toansi" => CallLuaStringFunction("utf8ToAnsi", input),
                "md5" => CallLuaStringFunction("stringToMD5String", input),
                _ => throw new ArgumentException($"Unsupported conversion type: {conversionType}"),
            };
        }
    }
}