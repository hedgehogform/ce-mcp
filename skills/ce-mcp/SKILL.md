---
name: ce-mcp
description: Use this skill to operate the ce-mcp Cheat Engine MCP server for processes, memory and pointers, scans, symbols and RTTI, Structure Dissect, cheat tables, disassembly, injection, debugger, DBVM, address-list, conversion, or Lua workflows; or to consult the installed Cheat Engine celua.txt.
---

# CE MCP

Use ce-mcp as a Cheat Engine MCP operator guide. Prefer the dedicated MCP tools for normal work, use `execute_lua` only when the exposed tools cannot do the job, and consult the user's installed Cheat Engine Lua reference before writing CE Lua.

Scope: this skill assumes ce-mcp is already installed or running. Its job is to help callers operate the MCP server and local Cheat Engine runtime.

## Start Here

1. Confirm the MCP server is running at the configured Streamable HTTP URL, normally `http://localhost:6300/`.
2. Call `get_plugin_version` and `get_current_process` before acting on memory.
3. If no target is open, use `get_process_list` then `open_process`.
4. Choose a dedicated tool from `references/tool-catalog.md`.
5. Read `references/lua-execution.md` before using `execute_lua` or writing Cheat Engine Lua.

Memory and physical-memory writes, allocation/protection changes, file load/save, process creation, injection, remote execution, debugger control, DBVM, Lua, and assembly can change target or host state. Obtain explicit confirmation for the exact target and values immediately before consequential calls.

## Tool Selection

Read `references/tool-catalog.md` when choosing tools or building a workflow. Prefer the live MCP client tool schemas for exact parameter names, defaults, and required fields when they are available.

Default workflow:

- Process: inspect and open the intended target before target-memory work; treat `create_process` as an external launch.
- Symbols/modules: enumerate modules, RTTI, and registered symbols; resolve expressions before strict-address tools.
- Memory/pointers: use bounded reads, pointer chains, region operations, and direct-reference scans; distinguish them from CE Pointer Scanner.
- Scans: use named independent scanners for automation, reset before a fresh first scan, and preserve empty/zero/false positional inputs.
- Structures/tables: use Structure Dissect and .CT tools; remember that global structures and address-list records persist in saved tables.
- Code/injection: generate and syntax-check scripts before execution; confirm injection, compilation, and remote calls.
- Debugger: use `dbg_*` for breakpoints, hit tracking, thread/context inspection, stepping, and find-writes/accesses workflows.
- DBVM: check `dbvm_status` first; physical writes and OS offload require explicit confirmation.
- Lua: use `execute_lua` only when the dedicated surface cannot perform the operation.

## Lua Reference And Local CE Path

Cheat Engine installs usually include `celua.txt`, which documents the CE Lua API for that installed build. A common Windows location is:

```text
C:\Program Files\Cheat Engine\celua.txt
```

Do not assume `celua.txt` ships with this skill package. Treat the Cheat Engine install path as the source of truth for Lua definitions.

When the user provides a Cheat Engine install path, or asks to update the skill with a CE path, find and verify the install directly. Check the user-provided path first, then common Windows paths such as `C:\Program Files\Cheat Engine` and `C:\Program Files (x86)\Cheat Engine`. A valid install must contain `celua.txt`; a CE executable, `plugins`, and `ce.runtimeconfig.json` are useful supporting signals.

After verifying the install, edit `references/local-cheat-engine.md` in this skill folder using `references/local-cheat-engine.example.md` as the shape. This file is local-machine state.

Reference priority for CE Lua:

1. `skills/ce-mcp/references/local-cheat-engine.md`, if present, for the configured install path and exact `celua.txt`.
2. The configured install's `celua.txt`.

If no local Cheat Engine path is configured and Lua definitions are needed, ask the user for the Cheat Engine install path before guessing APIs.
