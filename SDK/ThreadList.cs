using System;
using System.Collections.Generic;

namespace CESDK
{
    class ThreadList : CEObjectWrapper
    {
        /// <summary>
        /// Get list of threads for the currently opened process
        /// </summary>
        /// <returns>List of thread information</returns>
        public List<string> GetThreads()
        {
            var threadList = new List<string>();

            try
            {
                // Create a StringList
                lua.GetGlobal("createStringlist");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("createStringlist function not found");
                }

                int result = lua.PCall(0, 1);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"createStringlist call failed: {error}");
                }

                // The StringList should now be on the stack
                if (!lua.IsCEObject(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("createStringlist did not return a valid object");
                }

                // Get the StringList object
                var stringListObj = lua.ToCEObject(-1);
                lua.Pop(1);

                // Call getThreadlist with the StringList
                lua.GetGlobal("getThreadlist");
                if (!lua.IsFunction(-1))
                {
                    lua.SetTop(0);
                    throw new InvalidOperationException("getThreadlist function not found");
                }

                lua.PushCEObject(stringListObj);
                result = lua.PCall(1, 0);
                if (result != 0)
                {
                    string error = lua.ToString(-1);
                    lua.SetTop(0);
                    throw new InvalidOperationException($"getThreadlist call failed: {error}");
                }

                // Now read from the StringList
                lua.PushCEObject(stringListObj);
                lua.PushString("Count");
                lua.GetTable(-2);

                if (lua.IsNumber(-1))
                {
                    int count = (int)lua.ToInteger(-1);
                    lua.Pop(1); // Remove count

                    // Read each item from the StringList
                    for (int i = 0; i < count; i++)
                    {
                        lua.PushCEObject(stringListObj);
                        lua.PushInteger(i);
                        lua.GetTable(-2);

                        if (lua.IsString(-1))
                        {
                            string threadInfo = lua.ToString(-1);
                            if (!string.IsNullOrEmpty(threadInfo))
                            {
                                threadList.Add(threadInfo);
                            }
                        }
                        lua.Pop(2); // Remove string and StringList
                    }
                }
                else
                {
                    lua.Pop(1); // Remove non-number
                }

                lua.Pop(1); // Remove StringList

                // Destroy the StringList
                lua.PushCEObject(stringListObj);
                lua.PushString("destroy");
                lua.GetTable(-2);
                if (lua.IsFunction(-1))
                {
                    lua.PushCEObject(stringListObj);
                    lua.PCall(1, 0);
                }
                else
                {
                    lua.Pop(1);
                }
                lua.Pop(1); // Remove StringList

                lua.SetTop(0);
                return threadList;
            }
            catch (Exception ex)
            {
                lua.SetTop(0);
                throw new InvalidOperationException($"ThreadList error: {ex.Message}", ex);
            }
        }
    }
}