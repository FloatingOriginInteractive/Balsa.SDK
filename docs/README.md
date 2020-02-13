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
  
* **Config Mods:**  
	Config Mods are mods that add new definitions into the game. These mods only require working with text files, such as .modcfg and .cfg. Configs can be used to create definitions such as new Resources, Career Missions, Parts List categories, or even variants of existing Parts.
  
* **Plugin Mods:**
	Plugin Mods contain code, in the form of C# Assemblies. Plugin mods can be created using Visual Studio, and compiled into .dll files that the game can load.
  
* **Localization Mods:**
	You can also make mods that add or modify the text in the game. This is typically used to create translations of the game text into new languages, or add new terms for localizing your own mod. It can also be abused to modify the existing game texts as well. Localization mods consist mainly of .loc files, which are CSV formatted text files containing the terms and their translations to various languages.

* **Manual Pages:**  
	You can also write your own manual pages and include them in your mod. Manual pages are text-based *.man* files. They contain some metadata using cfg-style syntax, and a text section which supports Balsa's SRT notation, which is a Markdown-like syntax.  
	Like .loc files, .man files don't need to be listed in a modcfg entry. The game will load all .man files it can find inside the Addons folder system.


With the exception of Content Mods, all other mod types are published to the Workshop using the [Balsaworks Uploader tool](uploader.md).



