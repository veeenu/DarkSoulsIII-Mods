from common import *

if __name__ == '__main__':
  proc = findProcess('DarkSoulsIII.exe')
  h_proc = OpenProcess(PROCESS_ALL_ACCESS, False, proc['pid'])
  print('OpenProcess: ' + hex(GetLastError()) + ' ' + hex(proc['pid']) + ' ' + hex(proc['base']))

  # [DarkSoulsIII.exe + 47103D8] + 250
  input('Press enter to quitout')
  addr = eval_pointer_chain(h_proc, [ proc['base']+0x47103d8, 0x250 ])
  write(h_proc, addr, c_char(1))