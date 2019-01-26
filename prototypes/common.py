from ctypes import *
from ctypes.wintypes import *

k32 = windll.kernel32
psapi = windll.psapi

SIZE_T = c_ulong
PROCESS_QUERY_INFORMATION = 0x0400
PROCESS_VM_READ = 0x0010
# Constants
DEBUG_PROCESS         = 0x00000001
CREATE_NEW_CONSOLE    = 0x00000010
PROCESS_ALL_ACCESS    = 0x001F0FFF
INFINITE              = 0xFFFFFFFF
DBG_CONTINUE          = 0x00010002

# Debug event constants
EXCEPTION_DEBUG_EVENT      =    0x1
CREATE_THREAD_DEBUG_EVENT  =    0x2
CREATE_PROCESS_DEBUG_EVENT =    0x3
EXIT_THREAD_DEBUG_EVENT    =    0x4
EXIT_PROCESS_DEBUG_EVENT   =    0x5
LOAD_DLL_DEBUG_EVENT       =    0x6
UNLOAD_DLL_DEBUG_EVENT     =    0x7
OUTPUT_DEBUG_STRING_EVENT  =    0x8
RIP_EVENT                  =    0x9

# debug exception codes.
EXCEPTION_ACCESS_VIOLATION     = 0xC0000005
EXCEPTION_BREAKPOINT           = 0x80000003
EXCEPTION_GUARD_PAGE           = 0x80000001
EXCEPTION_SINGLE_STEP          = 0x80000004


# Thread constants for CreateToolhelp32Snapshot()
TH32CS_SNAPHEAPLIST = 0x00000001
TH32CS_SNAPPROCESS  = 0x00000002
TH32CS_SNAPTHREAD   = 0x00000004
TH32CS_SNAPMODULE   = 0x00000008
TH32CS_INHERIT      = 0x80000000
TH32CS_SNAPALL      = (TH32CS_SNAPHEAPLIST | TH32CS_SNAPPROCESS | TH32CS_SNAPTHREAD | TH32CS_SNAPMODULE)
THREAD_ALL_ACCESS   = 0x001F03FF

# Context flags for GetThreadContext()
CONTEXT_FULL                   = 0x00010007
CONTEXT_DEBUG_REGISTERS        = 0x00010010

# Memory permissions
PAGE_EXECUTE_READWRITE         = 0x00000040

# Hardware breakpoint conditions
HW_ACCESS                      = 0x00000003
HW_EXECUTE                     = 0x00000000
HW_WRITE                       = 0x00000001

# Memory page permissions, used by VirtualProtect()
PAGE_NOACCESS                  = 0x00000001
PAGE_READONLY                  = 0x00000002
PAGE_READWRITE                 = 0x00000004
PAGE_WRITECOPY                 = 0x00000008
PAGE_EXECUTE                   = 0x00000010
PAGE_EXECUTE_READ              = 0x00000020
PAGE_EXECUTE_READWRITE         = 0x00000040
PAGE_EXECUTE_WRITECOPY         = 0x00000080
PAGE_GUARD                     = 0x00000100
PAGE_NOCACHE                   = 0x00000200
PAGE_WRITECOMBINE              = 0x00000400

VirtualAllocEx = k32.VirtualAllocEx
VirtualFreeEx = k32.VirtualFreeEx
ReadProcessMemory = k32.ReadProcessMemory
WriteProcessMemory = k32.WriteProcessMemory
CreateRemoteThreadEx = k32.CreateRemoteThreadEx
OpenProcess = k32.OpenProcess
CloseHandle = k32.CloseHandle
EnumProcesses = psapi.EnumProcesses
EnumProcessModules = psapi.EnumProcessModules
GetModuleBaseNameA = psapi.GetModuleBaseNameA
GetModuleInformation = psapi.GetModuleInformation
GetLastError = k32.GetLastError

VirtualAllocEx.restype = LPVOID
VirtualAllocEx.argtypes = (HANDLE, LPVOID, DWORD, DWORD, DWORD)
VirtualFreeEx.argtypes = (HANDLE, LPVOID, SIZE_T, DWORD)
ReadProcessMemory.argtypes = (
    HANDLE, LPCVOID, LPVOID, SIZE_T, POINTER(c_ulong)
)
WriteProcessMemory.argtypes = (HANDLE, LPVOID, LPCVOID, DWORD, LPDWORD)
CreateRemoteThreadEx.argtypes = (
    HANDLE, LPVOID, DWORD, LPVOID, LPVOID, DWORD, LPVOID, LPDWORD
)

#
# MODULEINFORMATION
#

class MODULEINFORMATION(Structure):
  _fields_ = [
    ("lpBaseOfDll", LPVOID),
    ("SizeOfImage", DWORD),
    ("EntryPoint", LPVOID)
  ]

def findProcess(name):
  arr = c_ulong * 256
  lpidProcess = arr()
  cb = sizeof(lpidProcess)
  cbNeeded = c_ulong()
  hModule = (HMODULE * 64)()
  count = c_ulong()
  modname = create_string_buffer(30)

  EnumProcesses(byref(lpidProcess),
                cb,
                byref(cbNeeded))

  nReturned = int(cbNeeded.value / sizeof(c_ulong()))

  pidProcess = [i for i in lpidProcess][:nReturned]

  ret = (None, None)

  for pid in pidProcess:

    h_proc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, False, pid)
    if h_proc:
      EnumProcessModules(h_proc, byref(hModule), sizeof(hModule), byref(count))
      GetModuleBaseNameA(h_proc, c_ulonglong(hModule[0]), modname, sizeof(modname))
      procname = ''.join([str(i.decode('utf-8')) for i in modname if i != b'\x00'])

      if procname.lower() == name.lower():
        hModInfo = MODULEINFORMATION()
        GetModuleInformation(h_proc, c_ulonglong(hModule[0]), byref(hModInfo), sizeof(MODULEINFORMATION))
        ret = dict(name=procname, pid=pid, base=hModule[0], info=hModInfo)

      for i in range(modname._length_):
        modname[i] = 0

      CloseHandle(h_proc)

  return ret

def read(h_proc, addr, c_type):

  dest = c_type()
  br = c_ulong()
  ret = ReadProcessMemory(h_proc, c_void_p(addr), byref(dest), sizeof(dest), byref(br))
  if ret == 0:
    raise Exception('ReadProcessMemory: ' + hex(GetLastError()))
  return dest

def write(h_proc, addr, buff):

  br = c_ulong()

  ret = WriteProcessMemory(h_proc, addr, byref(buff), sizeof(buff), byref(br))
  if ret == 0:
    raise Exception('WriteProcessMemory: ' + hex(GetLastError()))

def eval_pointer_chain(h_proc, ptrc):
  addr = c_ulonglong(ptrc[0])
  for i in ptrc[1:]:
    print(hex(addr.value))
    addr = read(h_proc, addr.value, c_ulonglong)
    addr = c_ulonglong(addr.value + i)

  return addr.value