import os, sys
import requests
import multiprocessing
from io import BytesIO
from zipfile import ZipFile
from subprocess import call

JOM_ERROR = '''Error downloading jom! Please download it and unzip it in the build folder.
It should look like this:

+ build
'-+ jom
  |- changelog.txt
  |- ibjom.bat
  |- jom.exe
  |- xgejom.bat
  '- xgejom.xml

Download link: http://download.qt.io/official_releases/jom/jom.zip
'''

UPX_ERROR = '''Error downloading UPX! Please download it and unzip it in the build folder.
It should look like this:

+ build
'-+ upx-3.95-win-64
  |- BUGS
  |- COPYING
  |- LICENSE
  |- NEWS
  |- README
  |- README.1ST
  |- THANKS
  |- upx.1
  |- upx.doc
  |- upx.exe
  '- upx.html

Download link: https://github.com/upx/upx/releases/download/v3.95/upx-3.95-win64.zip
'''

HELP = '''python build.py [help|-h|--help] [build] [strip] [run]

help ............ print this help
build ........... build the executable
strip ........... compress the executable with UPX
run ............. run the program (does not build)

If no parameter is specified, build is assumed. '''

def run(s):
  return call(s.split())

def build ():
  print('Running qmake...')
  run('qmake ../DarkSoulsIII-Mods.pro')

  if not os.path.exists('jom') or not os.path.exists('jom/jom.exe'):
    print('Downloading jom...')
    r = requests.get(r'http://download.qt.io/official_releases/jom/jom.zip')
    if not r.ok:
      print(JOM_ERROR)
      exit(1)
    if not os.path.exists('jom'):
      os.mkdir('jom') # in case path exists but not .exe
    ZipFile(BytesIO(r.content)).extractall('jom')
  
  print('Building with Jom...')
  run(f'jom -j{multiprocessing.cpu_count()}')

def strip ():
  if not os.path.exists('upx-3.95-win64') or not os.path.exists('upx-3.95-win64/upx.exe'):
    print('Downloading UPX...')
    r = requests.get(r'https://github.com/upx/upx/releases/download/v3.95/upx-3.95-win64.zip')
    if not r.ok:
      print(UPX_ERROR)
      exit(1)
    ZipFile(BytesIO(r.content)).extractall()

  print('Stripping with UPX...')
  run(r'upx-3.95-win64\upx -9 release\DarkSoulsIII-PracticeTool.exe')

def debug():
  print('Building with Jom...')
  run(f'jom -j{multiprocessing.cpu_count()} -f Makefile.Debug')

def run_program():
  print('Running...')
  run(r'release\DarkSoulsIII-PracticeTool.exe')

if __name__ == '__main__':
  if not os.path.exists('build'):
    os.mkdir('build')
  os.chdir('build')

  if '-h' in sys.argv or '--help' in sys.argv or 'help' in sys.argv:
    print(HELP)
    exit()
  if 'build' in sys.argv or len(sys.argv) == 1:
    build()
  if 'strip' in sys.argv:
    strip()
  if 'debug' in sys.argv:
    debug()
  if 'run' in sys.argv:
    run_program()