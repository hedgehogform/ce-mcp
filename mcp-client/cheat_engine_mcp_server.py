#!/usr/bin/env python3
"""
Cheat Engine MCP Server
Provides tools to interact with Cheat Engine via REST API
"""

from mcp.server.fastmcp import FastMCP
import httpx
import os
from typing import Dict, Any

mcp = FastMCP(name="Cheat Engine Server")

CHEAT_ENGINE_HOST = os.getenv("MCP_HOST", "localhost")
CHEAT_ENGINE_PORT = int(os.getenv("MCP_PORT", "6300"))
CHEAT_ENGINE_BASE_URL = f"http://{CHEAT_ENGINE_HOST}:{CHEAT_ENGINE_PORT}"
API_BASE_PATH = "/api/cheatengine"

async def make_request(endpoint: str, method: str = "GET", data: Dict[str, Any] = None) -> Dict[str, Any]:
    """
    Make a request to the Cheat Engine API with error handling
    
    Args:
        endpoint: API endpoint (without base path)
        method: HTTP method (GET or POST)
        data: Request data for POST requests
        
    Returns:
        Dictionary with response data or error information
    """
    url = f"{CHEAT_ENGINE_BASE_URL}{API_BASE_PATH}/{endpoint}"
    
    async with httpx.AsyncClient() as client:
        try:
            if method.upper() == "POST":
                response = await client.post(url, json=data, timeout=600.0)
            else:
                response = await client.get(url, timeout=600.0)
            
            response.raise_for_status()
            return response.json()
        except httpx.RequestError as e:
            return {"success": False, "error": f"Request failed: {e}"}
        except httpx.HTTPStatusError as e:
            return {"success": False, "error": f"HTTP error: {e.response.status_code}"}

@mcp.tool()
async def execute_lua(code: str) -> Dict[str, Any]:
    """
    Execute Lua code in Cheat Engine
    
    Args:
        code: The Lua code to execute
        
    Returns:
        Dictionary with result and success status
    """
    return await make_request("execute-lua", "POST", {"code": code})

@mcp.tool()
async def get_process_list() -> Dict[str, Any]:
    """
    Get list of running processes from Cheat Engine
    
    Returns:
        Dictionary with process list and success status
    """
    return await make_request("process-list")

@mcp.tool()
async def open_process(process: str) -> Dict[str, Any]:
    """
    Open a process in Cheat Engine by process ID or name
    
    Args:
        process: Process ID (as string) or process name to open
        
    Returns:
        Dictionary with success status and error message if any
    """
    return await make_request("open-process", "POST", {"process": process})

@mcp.tool()
async def get_thread_list() -> Dict[str, Any]:
    """
    Get list of threads for the currently opened process in Cheat Engine
    
    Returns:
        Dictionary with thread list and success status
    """
    return await make_request("thread-list")

@mcp.tool()
async def get_process_status() -> Dict[str, Any]:
    """
    Get status of the currently opened process in Cheat Engine
    
    Returns:
        Dictionary with process ID, open status, process name, and success status
    """
    return await make_request("process-status")

@mcp.tool()
async def read_bytes(address: str, byte_count: int, return_as_table: bool = True) -> Dict[str, Any]:
    """
    Read bytes from memory at the specified address in the currently opened process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        byte_count: Number of bytes to read
        return_as_table: If True, returns bytes as array. If False, returns multiple values
        
    Returns:
        Dictionary with bytes array/values and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "bytes", 
        "byteCount": byte_count,
        "returnAsTable": return_as_table
    })

@mcp.tool()
async def read_bytes_local(address: str, byte_count: int, return_as_table: bool = True) -> Dict[str, Any]:
    """
    Read bytes from memory at the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        byte_count: Number of bytes to read
        return_as_table: If True, returns bytes as array. If False, returns multiple values
        
    Returns:
        Dictionary with bytes array/values and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "byteslocal", 
        "byteCount": byte_count,
        "returnAsTable": return_as_table
    })

@mcp.tool()
async def read_small_integer(address: str, signed: bool = False) -> Dict[str, Any]:
    """
    Read a 16-bit integer (small integer) from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        signed: If True, interpret value as signed. If False, interpret as unsigned.
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "smallinteger", 
        "signed": signed
    })

@mcp.tool()
async def read_small_integer_local(address: str) -> Dict[str, Any]:
    """
    Read a 16-bit integer from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "smallintegerlocal"
    })

@mcp.tool()
async def read_integer(address: str, signed: bool = False) -> Dict[str, Any]:
    """
    Read a 32-bit integer from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        signed: If True, interpret value as signed. If False, interpret as unsigned.
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "integer", 
        "signed": signed
    })

@mcp.tool()
async def read_integer_local(address: str, signed: bool = False) -> Dict[str, Any]:
    """
    Read a 32-bit integer from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        signed: If True, interpret value as signed. If False, interpret as unsigned.
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "integerlocal", 
        "signed": signed
    })

@mcp.tool()
async def read_qword(address: str) -> Dict[str, Any]:
    """
    Read a 64-bit integer from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "qword"
    })

@mcp.tool()
async def read_qword_local(address: str) -> Dict[str, Any]:
    """
    Read a 64-bit integer from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with integer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "qwordlocal"
    })

@mcp.tool()
async def read_pointer(address: str) -> Dict[str, Any]:
    """
    Read a pointer-sized integer from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with pointer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "pointer"
    })

@mcp.tool()
async def read_pointer_local(address: str) -> Dict[str, Any]:
    """
    Read a pointer-sized integer from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with pointer value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "pointerlocal"
    })

@mcp.tool()
async def read_float(address: str) -> Dict[str, Any]:
    """
    Read a single precision (32-bit) floating point value from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with float value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "float"
    })

@mcp.tool()
async def read_float_local(address: str) -> Dict[str, Any]:
    """
    Read a single precision (32-bit) floating point value from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with float value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "floatlocal"
    })

@mcp.tool()
async def read_double(address: str) -> Dict[str, Any]:
    """
    Read a double precision (64-bit) floating point value from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with double value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "double"
    })

@mcp.tool()
async def read_double_local(address: str) -> Dict[str, Any]:
    """
    Read a double precision (64-bit) floating point value from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with double value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "doublelocal"
    })

@mcp.tool()
async def read_string(address: str, max_length: int, wide_char: bool = False) -> Dict[str, Any]:
    """
    Read a string from the specified address in the target process
    
    Args:
        address: Memory address to read from (as string, can be hex like "0x12345" or decimal)
        max_length: Maximum number of characters to read
        wide_char: If True, read as widechar (UTF-16/Unicode). If False, read as ASCII/ANSI.
        
    Returns:
        Dictionary with string value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "string", 
        "maxLength": max_length,
        "wideChar": wide_char
    })

@mcp.tool()
async def read_string_local(address: str, max_length: int, wide_char: bool = False) -> Dict[str, Any]:
    """
    Read a string from the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to read from in CE's memory (as string, can be hex like "0x12345" or decimal)
        max_length: Maximum number of characters to read
        wide_char: If True, read as widechar (UTF-16/Unicode). If False, read as ASCII/ANSI.
        
    Returns:
        Dictionary with string value and success status
    """
    return await make_request("read-memory", "POST", {
        "address": address, 
        "dataType": "stringlocal", 
        "maxLength": max_length,
        "wideChar": wide_char
    })

@mcp.tool()
async def write_bytes(address: str, byte_values: list) -> Dict[str, Any]:
    """
    Write bytes to memory at the specified address in the currently opened process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        byte_values: List of byte values to write (integers 0-255)
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "bytes", 
        "byteValues": byte_values
    })

@mcp.tool()
async def write_bytes_local(address: str, byte_values: list) -> Dict[str, Any]:
    """
    Write bytes to memory at the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        byte_values: List of byte values to write (integers 0-255)
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "byteslocal", 
        "byteValues": byte_values
    })

@mcp.tool()
async def write_small_integer(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 16-bit integer to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        value: The 16-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "smallinteger", 
        "value": value
    })

@mcp.tool()
async def write_small_integer_local(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 16-bit integer to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        value: The 16-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "smallintegerlocal", 
        "value": value
    })

@mcp.tool()
async def write_integer(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 32-bit integer to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        value: The 32-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "integer", 
        "value": value
    })

@mcp.tool()
async def write_integer_local(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 32-bit integer to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        value: The 32-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "integerlocal", 
        "value": value
    })

@mcp.tool()
async def write_qword(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 64-bit integer to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        value: The 64-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "qword", 
        "value": value
    })

@mcp.tool()
async def write_qword_local(address: str, value: int) -> Dict[str, Any]:
    """
    Write a 64-bit integer to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        value: The 64-bit integer value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "qwordlocal", 
        "value": value
    })

@mcp.tool()
async def write_float(address: str, value: float) -> Dict[str, Any]:
    """
    Write a single precision (32-bit) floating point value to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        value: The single precision floating point value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "float", 
        "value": value
    })

@mcp.tool()
async def write_float_local(address: str, value: float) -> Dict[str, Any]:
    """
    Write a single precision (32-bit) floating point value to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        value: The single precision floating point value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "floatlocal", 
        "value": value
    })

@mcp.tool()
async def write_double(address: str, value: float) -> Dict[str, Any]:
    """
    Write a double precision (64-bit) floating point value to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        value: The double precision floating point value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "double", 
        "value": value
    })

@mcp.tool()
async def write_double_local(address: str, value: float) -> Dict[str, Any]:
    """
    Write a double precision (64-bit) floating point value to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        value: The double precision floating point value to write
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "doublelocal", 
        "value": value
    })

@mcp.tool()
async def write_string(address: str, text: str, wide_char: bool = False) -> Dict[str, Any]:
    """
    Write a string to the specified address in the target process
    
    Args:
        address: Memory address to write to (as string, can be hex like "0x12345" or decimal)
        text: The string to write
        wide_char: If True, write as widechar (UTF-16/Unicode). If False, write as ASCII/ANSI.
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "string", 
        "value": text,
        "wideChar": wide_char
    })

@mcp.tool()
async def write_string_local(address: str, text: str, wide_char: bool = False) -> Dict[str, Any]:
    """
    Write a string to the specified address in Cheat Engine's own process memory
    
    Args:
        address: Memory address to write to in CE's memory (as string, can be hex like "0x12345" or decimal)
        text: The string to write
        wide_char: If True, write as widechar (UTF-16/Unicode). If False, write as ASCII/ANSI.
        
    Returns:
        Dictionary with success status
    """
    return await make_request("write-memory", "POST", {
        "address": address, 
        "dataType": "stringlocal", 
        "value": text,
        "wideChar": wide_char
    })

@mcp.tool()
async def ansi_to_utf8(text: str) -> Dict[str, Any]:
    """
    Convert a string from ANSI encoding to UTF-8
    
    Args:
        text: The string to convert from ANSI encoding
        
    Returns:
        Dictionary with converted UTF-8 string and success status
    """
    return await make_request("convert", "POST", {
        "input": text,
        "conversionType": "ansitoutf8"
    })

@mcp.tool()
async def utf8_to_ansi(text: str) -> Dict[str, Any]:
    """
    Convert a string from UTF-8 encoding to ANSI
    
    Args:
        text: The string to convert from UTF-8 encoding
        
    Returns:
        Dictionary with converted ANSI string and success status
    """
    return await make_request("convert", "POST", {
        "input": text,
        "conversionType": "utf8toansi"
    })

@mcp.tool()
async def string_to_md5(text: str) -> Dict[str, Any]:
    """
    Generate MD5 hash from the provided string
    
    Args:
        text: The string to hash
        
    Returns:
        Dictionary with MD5 hash string and success status
    """
    return await make_request("convert", "POST", {
        "input": text,
        "conversionType": "md5"
    })

@mcp.tool()
async def aob_scan(aob_string: str, protection_flags: str = None, alignment_type: int = None, alignment_param: str = None) -> Dict[str, Any]:
    """
    Scan the currently opened process for an Array of Bytes (AOB) pattern
    
    Args:
        aob_string: A string of bytes, as hex, to scan for (e.g., "48 8B 05 ? ? ? ?")
        protection_flags: Optional protection flags (+W = writable, +X = executable, -C = not copy-on-write)
        alignment_type: Optional alignment type (0=none, 1=divisible by param, 2=address ends with param)
        alignment_param: Optional alignment parameter (value for alignment check)
        
    Returns:
        Dictionary with array of found addresses and success status
    """
    data = {"aobString": aob_string}
    
    if protection_flags is not None:
        data["protectionFlags"] = protection_flags
    if alignment_type is not None:
        data["alignmentType"] = alignment_type
    if alignment_param is not None:
        data["alignmentParam"] = alignment_param
    
    return await make_request("aob-scan", "POST", data)

@mcp.tool()
async def disassemble(address: str) -> Dict[str, Any]:
    """
    Disassemble the instruction at the given address
    
    Args:
        address: The address to disassemble (as string, can be hex like "0x12345" or decimal)
        
    Returns:
        Dictionary with disassembled instruction string and success status
        Format: "address - bytes - opcode : extra"
    """
    return await make_request("disassemble", "POST", {
        "address": address
    })

@mcp.tool()
async def get_instruction_size(address: str) -> Dict[str, Any]:
    """
    Returns the size of an instruction (basically it disassembles the instruction and 
    returns the number of bytes for you).
    
    Args:
        address: CEAddressString or Integer (e.g. "game.exe+1234" or "8")
        
    Returns:
        Dictionary with success status, size, and error message if any
    """
    return await make_request("get-instruction-size", "POST", {
        "address": address
    })

@mcp.tool()
async def memscan_first(scan_option: str, var_type: str, input1: str = None, input2: str = None, 
                       rounding_type: str = "rtRounded", start_address: str = "0", 
                       stop_address: str = "0xffffffffffffffff", protection_flags: str = None,
                       alignment_type: str = None, alignment_param: str = None,
                       is_hexadecimal: bool = False, is_not_binary_string: bool = False,
                       is_unicode: bool = False, is_case_sensitive: bool = False) -> Dict[str, Any]:
    """
    Perform a first memory scan to search for values in the target process
    This function blocks and waits until the scan is complete before returning.
    
    Args:
        scan_option: Type of scan (soUnknownValue, soExactValue, soValueBetween, soBiggerThan, soSmallerThan)
        var_type: Variable type (vtByte, vtWord, vtDword, vtQword, vtSingle, vtDouble, vtString, vtByteArray, vtGrouped, vtBinary, vtAll)
        input1: Primary search value (required for most scan types except soUnknownValue)
        input2: Secondary search value (required for soValueBetween)
        rounding_type: Rounding type for floating point values (rtRounded, rtTruncated, rtExtremerounded)
        start_address: Start address for scan (default: "0")
        stop_address: Stop address for scan (default: "0xffffffffffffffff")
        protection_flags: Memory protection flags (e.g., "+W+X")
        alignment_type: Alignment type (fsmNotAligned, fsmAligned, fsmLastDigits)
        alignment_param: Alignment parameter
        is_hexadecimal: Whether input values are hexadecimal
        is_not_binary_string: Whether to handle binary as decimal instead of binary string
        is_unicode: Whether to use Unicode (UTF-16) for string scans
        is_case_sensitive: Whether string comparison is case sensitive
        
    Returns:
        Dictionary with scan ID and result count after completion
    """
    data = {
        "scanOption": scan_option,
        "varType": var_type,
        "roundingType": rounding_type,
        "startAddress": start_address,
        "stopAddress": stop_address,
        "isHexadecimalInput": is_hexadecimal,
        "isNotABinaryString": is_not_binary_string,
        "isUnicodeScan": is_unicode,
        "isCaseSensitive": is_case_sensitive
    }
    
    if input1 is not None:
        data["input1"] = input1
    if input2 is not None:
        data["input2"] = input2
    if protection_flags is not None:
        data["protectionFlags"] = protection_flags
    if alignment_type is not None:
        data["alignmentType"] = alignment_type
    if alignment_param is not None:
        data["alignmentParam"] = alignment_param
    
    return await make_request("memscan-first", "POST", data)

@mcp.tool()
async def memscan_next(scan_id: str, scan_option: str, input1: str = None, input2: str = None,
                      rounding_type: str = "rtRounded", is_hexadecimal: bool = False,
                      is_not_binary_string: bool = False, is_unicode: bool = False,
                      is_case_sensitive: bool = False, is_percentage: bool = False,
                      saved_result_name: str = None) -> Dict[str, Any]:
    """
    Perform a next memory scan based on previous scan results
    This function blocks and waits until the scan is complete before returning.
    
    Args:
        scan_id: ID of the previous scan to build upon
        scan_option: Type of next scan (soExactValue, soValueBetween, soBiggerThan, soSmallerThan, 
                    soIncreasedValue, soIncreasedValueBy, soDecreasedValue, soDecreasedValueBy, 
                    soChanged, soUnchanged)
        input1: Primary search value
        input2: Secondary search value (for between scans)
        rounding_type: Rounding type for floating point values (rtRounded, rtTruncated, rtExtremerounded)
        is_hexadecimal: Whether input values are hexadecimal
        is_not_binary_string: Whether to handle binary as decimal instead of binary string
        is_unicode: Whether to use Unicode (UTF-16) for string scans
        is_case_sensitive: Whether string comparison is case sensitive
        is_percentage: Whether to use percentage-based comparison
        saved_result_name: Name of saved result to compare against (optional)
        
    Returns:
        Dictionary with scan ID and result count after completion
    """
    data = {
        "scanId": scan_id,
        "scanOption": scan_option,
        "roundingType": rounding_type,
        "isHexadecimalInput": is_hexadecimal,
        "isNotABinaryString": is_not_binary_string,
        "isUnicodeScan": is_unicode,
        "isCaseSensitive": is_case_sensitive,
        "isPercentageScan": is_percentage
    }
    
    if input1 is not None:
        data["input1"] = input1
    if input2 is not None:
        data["input2"] = input2
    if saved_result_name is not None:
        data["savedResultName"] = saved_result_name
    
    return await make_request("memscan-next", "POST", data)


@mcp.tool()
async def memscan_results(scan_id: str, start_index: int = 0, count: int = 100) -> Dict[str, Any]:
    """
    Get results from a completed memory scan
    
    Args:
        scan_id: ID of the scan to get results from
        start_index: Starting index for results (for pagination)
        count: Maximum number of results to return
        
    Returns:
        Dictionary with scan results including:
        - results: Array of {address, value} objects
        - totalCount: Total number of results available
        - hasMore: Whether there are more results beyond this batch
    """
    return await make_request("memscan-results", "POST", {
        "scanId": scan_id,
        "startIndex": start_index,
        "count": count
    })

@mcp.tool()
async def memscan_new_scan(scan_id: str) -> Dict[str, Any]:
    """
    Clear the results of an existing memory scan to prepare for a new first scan
    
    Args:
        scan_id: ID of the scan to clear
        
    Returns:
        Dictionary with success status
    """
    return await make_request("memscan-new", "POST", {"scanId": scan_id})

@mcp.tool() 
async def get_api_info() -> Dict[str, Any]:
    """
    Get information about the Cheat Engine REST API
    
    Returns:
        API information and available endpoints
    """
    return {
        "base_url": CHEAT_ENGINE_BASE_URL,
        "swagger_ui": f"{CHEAT_ENGINE_BASE_URL}/swagger",
        "description": "REST API for Cheat Engine MCP Server"
    }

if __name__ == "__main__":
    # Run as MCP server using stdin/stdout
    mcp.run()