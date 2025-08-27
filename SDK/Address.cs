using System;

namespace CESDK
{
    class Address : CEObjectWrapper
    {
        /// <summary>
        /// Gets the address for the given string safely, returns null if not found
        /// </summary>
        /// <param name="addressString">The address string to convert</param>
        /// <param name="local">Set to true to query CE's own symbol table</param>
        /// <returns>Address as string or null if not found</returns>
        public string GetAddressSafe(string addressString, bool local = false)
        {
            if (string.IsNullOrWhiteSpace(addressString))
                throw new ArgumentException("AddressString cannot be null or empty", nameof(addressString));

            try
            {
                // Use getAddressSafe function from Cheat Engine
                string luaCode = local
                    ? $"return getAddressSafe('{addressString}', true)"
                    : $"return getAddressSafe('{addressString}')";

                // Execute the Lua code
                int loadResult = lua.LoadString(luaCode);
                if (loadResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua load failed: {error}");
                }

                int result = lua.PCall(0, 1); // Expect 1 result
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua execution failed: {error}");
                }

                // Check if the result is nil
                if (lua.IsNil(-1))
                {
                    lua.SetTop(0);
                    return null;
                }

                // Get the address as string
                string address = lua.ToString(-1);
                lua.SetTop(0);

                return address;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"GetAddressSafe error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the name representation for the given address
        /// </summary>
        /// <param name="address">The address to convert to a string</param>
        /// <param name="moduleNames">If true, allows returning modulename+offset (default true)</param>
        /// <param name="symbols">If true, allows returning symbol names (default true)</param>
        /// <param name="sections">If true, allows returning section names (default false)</param>
        /// <returns>String representation of the address</returns>
        public string GetNameFromAddress(string address, bool moduleNames = true, bool symbols = true, bool sections = false)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));

            try
            {
                // Build the Lua function call with parameters
                string luaCode = $"return getNameFromAddress({address}, {moduleNames.ToString().ToLower()}, {symbols.ToString().ToLower()}, {sections.ToString().ToLower()})";

                // Execute the Lua code
                int loadResult = lua.LoadString(luaCode);
                if (loadResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua load failed: {error}");
                }

                int result = lua.PCall(0, 1); // Expect 1 result
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua execution failed: {error}");
                }

                // Get the name as string
                string name = lua.ToString(-1);
                lua.SetTop(0);
                
                return name;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"GetNameFromAddress error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Returns true if the provided address resides inside a module (e.g., .exe or .dll)
        /// </summary>
        /// <param name="address">The address to check</param>
        /// <returns>True if address is in a module, false otherwise</returns>
        public bool InModule(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));

            try
            {
                // Use inModule function from Cheat Engine
                string luaCode = $"return inModule({address})";

                // Execute the Lua code
                int loadResult = lua.LoadString(luaCode);
                if (loadResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua load failed: {error}");
                }

                int result = lua.PCall(0, 1); // Expect 1 result
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua execution failed: {error}");
                }

                // Get the boolean result
                bool inModule = lua.ToBoolean(-1);
                lua.SetTop(0);
                
                return inModule;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"InModule error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Returns true if the provided address resides inside a system module (stored in Windows folder)
        /// </summary>
        /// <param name="address">The address to check</param>
        /// <returns>True if address is in a system module, false otherwise</returns>
        public bool InSystemModule(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty", nameof(address));

            try
            {
                // Use inSystemModule function from Cheat Engine
                string luaCode = $"return inSystemModule({address})";

                // Execute the Lua code
                int loadResult = lua.LoadString(luaCode);
                if (loadResult != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua load failed: {error}");
                }

                int result = lua.PCall(0, 1); // Expect 1 result
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"Lua execution failed: {error}");
                }

                // Get the boolean result
                bool inSystemModule = lua.ToBoolean(-1);
                lua.SetTop(0);
                
                return inSystemModule;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"InSystemModule error: {ex.Message}", ex);
            }
        }
    }
}