# Unity History Window *Restyled*

This small plugin keeps track of the Unity Editor's object selection history. You can use keyboard shortcuts or the history window to return to previously selected objects.
It is really useful when editing stuff and following a link to an object reference to see some details and then go back to previous selection.

![Alt text](screenshots/Screenshot.png?raw=true "New Look")

This is a fork of [Unity Selection History Window](https://github.com/acoppes/unity-history-window) that updates the visual style to match Unity's. It also updates some outdated APIs and adds forward and backward buttons to the UI.

# Features

* Stores history of selected objects.
* Pin objects to the history window for quick access.
* Drag objects from the history window to move or link them.
* Use customizable keyboard shortcuts to move through the history, even when the window is closed.
* Double click to ping (focus) an object from the history.

# Installation

Open Unity Package Manager, select "Add package from git URL" and enter this URL:  
`git+https://git@github.com/adamgryu/unity-history-window-restyled.git?path=/Assets/Gemserk.SelectionHistory#release`

Tested with Unity 2020.3, but should theoretically work with 2019.4 and 2018.4.

# How To Use

* Follow `Window > General > History` to open the history window.
* Press `Ctrl+Alt+Left Arrow` to go backward through history and `Ctrl+Alt+Right Arrow` to go forward.
* Customize the keyboard shortcuts in the `Edit > Shortcuts > History` window.
* Click on the window's context menu to access the plugin's settings to customize the history window.

# Demo

Here's the plugin in action!

![Alt text](screenshots/Demo2.gif?raw=true "Demo")

Here's an example of dragging an element from the window.

![Alt text](screenshots/Drag3.gif?raw=true "Demo Drag")
