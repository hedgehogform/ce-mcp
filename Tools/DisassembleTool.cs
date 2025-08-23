using System;
using CESDK;
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

                var disassembler = new Disassembler();
                string result;

                switch (request.RequestType?.ToLower())
                {
                    case "get-instruction-size":
                        int size = disassembler.GetInstructionSize(request.Address);
                        result = size.ToString();
                        break;

                    case "disassemble":
                    case null:
                    case "":
                        result = disassembler.Disassemble(request.Address);
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