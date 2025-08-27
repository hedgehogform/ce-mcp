using System;
using CESDK;
using CeMCP.Models;

namespace CeMCP.Tools
{
    public class AddressTool
    {
        public GetAddressSafeResponse GetAddressSafe(GetAddressSafeRequest request)
        {
            try
            {
                if (request?.AddressString == null)
                {
                    return new GetAddressSafeResponse
                    {
                        Success = false,
                        Error = "AddressString parameter is required"
                    };
                }

                var address = new Address();
                string result = address.GetAddressSafe(request.AddressString, request.Local ?? false);

                return new GetAddressSafeResponse
                {
                    Address = result,
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new GetAddressSafeResponse
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }

        public GetNameFromAddressResponse GetNameFromAddress(GetNameFromAddressRequest request)
        {
            try
            {
                if (request?.Address == null)
                {
                    return new GetNameFromAddressResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                var address = new Address();
                string name = address.GetNameFromAddress(
                    request.Address, 
                    request.ModuleNames ?? true, 
                    request.Symbols ?? true, 
                    request.Sections ?? false
                );

                return new GetNameFromAddressResponse
                {
                    Name = name,
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new GetNameFromAddressResponse
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }

        public InModuleResponse InModule(InModuleRequest request)
        {
            try
            {
                if (request?.Address == null)
                {
                    return new InModuleResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                var address = new Address();
                bool inModule = address.InModule(request.Address);

                return new InModuleResponse
                {
                    InModule = inModule,
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new InModuleResponse
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }

        public InSystemModuleResponse InSystemModule(InSystemModuleRequest request)
        {
            try
            {
                if (request?.Address == null)
                {
                    return new InSystemModuleResponse
                    {
                        Success = false,
                        Error = "Address parameter is required"
                    };
                }

                var address = new Address();
                bool inSystemModule = address.InSystemModule(request.Address);

                return new InSystemModuleResponse
                {
                    InSystemModule = inSystemModule,
                    Success = true
                };
            }
            catch (Exception e)
            {
                return new InSystemModuleResponse
                {
                    Success = false,
                    Error = e.Message
                };
            }
        }
    }
}