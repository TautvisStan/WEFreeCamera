# WEFreeCamera
Enables the free camera in game. Mainly intended to use in spectating AI matches.

Main camera script is based on [UnityExplorer's](https://github.com/sinai-dev/UnityExplorer) Freecam feature, modified to fit the game.
## Usage
You can install this mod using the mod manager or by putting `WEFreeCamera.dll` to `BepInEx/plugins` folder.
### Camera angle saving
Pressing the `~` button and a number at the top of the keyboard at the same time will save the free camera position and rotation to that slot. Pressing the slot number will load the saved position. Saves persist even after restarting the game. Supports up to 10 saved camera angles.
### Default controls:
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
- Q - toggle the action targeting mode.
- [ - increase the camera field of view.
- ] - decrease the camera field of view.

Default move speed and controls can be customized in the mod config.
