#pragma once

%_PRAGMA_COMMENTS_%

#include "windows.h"
#include "ios"
#include "fstream"

%_TYPEDEF_%

// Remove this line if you aren't proxying any functions.
HMODULE hModule = LoadLibrary(L"%_REAL_DLL_%");

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

// Remove this function if you aren't proxying any functions.
VOID DebugToFile(LPCSTR szInput)
{
    std::ofstream log("spartacus-proxy.log", std::ios_base::app | std::ios_base::out);
    log << szInput;
    log << "\n";
}

%_FUNCTIONS_%