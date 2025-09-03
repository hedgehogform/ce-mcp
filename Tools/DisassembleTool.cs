using System;
using CESDK.Classes;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class DisassembleTool
    {
        public DisassemblerResponse Disassemble(DisassemblerRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Address))
                {
                    return new DisassemblerResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                if (!ulong.TryParse(request.Address.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber, null, out ulong address))
                {
                    return new DisassemblerResponse
                    {
                        Success = false,
                        Error = "Invalid address format"
                    };
                }

                string result;

                switch (request.RequestType?.ToLower())
                {
                    case "get-instruction-size":
                        var size = Disassembler.GetInstructionSize(address);
                        result = size.ToString();
                        break;

                    case "disassemble":
                    case null:
                    case "":
                        var instruction = Disassembler.Disassemble(address);
                        result = instruction.ToString();
                        break;

                    default:
                        return new DisassemblerResponse
                        {
                            Success = false,
                            Error = $"Unsupported RequestType: {request.RequestType}"
                        };
                }

                return new DisassemblerResponse
                {
                    Success = true,
                    Output = result
                };
            }
            catch (Exception ex)
            {
                return new DisassemblerResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}