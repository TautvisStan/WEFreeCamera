# WEFreeCamera
Enables the free camera in game. Mainly intended to use in spectating AI matches.

Main camera script is based on [UnityExplorer's](https://github.com/sinai-dev/UnityExplorer) Freecam feature, modified to fit the game.
## KNOWN ISSUES
The game camera will break if the free camera is enabled during scene transitions. Make sure to disable the free camera before changing the game scene. To fix, switch scenes while the free camera is disabled.

Character controller icons are not displayed if the character is not visible on the main camera.
## Usage
You can install this mod using the mod manager or by putting `WEFreeCamera.dll` to `BepInEx/plugins` folder.
Default controls:
- O - enable/disable the free camera.
- L - lock/unlock the free camera in place.
- Up Arrow - move the camera forwards.
- Down Arrow - move the camera backwards.
- Left Arrow - move the camera left.
- Right Arrow - move the camera right.
- Spacebar - move the camera up.
- LeftControl - move the camera down.
- RightShift - move the camera in super speed.
- Right Mouse Button - hold and drag the mouse to rotate the camera.

Default move speed and controls can be customized in the mod config.
