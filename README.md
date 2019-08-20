# Dark Souls III Mods

**Disclaimer**: this repository is deprecated and archived. It is recommended that you use the new version of the Practice Tool, which is lightweight, portable across patches and with an in-game UI.

You can find it here: https://github.com/veeenu/darksoulsiii-practice-tool

-----

Here you can find some utilities intended to aid in practicing Dark Souls III speedrunning in the least dreadful way possible.

This repository contains the source code for all the tools. 

You can download executable versions of the tools under the **Releases** tab.

## List of tools

### PracticeTool

[Download](https://github.com/veeenu/DarkSoulsIII-Mods/releases/download/0.0.5-alpha/PracticeTool.zip)

A general-purpose configurable practice tool. It currently has the following features:

- Save/load position
- No damage
- No death
- Deathcam
- Draw events
- Disable events
- Disable all AI
- Infinite stamina, FP, consumables (this one is currently broken)
- One-shot damage
- Show/hide map, character and objects
- Noclip + move up/down
- Speed multiplier (1x, 1.5x, 2x, 4x) (`,`)
- Locking/editing values for HP, FP, SP, and position

The `1.08` and `1.12` patches are currently supported, and the `1.04` support is under way.

The tool has hotkeys which can be configured in the `Settings` panel.

Just open Dark Souls III, run the executable and you're good to go.

## Building

The project has recently been ported from C#/Visual Studio to C++/Qt with static linkage,
in order to provide better user experience and better developer experience as well. This does
have a few drawbacks, as well, in terms of initial setup effort as Qt doesn't really play
nice for static builds with x64 Windows. I will try and explain my process as easy as possible.
I will assume you already have a recent and working Visual Studio installation with C++ available.
Credits to [this post](https://retifrav.github.io/blog/2018/02/17/build-qt-statically) for explaining
the process (you can refer to it for more in depth information).

### Building Qt

- Install a recent Python. I use 3.7 on [Miniconda](https://repo.anaconda.com/miniconda/Miniconda3-latest-Windows-x86_64.exe).
  Install [ActivePerl](https://www.activestate.com/products/activeperl/downloads/). Make sure
  both `perl` and `python`'s executables are in your `PATH` environment variable, hence reachable
  from any command prompt by typing their name.

- Download Qt's source code from [](https://www.qt.io/offline-installers). The package should
  be named something like `qt-everywhere-5.11.2-src.zip`. I will assume the version used is
  5.11.2, but adjust accordingly in what follows.
- Create a directory in some place of your liking such as `C:\Qt` where the installation will
  be placed, and extract the zip straight in this directory. You will now have a
  `C:\Qt\qt-everywhere-5.11.2-src` directory, which I renamed to `C:\Qt\Source-5.11.2` because
  it is comfier.
- Create another directory named `C:\Qt\5.11.2-static`. This will hold our installation when
  everything is said and done.
- Download and extract [Jom](http://download.qt.io/official_releases/jom/jom.zip) to a directory
  such as `C:\Qt\jom` (so you have the executable somewhere in `C:\Qt\jom\jom.exe`).
- Now create a directory named `C:\Qt\Shadow-5.11.2`. This will contain our [shadow build](https://wiki.qt.io/Qt_shadow_builds).
  Launch a [Developer command prompt](https://docs.microsoft.com/en-us/cpp/build/building-on-the-command-line?view=vs-2017)
  and navigate to this directory.
- It is time to launch our configuration and build, and pray everything goes all right:

```
cd C:\Qt\Shadow-5.11.2
C:\Qt\Source-5.11.2\configure.bat -release -static -no-pch -optimize-size -opengl desktop -platform win32-msvc -prefix "\path\to\Qt\511-static" -skip webengine -nomake tools -nomake tests -nomake examples
C:\Qt\jom\jom.exe -j8
C:\Qt\jom\jom.exe -j8 install
```

- If everything went all right -- it should have, I fought a lot before finding this combination --
  you now have a working Qt installation in `C:\Qt\5.11.2-static` and can add the `C:\Qt\5.11.2-static\bin`
  directory to the `PATH` environment variable. This is fundamental for my build script to work.

### Using the buildscript

The buildscript is pretty trivial to use.

```
python build.py [help|-h|--help] [build] [strip] [run]

help ............ print this help
build ........... build the executable
strip ........... compress the executable with UPX
run ............. run the program (does not build)

If no parameter is specified, build is assumed.
```

The compiled program will be found in `build\release`.

## Credits

- [Reverse Souls](https://github.com/igromanru/Dark-Souls-III-Cheat-Engine-Guide) for the CE table upon which some of the trainers and tools are based
- [retifrav](https://retifrav.github.io/blog/2018/02/17/build-qt-statically/) for the post about statically building Qt
- Pav for the invaluable help in figuring out the instant quitout static addresses

### Libraries

- [Qt](https://www.qt.io/)
- [skystrife/cpptoml](https://github.com/skystrife/cpptoml)
- [c42f/tinyformat](https://github.com/c42f/tinyformat)