# Milkstain
Implementation of Milkdrop Visualizer in Unity. Based on <a href=https://github.com/jberg/butterchurn>Butterchurn</a>.

![Screenshot](Screenshot.png)

## Usage

### Requirements
Milkstain can be driven from any ```AudioSource``` as long as ```AudioClip``` is not compressed. Otherwise the time array will be filled with zeros.

RenderTextures are used for generating output image. To display visualizer on a surface just pass ```FinalTexture``` into your material on runtime.

### Playing presets
For visualizer to work, first you need to instantiate the prefab ```Assets/Milkstain/Resources/Milkdrop.prefab```. It has everything already set up.

Next, pass your ```AudioSource``` to ```TargetAudio``` field.

Then call ```Initialize()```.

After these steps, ```FinalTexture``` can be grabbed and passed to your desired display location.

Every preset is stored as a ```TextAsset``` in ```PresetFiles``` array. It will automatically pick one of them every 15 seconds. Time can be adjusted in ```ChangePresetIn``` field. To manually switch the preset call ```PlayPreset(index, transitionDuration)``` for a specific pick or ```PlayRandomPreset(transitionDuration)``` to let it pick the next preset automatically.

Usage example is included into the project.

## Limitations
Currently presets with custom Warp and Comp shaders are not supported because Unity requires shaders to be precompiled in editor. If you don't plan to give user ability to load custom presets, shaders can be precompiled and then used for a specific preset.

Custom editor script for generating shaders may be added in the future.

I wrote this port for a small throwaway easter egg in my own project, so it might be inaccurate or contain bugs!
