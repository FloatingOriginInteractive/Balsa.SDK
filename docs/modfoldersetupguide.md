# Creating Addon/Plugin Mods




### Setting up your Mod Project
The first step when starting any new Addon or Plugin mod is to create a mod folder in the game's Addon folder. This is located in `<Path to Steam Library>\steamapps\common\BALSA Model Flight Simulator\Addons`

-------

### .modcfg Files
As well as a mod folder, all addon mods need to have a `.modcfg` file. This file is like an index into your mod content. 

Modcfg files tell the game what content the mod contains, and where to find it. These files can also contain new definitions as well, for things like Part Resources, Career Missions, Parts List categories and filtering options, and so on.

The modcfg file then is essentially a table of contents for your mod. If it isn't listed there, the game will not load it.  

Have a look at the modcfg files in the basegame folder for reference and examples. You can have multiple modcfg files for different things in your mod folder, or you can combine it all into one file. 

You can name your modcfg file anything you want, as long as it has the .modcfg extension.

---------

# Start your Mod Project

With your mod folder set up and with a .modcfg file in place, you can now begin creating your actual mod.

There are several different types of mod content you can create. Check out the guides for setting up projects for each type:

[Plugin Mods using Visual Studio 2019](vsProjectSetup.md)
