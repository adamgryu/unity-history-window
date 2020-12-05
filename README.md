# Changes in Fork

This is a fork of [Unity Selection History Window](https://github.com/acoppes/unity-history-window) that updates the visual style to match modern Unity better. It also updates some outdated APIs and adds forward and backward buttons.

![Alt text](screenshots/new.png?raw=true "New Look")

# Unity Selection History Window

This is a small plugin that keeps a history of the Unity's Editor object selection (it stores in the background) and displays it in a Window to easily access it. 

It is really useful when editing stuff and following a link to an object reference to see some details and then go back to previous selection.

# Features

* Stores history of selected objects (custom count).
* Selects objects from the history (with left click).
* Pings (focus) objects from the history (with right click or Ping button).
* Drag objects from history to other object fields to link them.
* Drag assets (folders, scripts, etc) from history to the project browser to move them.

# Install using UPM

Just open Unity Package Manager and select add package from git URL and add this `git+https://git@github.com/adamgryu/unity-history-window-updated.git#master`

# Download 

[Unity Package](release/unity-selection-history.unitypackage?raw=true)

# Demo

![Alt text](screenshots/demo.gif?raw=true "Demo")

![Alt text](screenshots/demodrag.gif?raw=true "Demo Drag")
