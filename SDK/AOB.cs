using System;
using System.Collections.Generic;

namespace CESDK
{
    class AOB : CEObjectWrapper
    {
        /// <summary>
        /// Scan for an Array of Bytes (AOB) pattern in the current process
        /// </summary>
        /// <param name="aobString">The AOB pattern string (e.g., "48 8B 05 ? ? ? ?")</param>
        /// <param name="protectionFlags">Optional protection flags (+W = writable, +X = executable, -C = not copy-on-write)</param>
        /// <param name="alignmentType">Optional alignment type (0=none, 1=divisible by param, 2=address ends with param)</param>
        /// <param name="alignmentParam">Optional alignment parameter</param>
        /// <returns>List of addresses where the pattern was found</returns>
        public List<string> Scan(string aobString, string protectionFlags = null, int? alignmentType = null, string alignmentParam = null)
        {
            if (string.IsNullOrWhiteSpace(aobString))
                throw new ArgumentException("AOB string cannot be null or empty", nameof(aobString));

            try
            {
                // Get AOBScan function from Lua
                lua.GetGlobal("AOBScan");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("AOBScan function not found in Lua environment");
                }

                // Push parameters to Lua stack
                lua.PushString(aobString);
                int paramCount = 1;

                if (!string.IsNullOrWhiteSpace(protectionFlags))
                {
                    lua.PushString(protectionFlags);
                    paramCount++;
                }

                if (alignmentType.HasValue)
                {
                    lua.PushInteger(alignmentType.Value);
                    paramCount++;

                    if (!string.IsNullOrWhiteSpace(alignmentParam))
                    {
                        lua.PushString(alignmentParam);
                        paramCount++;
                    }
                }

                // Call AOBScan function
                int result = lua.PCall(paramCount, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"AOBScan call failed: {error}");
                }

                // Process the result (should be a StringList userdata)
                var addresses = new List<string>();

                if (!lua.IsNil(-1))
                {
                    // Get the Count property of the StringList
                    lua.PushString("Count");
                    lua.GetTable(-2);

                    if (lua.IsNumber(-1))
                    {
                        int count = (int)lua.ToInteger(-1);
                        lua.Pop(1); // Remove count

                        // Iterate through StringList items
                        for (int i = 0; i < count; i++)
                        {
                            lua.PushInteger(i);
                            lua.GetTable(-2);

                            if (lua.IsString(-1))
                            {
                                string address = lua.ToString(-1);
                                addresses.Add(address);
                            }
                            lua.Pop(1);
                        }

                        // Destroy the StringList to free memory
                        lua.PushString("destroy");
                        lua.GetTable(-2);
                        if (lua.IsFunction(-1))
                        {
                            lua.PushValue(-2); // Push StringList as self
                            lua.PCall(1, 0); // Call destroy method
                        }
                        else
                        {
                            lua.Pop(1); // Pop the non-function
                        }
                    }
                    else
                    {
                        lua.Pop(1); // Pop the non-number
                    }
                }

                lua.SetTop(0); // Clean up stack
                return addresses;
            }
            catch (Exception ex)
            {
                lua.SetTop(0); // Clean up stack on error
                throw new InvalidOperationException($"AOB scan error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Scan for the first occurrence of an AOB pattern
        /// </summary>
        /// <param name="aobString">The AOB pattern string</param>
        /// <param name="protectionFlags">Optional protection flags</param>
        /// <param name="alignmentType">Optional alignment type</param>
        /// <param name="alignmentParam">Optional alignment parameter</param>
        /// <returns>First address found, or null if not found</returns>
        public string ScanFirst(string aobString, string protectionFlags = null, int? alignmentType = null, string alignmentParam = null)
        {
            var results = Scan(aobString, protectionFlags, alignmentType, alignmentParam);
            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// Check if an AOB pattern exists in the current process
        /// </summary>
        /// <param name="aobString">The AOB pattern string</param>
        /// <param name="protectionFlags">Optional protection flags</param>
        /// <param name="alignmentType">Optional alignment type</param>
        /// <param name="alignmentParam">Optional alignment parameter</param>
        /// <returns>True if the pattern was found, false otherwise</returns>
        public bool Exists(string aobString, string protectionFlags = null, int? alignmentType = null, string alignmentParam = null)
        {
            var results = Scan(aobString, protectionFlags, alignmentType, alignmentParam);
            return results.Count > 0;
        }
    }
}