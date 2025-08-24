using System;

namespace CESDK
{
    class MemoryWrite : CEObjectWrapper
    {
        /// <summary>
        /// Write bytes to memory
        /// </summary>
        public void WriteBytes(string address, int[] bytes)
        {
            try
            {
                lua.GetGlobal("writeBytes");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("writeBytes function not found");
                }

                lua.PushString(address);

                // Create a table with the bytes
                lua.PushNil(); // Create new table
                lua.CreateTable(0, 0);
                for (int i = 0; i < bytes.Length; i++)
                {
                    lua.PushInteger(i + 1); // Lua arrays are 1-indexed
                    lua.PushInteger(bytes[i]);
                    lua.SetTable(-3);
                }

                int result = lua.PCall(2, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"writeBytes call failed: {error}");
                }

                lua.SetTop(0);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"WriteBytes error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Write a 32-bit integer
        /// </summary>
        public void WriteInteger(string address, int value)
        {
            try
            {
                lua.GetGlobal("writeInteger");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("writeInteger function not found");
                }

                lua.PushString(address);
                lua.PushInteger(value);

                int result = lua.PCall(2, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"writeInteger call failed: {error}");
                }

                lua.SetTop(0);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"WriteInteger error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Write a 64-bit integer
        /// </summary>
        public void WriteQword(string address, long value)
        {
            try
            {
                lua.GetGlobal("writeQword");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("writeQword function not found");
                }

                lua.PushString(address);
                lua.PushInteger(value);

                int result = lua.PCall(2, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"writeQword call failed: {error}");
                }

                lua.SetTop(0);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"WriteQword error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Write a float
        /// </summary>
        public void WriteFloat(string address, float value)
        {
            try
            {
                lua.GetGlobal("writeFloat");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("writeFloat function not found");
                }

                lua.PushString(address);
                lua.PushNumber(value);

                int result = lua.PCall(2, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"writeFloat call failed: {error}");
                }

                lua.SetTop(0);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"WriteFloat error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Write a string
        /// </summary>
        public void WriteString(string address, string text, bool wideChar = false)
        {
            try
            {
                lua.GetGlobal("writeString");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("writeString function not found");
                }

                lua.PushString(address);
                lua.PushString(text);
                if (wideChar)
                {
                    lua.PushBoolean(true);
                }

                int paramCount = wideChar ? 3 : 2;
                int result = lua.PCall(paramCount, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"writeString call failed: {error}");
                }

                lua.SetTop(0);
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"WriteString error: {ex.Message}", ex);
            }
        }
    }
}