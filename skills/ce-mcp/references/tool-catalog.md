# Tool Catalog

This catalog summarizes the ce-mcp tools exposed by the MCP server. Prefer the live MCP schemas for exact parameter names, defaults, and required fields.

## Process

- Inspection: `get_plugin_version`, `get_process_list`, `get_current_process`, `get_process_state`.
- Attachment: `open_process`, `open_foreground_process`.
- Control: `create_process`, `pause_process`, `resume_process`.

Start target-memory workflows with `get_current_process`. `create_process` launches an executable; confirm the exact path and parameters before calling it.

## Memory

- Basic access: `read_memory`, `write_memory` (byte and string operations are capped at 1 MiB per call).
- Allocation: `allocate_memory`, `allocate_shared_memory`, `free_memory`.
- Regions: `enum_memory_regions`, `get_memory_protection`, `set_memory_protection`.
- Transfer and comparison: `copy_memory`, `compare_memory`, `hash_memory`.
- Files: `dump_memory`, `load_memory`.

`dump_memory` creates or overwrites a file. `load_memory`, `write_memory`, allocation, freeing, copying to an existing destination, and protection changes alter the target; confirm scope first. Use `resolve_address` before tools that require strict hexadecimal addresses.

## Pointers

- `read_pointer_chain`: Dereference a base and apply each signed offset; returns every step and readability state.
- `find_pointer_references`: Run a bounded direct-reference scan over at most 64 MiB.
- `get_pointer_size`: Read or set CE's target pointer size, normally 4 or 8.

`find_pointer_references` is intentionally not presented as CE Pointer Scanner: the installed CE Lua API has no pointer-scanner binding. It finds direct references only.

## Scanning

- `aob_scan`: Return all AOB matches reported by CE.
- `aob_scan_unique`: Return the first global AOB match when the caller expects uniqueness.
- `aob_scan_module_unique`: Return the first AOB match inside one module.
- `string_scan`: Run a fresh exact string scan with a named independent scanner.
- `memory_scan`: Perform first or next value scans.
- `reset_memory_scan`: Reset the main or a named independent scanner.

Empty protection/alignment strings, zero-valued enums, and `false` flags are valid positional CE arguments and must not be dropped. Prefer named scanners for automation so the CE GUI scanner is unchanged.
Named scanners are limited to 32 active instances and names are limited to 64 characters; reset scanners when finished.

Useful enum values:

- `ScanOption`: `soUnknownValue`, `soExactValue`, `soValueBetween`, `soBiggerThan`, `soSmallerThan`, `soIncreasedValue`, `soIncreasedValueBy`, `soDecreasedValue`, `soDecreasedValueBy`, `soChanged`, `soUnchanged`.
- `VariableType`: `vtByte`, `vtWord`, `vtDword`, `vtQword`, `vtSingle`, `vtDouble`, `vtString`, `vtByteArray`, `vtGrouped`, `vtBinary`, `vtAll`.
- `AlignmentType`: `fsmNotAligned`, `fsmAligned`, `fsmLastDigits`.

Typical value scan:

1. Choose a `scannerName` and call `reset_memory_scan(scannerName=...)`.
2. Call `memory_scan(scannerName=..., scanOption="soExactValue", varType="vtDword", input1="100")`.
3. Observe or change the target value.
4. Narrow with the same scanner name and next-scan condition.
5. Reset the named scanner when finished.

## Symbols, Modules, And RTTI

- Modules/symbols: `enum_modules`, `get_symbol_info`, `get_name_from_address`, `get_module_size`, `resolve_address`.
- Loading: `enable_symbols`, `reinitialize_symbols`, `wait_for_symbols`.
- RTTI: `get_rtti_class_name`.
- User symbols: `register_symbol`, `unregister_symbol`, `enum_registered_symbols`.

Prefer module+offset or symbols in explanations. `register_symbol` changes CE symbol state and can persist in a table unless `doNotSave=true`.

## Structure Dissect

- Inspect: `list_structures`, `get_structure`.
- CRUD: `create_structure`, `delete_structure`, `add_structure_element`, `remove_structure_element`.
- Analysis: `autoguess_structure`, `compare_structures`.

Structure element `variableType` uses CE's numeric variable-type constants from the installed `defines.lua`. `autoguess_structure` is bounded to 1 MiB. Structure changes affect global CE state and saved tables.

## Disassembly, Assembly, And Analysis

- Basic: `disassemble`, `disassemble_range`, `disassemble_bytes`, `get_previous_opcodes`, `get_function_range`.
- Bounded analysis: `analyze_code_range`, `search_disassembly`.
- Assembly: `assemble`, `auto_assemble_check`, `auto_assemble`.
- Memory View: `set_comment`.

Call `auto_assemble_check` before `auto_assemble` when practical. `analyze_code_range` and `search_disassembly` enforce byte and instruction caps to keep CE's GUI thread responsive.
`auto_assemble` disable IDs are session-local: retain the returned ID and pass it with the identical script to disable that script before restarting CE or the plugin.

## Cheat Tables And Address List

- Table files: `load_cheat_table`, `save_cheat_table`.
- Records: `get_address_list`, `add_memory_record`, `update_memory_record`, `delete_memory_record`, `clear_address_list`.

For pointer records, offsets are outermost-to-innermost, for example `0x10,0x18`. Loading with `merge=false` replaces the current table; saving creates or overwrites the destination file. Confirm both operations first.

## Injection And Remote Execution

- Libraries: `inject_library`, `inject_dotnet_assembly`.
- Calls: `execute_remote_function`, `execute_remote_function_ex`.
- Generation: `generate_api_hook_script`, `generate_code_injection_script`.
- Compilation: `compile_c`.

Generation tools return text and do not execute it. `execute_remote_function_ex` returns `result=null` for CE's nil result, including fire-and-forget calls (`timeout=0`). Injection, target C compilation, and remote calls can execute arbitrary code; confirm the exact target, path/source, address, calling convention, parameters, and timeout immediately before use.

## Debugger

- Attach/status: `dbg_start`, `dbg_exit`, `dbg_is_debugging`, `dbg_is_broken`.
- Breakpoints: `dbg_add_bp`, `dbg_toggle_bp`, `dbg_delete_bp`, `dbg_bps`, `dbg_add_thread_bp`.
- Hit tracking: `dbg_get_bp_hits`, `dbg_clear_bp_hits`.
- Thread control: `dbg_break_thread`, `dbg_exclude_thread`, `dbg_include_thread`.
- Registers/context: `dbg_gpregs`, `dbg_gpregs_remote`, `dbg_regs`, `dbg_regs_all`, `dbg_regs_named`, `dbg_regs_named_remote`, `dbg_regs_remote`, `dbg_context_table`, `dbg_read_xmm`.
- Execution: `dbg_continue`, `dbg_step_into`, `dbg_step_over`, `dbg_run_to`.
- Last branch recording: `dbg_lbr_enable`, `dbg_lbr_records`.
- Stack/memory: `dbg_stacktrace`, `dbg_read`, `dbg_write`.

Find what writes/accesses:

1. `dbg_start`.
2. `dbg_add_bp(address="0x...", size=4, trigger="write" | "access", trackHits=true)`.
3. Trigger the target action.
4. Read `dbg_get_bp_hits`.
5. Inspect instructions/registers, then call `dbg_delete_bp`.

Thread breakpoints and last-branch recording depend on the active debugger interface and target. LBR generally requires kernel debugging and compatible hardware.

## DBVM

- Availability: `dbvm_status`, `dbvm_initialize`.
- Physical memory: `dbvm_read_physical`, `dbvm_write_physical`.
- Watches: `dbvm_watch`, `dbvm_watch_log`, `dbvm_watch_stop`.

DBVM is optional. Tools return an availability error when it is not initialized. Physical writes, OS offload, and watch configuration are high-risk host operations; never call them without explicit confirmation of the exact physical address and scope.

## Lua And Conversion

- `execute_lua`: Run CE Lua and return serialized values, including tables and multiple returns.
- `convert_string`: Convert `md5`, `ansitoutf8`, or `utf8toansi`.

Use `execute_lua` only when no dedicated tool fits. Read `lua-execution.md` and the installed `celua.txt` first.
