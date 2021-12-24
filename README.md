# papyMonitor

![papyMonitor](small.gif)

## Description

papyMonitor-gui is a multiplatform (Windows, Linux, Mac, ...) tool to interactively receive/edit/monitor data and send commands to an embedded system with the serial port. The embedded system can be Arduino (all flavors supported), PIC, AVR, ARM,... or a computer.

The tool provide a multifunction plotter as well a 3D display to simulate real mechanical systems.

It is fast, real time (the embedded system is the master) and proudly made with Godot Engine. The language used is C#, the user configuration file rely on Lua language but is very easy to manipulate.

This tool is already used in production but -for sure- still has some bugs to discover. The MAC version has not been tested since we don't have a MAC, so developers are welcome.

## Installation

Go to Releases and download the binary for your platform

## Installation (developers)

Clone this repo and adapt the .vscode to your need if you use VSCode

## Usage/Documentation

The application configure itself (GUI, behavior,...) based on informations given in  **one** configuration file (a *.lua file). This *.lua file is writen by the user.

For the embedded system configuration, see the related sections

## Contributing

This project can be enhanced in several ways, ideas and contributors welcomed

## Credits

Thanks to Godot Engine developpers who have done this excellent multiplatform development tool

## License

MIT license
