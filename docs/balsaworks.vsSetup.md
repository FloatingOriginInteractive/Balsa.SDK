# Visual Studio Setup Guide

## Getting Started with a Plugin Mod project
Balsa supports plugin mods in the form of C# assemblies. 

This guide will take you through the steps to set up a Plugin mod project in Visual Studio 2019 Community.
It assumes you have Visual Studio installed already. If not, you can [get it here](https://visualstudio.microsoft.com/vs/community/).









### [Optional]: Automate copying output files to the mod folder
You can automate the process of copying over your output dll file from your VS solution to your mod folder in Balsa/Addons/, so that the output dll file gets copied at the end of each build.

To do this, open your project properties in VS, and in the Build Events tab, add the following line to your post-build command line:

```
copy /Y $(SolutionDir)$(OutDir)$(TargetFileName) <BalsaInstallFolder>\Addons\MyMod\$(TargetFileName)
```  
(remember to replace `<BalsaInstallFolder>` with the actual path to your Balsa install)

!()[img/vsaddonautomateoutputcopy.png]

Now, every time you run a successful build, VS should take care of copying over the dll file to where it needs to be.
