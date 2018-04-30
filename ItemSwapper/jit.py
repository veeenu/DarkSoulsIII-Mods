from keystone import *

ks = Ks(KS_ARCH_X86, KS_MODE_64)

def prasm(s):
  a, b = ks.asm(s)

  print(', '.join(['0x{:X}'.format(i) for i in a]))

prasm('jmp 0xffffffff; nop')
prasm('movq rcx, 0xffffffffffffffff; mov [rcx], rbx; mov ecx, [rbx + 0x20]; mov [r14], ecx; jmp 0xffffffff')