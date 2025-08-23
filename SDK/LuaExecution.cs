using System;

namespace CESDK
{
    class LuaExecution : CEObjectWrapper
    {
        /// <summary>
        /// Execute Lua code and return result
        /// </summary>
        /// <param name="code">Lua code to execute</param>
        /// <returns>Result string</returns>
        public string ExecuteCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            try
            {
                // Load the string as a Lua chunk
                int loadResult = lua.LoadString(code);
                if (loadResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua load failed: {error}");
                }

                // Execute the loaded chunk
                int result = lua.PCall(0, -1); // -1 means return all results
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua execution failed: {error}");
                }

                // Check if there are any return values on the stack
                int stackSize = lua.GetTop();
                string returnValue;

                if (stackSize > 0)
                {
                    // Get the top value from the stack as a string
                    returnValue = lua.ToString(-1);
                    lua.SetTop(0);
                }
                else
                {
                    // No return value, indicate success
                    returnValue = "0";
                }

                return returnValue;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"Lua execution error: {ex.Message}", ex);
            }
        }
    }
}