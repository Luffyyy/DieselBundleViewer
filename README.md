
# Notice about Diesel V3
This project is no longer going to be updated, you should use the new and better Diesel Crate Browser https://modworkshop.net/mod/57750

# DieselBundleViewer

DieselBundleViewer is a program that allows you to view the files of all diesel games (PDTH, PD2, and RWW2)
The program is mostly based of DieselToolBox with some code taken from there.

## What's the difference between DieselToolBox and this?
* This version hopes to eliminate most of the bugs that the previous had and make it a viable option to DieselBundleModder.
* Hopefully more user friendly.
* New features!
  * Pressing the list headers actually sorts the items!
  * grid view for items.
  * Save files via dialog.
  * Powerful finder that let's you search with whole word and regex.
  * Select multiple bundles with a dialog and not a slow as fuck context menu.
  * Light/dark mode switch (defaults to dark 😎).
  * Option to hide 0 byte files (defaults to true).
  * Play/convert stream (wem) to wav on the fly.
  
* This version also moves to WPF / .NET Core; I've tried using a bunch of cross-platform .NET GUIs, but in the end I decided it's best to just go full WPF and so this means this program is only for Windows.

## Installation
Simply download from releases and unzip it somewhere.
In case you don't have the .NET 8 Desktop Runtime, the program will prompt you to install it. However, you can install it easily from here: https://dotnet.microsoft.com/download/dotnet/8.0

## Building
Built with Microsoft Visual Studio Community 2022 (64-bit), version 17.14.34, and .NET 8.0 targeting Windows. The project's target framework is `net8.0-windows7.0`. Make sure you have a compatible Visual Studio and the .NET 8.0 SDK.

Most icons are from https://icons8.com/

Source code of the DieselEngineFormats library https://github.com/Luffyyy/DieselEngineFormats

## Audio conversion (vgmstream)
Exporting stream/bnk audio to WAV is handled by [vgmstream](https://github.com/vgmstream/vgmstream), bundled in the `vgmstream-win64/` folder so it ships alongside the build. It correctly decodes the Wwise codecs PAYDAY 2 uses (IMA ADPCM and Vorbis) across their differing sample rates, which the previously bundled library mis-decoded. vgmstream is distributed under its own license; see `vgmstream-win64/COPYING`.
