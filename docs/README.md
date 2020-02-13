# Welcome
Welcome to the Balsa Addons SDK documentation site. Here you can find information to get you started making addons for Balsa Model Flight Simulator.

Keep in mind that just as the game, these pages are also a work in progress.

## Setting Up
If you're just getting started, check out the [Getting Started](gettingstarted.md) guide.


## Balsa Modding 101
Balsa was designed from the group up to support player-generated creations. Apart from vehicles and missions, you can create new content for the game, adding new Parts, Maps, or even expand the game itself with new features by creating C# plugins. 

Throughout this documentation, we use the word 'Mod' when referring to anything that can be shared with others using the Steam Workshop. Mods can then be further categorized like this:


* **Content Mods:**
 	Content mods are submissions that only contain data files, such as .craft files for vehicles, or .scn files for missions. Content mods are exported to the Steam Workshop from inside the game itself.
  
  
  
* **Addon Mods:**
	Addons are mods that add extra content to the game, such as new parts, sceneries, etc. When we talk about Addons, we are referring to these types of mods, which are created using the BalsaAddons_Unity project, and exported for the game to load.
  
  
  
* **Plugin Mods:**
	Plugin Mods contain code, in the form of C# Assemblies. Plugin mods can be created using Visual Studio, and compiled into .dll files that the game can load.
  

Both Addon and Plugin mods get published to the Workshop using the [Balsaworks Uploader tool](uploader.md).



