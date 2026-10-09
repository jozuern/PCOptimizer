# NVIDIA shader cache: unlimited

## Summary
Lets the NVIDIA shader cache on disk grow without the driver's size limit, so compiled shaders are not deleted and recompiled.

## How it works
Games compile their shaders for your graphics card; the driver stores the results on disk and reuses them next time. "Shader Cache Size" in the NVIDIA Control Panel controls the maximum disk space the driver may use for this, with the options Disabled, Unlimited or a size from 128 MB to 100 GB [1]. The app writes the maximum value that NVIDIA's settings header defines for this setting into the global profile [3].

## Why it can help
If you play many large games, a full cache means shaders are compiled again, which shows as stutter the first minutes of a session.

## Evidence
The driver default is 16 GB [2]. NVIDIA advises a limit large enough for the games you usually play [1]; unlimited only matters if your games need more than the default.

## Trade-offs & risks
The cache can use a lot of disk space with many games. A new driver deletes it, and the first sessions afterwards can stutter while shaders are compiled again [1]. You can delete it by hand in the NVIDIA cache folders [2].

## When not to use it
On a small, nearly full system drive.

## Sources
1. https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_(reference).htm
2. https://nvidia.custhelp.com/app/answers/detail/a_id/5735/~/deleting-nvidia-shader-cache-files
3. https://github.com/NVIDIA/nvapi/blob/main/NvApiDriverSettings.h
