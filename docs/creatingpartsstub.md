:warning: This page is only a stub. 

---------------


////////// Part 1: Basic Setup for Exporting a model as a part ///////////

- import meshes and materials into project
- set up mesh as GO in scene
- rename as needed

- rotate mesh to neutral orientation

- tools/floating origin studios/exp/parts setup
- click create part setup

- add collision components 
- make sure all part colliders are convex

- set up attachment nodes 
	- orient node axes as needed
		- surface nodes
			- primary axis (lime) will align to srf normal
			- secondary axis (grey) defines forward direction of the part
			
- create prefab for the part



- tools/floating origin studios/part export tools

- make sure prefabs list matches the scene (hit Get Prefabs from Scene button to make sure)

- set up output folder path (hit browse)

- Click Pre-Process Enabled Part Prefabs to generate export assets

- Open AssetBundleBrowser panel
	- Configure Tab
		- Check part prefabs assignment to assebundles, create shared asset bundles if needed,  move duplicated assets to shared bundle

	- Move to Build Tab
		- Make sure output path matches the output folder from the AB export panel
		
		- Build target set to Standalone Windows 64
		
		- open advanced settings
			- standard compression is fine
			- all other settings unchecked
			
		- hit build
		
	- Hit post-process asset bundles
	
	- Generate ModCFG file
	
	
	
/////////////// Part 2: Configuring Part Info ////////////////////////

- Part script inspector panel
	- hit process button
		- should automatically fill out name, fob file and asset path fields
		
	- fill out fields:
		- part title
		- manufacturer
		- shortname
		
	- hit save cfg button
		- will auto fill out other fields and module references
		
	- add parts list data tags
		- examples from other parts most useful

- Part Physics Component
	- auto-calculate context options
		- part needs a collider for it to work
	
- Part Icon Fitter
	- auto-fit context option
	
- Remember to apply the prefab
	- select root part object
		- expand prefab overrides, hit apply all
		
- Hit Save Cfg button 
		
		
		
////////// GENERAL TIPS ///////////////

- You don't need to re-export the part prefab (pre-process, build, post-process) if you just made changes to the part's inspector values. You can just re-save the cfg file.
	- note that this doesn't apply if you change values in components attached to any child objects of the part (only for the components attached to the part's root gameobject)
	

		
		
//////////////// /////////////////////
