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
In case you don't have .NET Core 3.1, the program will prompt you to install it. However, you can install it easily from here: https://dotnet.microsoft.com/download/dotnet-core/current/runtime

## Builiding
The program is built using Visual Studio 2019 and .NET Core 3.1. Make sure you have both.

Most icons are from https://icons8.com/

Source code of the DieselEngineFormats library https://github.com/Luffyyy/DieselEngineFormats
