using System;
using System.Collections.Generic;

namespace CESDK
{
    class StringConverter : CEObjectWrapper
    {
        private static string GetLuaFunction(string conversionType)
        {
            string lowerType = conversionType.ToLower();
            if (lowerType == "ansitoutf8")
                return "ansiToUtf8";
            else if (lowerType == "utf8toansi")
                return "utf8ToAnsi";
            else if (lowerType == "stringtomd5string" || lowerType == "stringtomd5" || lowerType == "md5")
                return "stringToMD5String";
            else
                return null;
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

            string lowerType = conversionType.ToLower();
            if (lowerType == "ansitoutf8")
                return CallLuaStringFunction("ansiToUtf8", input);
            else if (lowerType == "utf8toansi")
                return CallLuaStringFunction("utf8ToAnsi", input);
            else if (lowerType == "md5")
                return CallLuaStringFunction("stringToMD5String", input);
            else
                throw new ArgumentException($"Unsupported conversion type: {conversionType}");
        }
    }
}