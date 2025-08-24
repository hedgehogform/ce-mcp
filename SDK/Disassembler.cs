using System;
using CESDK;
using CeMCP.Models;
using Microsoft.Owin.BuilderProperties;

namespace CeMCP.Tools
{
    class Disassembler : CEObjectWrapper
    {
        public string Disassemble(dynamic address)
        {
            if (address == null)
                throw new ArgumentException("Address parameter is required");

            // get the Lua function
            lua.GetGlobal("disassemble");

            switch (address)
            {
                case string s:
                    lua.PushString(s);
                    break;

                case int i:
                    lua.PushInteger(i);
                    break;

                case long l:
                    lua.PushInteger((int)l); // CE Lua typically works with 32-bit ints
                    break;

                default:
                    throw new ArgumentException("Address parameter must be a string or integer");
            }

            lua.PCall(1, 1);
            var returnValue = lua.ToString(-1);
            lua.Pop(1); // remove the result from the stack
            return returnValue;
        }

        public int GetInstructionSize(dynamic address)
        {
            if (address == null)
                throw new ArgumentException("Address parameter is required");

            // get the Lua function
            lua.GetGlobal("getInstructionSize");

            switch (address)
            {
                case string s:
                    lua.PushString(s);
                    break;

                case int i:
                    lua.PushInteger(i);
                    break;

                case long l:
                    lua.PushInteger((int)l);
                    break;

                default:
                    throw new ArgumentException("Address parameter must be a string or integer");
            }

            lua.PCall(1, 1);
            var returnValue = (int)lua.ToInteger(-1);
            lua.Pop(1); // remove the result from the stack
            return returnValue;
        }
    }
}
