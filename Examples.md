# Spartacus Examples

## Contents

I want to...

* [Find applications that are vulnerable to DLL Hijacking](#i-want-to-find-applications-that-are-vulnerable-to-dll-hijacking)
* [Find applications that are vulnerable to COM Hijacking](#i-want-to-find-applications-that-are-vulnerable-to-com-hijacking)
* [Parse an existing SysInternals Process Monitor Log](#i-want-to-parse-an-existing-sysinternals-process-monitor-log)
* [Create Visual Studio solutions for all the vulnerable DLLs identified](#i-want-to-create-visual-studio-solutions-for-all-the-vulnerable-dlls-identified)
* [Make the output to include all DLLs even if they are in a privileged directory (ie C:\Windows)](#i-want-the-output-to-include-all-dlls-even-if-they-are-in-a-privileged-directory-ie-cwindows)
* [Scan the local device for misconfigured COM objects](#i-want-to-scan-the-local-device-for-misconfigured-com-objects)
* [View the exports for a DLL](#i-want-to-view-the-exports-for-a-dll)
* [Create a proxy for a specific DLL](#i-want-to-create-a-proxy-for-a-specific-dll)
* [Run my implant outside of DllMain, use Ghidra and pre-generated prototypes to create function definitions](#i-dont-want-to-run-my-implant-from-dllmain-use-ghidra-and-pre-generated-prototypes-to-create-function-definitions)
* [Only proxy specific functions from the DLL, and forward the rest](#only-proxy-specific-functions-from-the-dll-and-forward-the-rest)
* [Create a certificate to self-sign DLLs](#i-want-to-create-a-certificate-to-self-sign-dlls)
    * [But I can't be bothered to fill in the Subject/Issuer fields myself](#i-cant-be-bothered-to-fill-in-the-subjectissuer-fields-myself)
* [Sign a DLL](#i-want-to-sign-a-dll)

## Examples

### I want to find applications that are vulnerable to DLL Hijacking

```
Spartacus.exe --mode dll --procmon "C:\SysInternals\ProcMon64.exe" --pml "C:\Output\ProcMonOutput.pml" --csv "C:\Output\VulnerableDLLs.csv" --verbose
```

### I want to find applications that are vulnerable to COM Hijacking

```
Spartacus.exe --mode com --verbose --procmon "C:\SysInternals\ProcMon64.exe" --pml "C:\Output\ProcMonOutput.pml" --csv "C:\Output\VulnerableCOM.csv"
```

### I want to parse an existing SysInternals Process Monitor Log

```
Spartacus.exe --mode dll --existing --pml "C:\Output\ProcMonOutput.pml" --csv "C:\Output\VulnerableDLLs.csv" --verbose
```

### I want to create Visual Studio solutions for all the vulnerable DLLs identified

Add `--solution "C:\Output\VisualStudioProjects"` to [I want to find applications that are vulnerable to DLL Hijacking](#i-want-to-find-applications-that-are-vulnerable-to-dll-hijacking).

### I want the output to include all DLLs even if they are in a privileged directory (ie C:\Windows)

Add `--all` to [I want to find applications that are vulnerable to DLL Hijacking](#i-want-to-find-applications-that-are-vulnerable-to-dll-hijacking).

### I want to scan the local device for misconfigured COM objects

```
Spartacus.exe --mode com --verbose --acl --csv "C:\Output\MisconfiguredCOM.csv"
```

### I want to view the exports for a DLL

```
Spartacus.exe --mode proxy --action exports --dll "C:\Windows\System32\version.dll" --prototypes "./Assets/prototypes.csv" --verbose
```

### I want to create a proxy for a specific DLL

```
Spartacus.exe --mode proxy --action default --dll "C:\Windows\System32\version.dll" --solution "C:\Output\VisualStudioProjects" --prototypes "./Assets/prototypes.csv" --verbose
```

### I don't want to run my implant from DllMain, use Ghidra and pre-generated prototypes to create function definitions

```
Spartacus.exe --mode proxy --action default --dll "C:\Windows\System32\amsi.dll" --ghidra "C:\Ghidra\support\analyzeHeadless.bat" --solution "C:\Output\AmsiSolution" --prototypes "./Assets/prototypes.csv" --verbose
```
#### Only proxy specific functions from the DLL, and forward the rest

```
Spartacus.exe --mode proxy --action default --dll "C:\Windows\System32\amsi.dll" --ghidra "C:\Ghidra\support\analyzeHeadless.bat" --solution "C:\Output\AmsiSolution" --only "FunctionA" --prototypes "./Assets/prototypes.csv" --verbose
```

### I want to create a certificate to self-sign DLLs

```
Spartacus.exe --mode sign --action generate --pfx "C:\Output\certificate.pfx" --password "Welcome1" --not-before "2022-12-31 00:00:02" --not-after "2026-01-01 00:00:03" --issuer "CN=Microsoft" --subject "CN=Microsoft" --verbose
```

#### I can't be bothered to fill in the Subject/Issuer fields myself

```
Spartacus.exe --mode sign --action generate --pfx "C:\Output\certificate.pfx" --password "Welcome1" --not-before "2022-12-31 00:00:55" --not-after "2026-01-01 00:00:01" --copy-from C:\Windows\System32\version.dll --verbose
```

### I want to sign a DLL

```
Spartacus.exe --mode sign --action sign --pfx "C:\Output\certificate.pfx" --password "Welcome1" --path "C:\Input\MyFakeVersion.dll" --algorithm SHA256 --verbose
```
