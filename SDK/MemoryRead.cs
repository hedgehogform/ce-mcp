using System;
using System.Collections.Generic;

namespace CESDK
{
    class MemoryRead : CEObjectWrapper
    {
        /// <summary>
        /// Read bytes from memory
        /// </summary>
        public int[] ReadBytes(string address, int byteCount, bool returnAsTable = true)
        {
            try
            {
                lua.GetGlobal("readBytes");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("readBytes function not found");
                }

                lua.PushString(address);
                lua.PushInteger(byteCount);
                lua.PushBoolean(returnAsTable);

                int result = lua.PCall(3, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"readBytes call failed: {error}");
                }

                var bytes = new List<int>();
                if (lua.IsTable(-1))
                {
                    int tableLength = lua.ObjLen(-1);
                    for (int i = 1; i <= tableLength; i++)
                    {
                        lua.PushInteger(i);
                        lua.GetTable(-2);
                        if (lua.IsNumber(-1))
                        {
                            bytes.Add((int)lua.ToNumber(-1));
                        }
                        lua.Pop(1);
                    }
                }

                lua.SetTop(0);
                return bytes.ToArray();
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ReadBytes error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read a 32-bit integer
        /// </summary>
        public int ReadInteger(string address, bool signed = false)
        {
            try
            {
                lua.GetGlobal("readInteger");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("readInteger function not found");
                }

                lua.PushString(address);
                lua.PushBoolean(signed);

                int result = lua.PCall(2, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"readInteger call failed: {error}");
                }

                int value = (int)lua.ToInteger(-1);
                lua.SetTop(0);
                return value;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ReadInteger error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read a 64-bit integer
        /// </summary>
        public long ReadQword(string address)
        {
            try
            {
                lua.GetGlobal("readQword");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("readQword function not found");
                }

                lua.PushString(address);

                int result = lua.PCall(1, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"readQword call failed: {error}");
                }

                long value = lua.ToInteger(-1);
                lua.SetTop(0);
                return value;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ReadQword error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read a float
        /// </summary>
        public float ReadFloat(string address)
        {
            try
            {
                lua.GetGlobal("readFloat");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("readFloat function not found");
                }

                lua.PushString(address);

                int result = lua.PCall(1, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"readFloat call failed: {error}");
                }

                float value = (float)lua.ToNumber(-1);
                lua.SetTop(0);
                return value;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ReadFloat error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Read a string
        /// </summary>
        public string ReadString(string address, int maxLength, bool wideChar = false)
        {
            try
            {
                lua.GetGlobal("readString");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("readString function not found");
                }

                lua.PushString(address);
                lua.PushInteger(maxLength);
                if (wideChar)
                {
                    lua.PushBoolean(true);
                }

                int paramCount = wideChar ? 3 : 2;
                int result = lua.PCall(paramCount, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"readString call failed: {error}");
                }

                string value = lua.ToString(-1);
                lua.SetTop(0);
                return value;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ReadString error: {ex.Message}", ex);
            }
        }

        // Add more read methods as needed...
    }
}