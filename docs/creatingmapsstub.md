:warning: This page is only a stub
  
  
    
   -------------
  
  # Setting up Map Boundaries 
  
   
   Part one: Create a template for the map texture

	Set scene view to top-down, orthographic.

	Take screenshot, make sure all accessible areas are visible in a square frame.
	(it's also a good idea to make sure the screenshot has a few registration marks visible, for later. scene gizmos tend to work well)


	Create new image in photoshop (512x512 map or larger if necessary).

	Paste screenshot, make sure all accessible areas fit inside the canvas. (anything outside will be considered out of bounds)
	When resizing the pasted screenshot, try to always maintain the image aspect ratio fixed (hold shift when resizing). This will make alignment easier later as the map will be square.

	Set up a solid black layer, make it the bottom layer.

	Set up three more solid color layers, for full red, green and blue.

	Set these three color layers to Linear Dodge (add) blending.

	Make the mask on these layers fully black.



Part two: Set up the map for positioning and scaling.

	The boundaries texture map needs to be set up in the scene, so the game can know how to convert an object's world position into texture coordinates for sampling. This is done using the Scn_MapBoundaries component in the Balsa.Core assembly.
	
	
	First, export your map template image and import it into the scenery project. Make sure the template layer is visible before exporting.
	
	When importing, make sure to tick the texture's read/write checkbox, and set its wrapping more to Clamped, Mipmaps disabled, filtering Bilinear. 
	
	Next, Create an empty game object in the scene, make sure it is placed at the scene origin 
	(right click the transform component, hit Reset to zero it out).
	
	Add the Scn_MapBoundaries component to it. 
	
	Assign the imported template map to the Map field of the component.
	
	
	
	The Map boundaries component has a few tools to make aligning the boundaries map easier:
	
	Hit the 'Create Preview Plane' button. This will spawn in a quad mesh, with the map image already assigned. This quad will be added as a child object of the scn_boundaries gameobject.
		
		
	You can now use the move and scale tools to adjust the placement and size of this quad, so it matches the scene.
	The registration marks from the screenshot should be useful here to assist with alignment. 	(Exact placement isn't critical unless your map requires very exact boundaries for some reason)
	To make alignment easier, the preview quad is set up to be slightly transparent by default.
	
	(make sure you have the quad selected when aligning it, not its parent scn_boundaries object)
	
	Rotating the preview plane is not allowed. It will mess up the map's coordinate frame. If your image appears rotated, rotate the map in photoshop and resave.	
	The Y position of the preview plane is irrelevant. You can use it as needed to make alignment easier.
	Alignment is easiest with scene view set to orthographic and aligned to look straight down.
	
	Once you are happy with the positioning of your map, select the quad's parent again (where you have Scn_MapBoundaries attached), and hit the 'Update from Preview Plane' button.
	This will update the MapSize and MapOffset parameters of Scn_MapBoundaries to match the size and location of the quad.
	
	The preview plane mesh is not used during gameplay for map sampling. 
	You can delete it after you are done positioning the map, but keep it enabled for now if you are following this guide.

	If you need to make changes to the map positioning after having deleted the preview plane, you can always press 'Create Preview Plane' again to spawn a new one. Repeat the steps above as needed.

	




Part Three: Paint boundary areas for players and vehicles

	You can now return to photoshop and start painting your map boundaries. The colour layers you set up earlier are set to blend in such a way that each of them is directly controlling 
	an individual colour channel (RGB) on the exported map, without interfering with the others.
	
	
	The boundaries system has two levels of enforcement: Warning and Moving.
	The level of boundary enforcement is determined by the intensity of the colours on the boundaries map. 


	The boundaries map defines where players and vehicles are not allowed to go. By default, the red channel defines player boundaries, and the blue channel controls vehicle boundaries. Green at the moment is unused.
	You can now start painting the map boundaries. The easiest way to do this is to select the layer masks of the colour layers, and paint the masks using grayscale values. This allows you to easily tweak the value of individual channels using standard brushes.

	
	For player boundaries (red channel), areas where the red colour value is higher than 10% will cause a warning to appear on screen whenever the player steps there. 
	Areas where the red intensity is above 90% will trigger the Move level of enforcement, causing the player to be automatically relocated back to a good within-bounds position.
	Areas with no red at all are considered to be within bounds, so no action is taken.


	For vehicles, map boundaries are also driven by checking its position against the map, but they are enforced differently, as a signal strength degradation. 
	The blue channel on the map represents the amount of signal degradation for vehicles. Areas with 10% or more blue colour values will cause the signal degradation effect to start becoming visible.
	Areas where blue intensity is 90% or more will cause vehicles to lose signal.
	Areas with no blue at all are considered to be within bounds, so no signal degradation is used.

	Additional Tips: 
		For vehicle boundaries, it is generally a good idea to paint the blue layer using smooth gradients, so there is a gradual increase in signal degradation as a vehicle approaches the map boundaries.

		For both players and vehicles, it is generally recommended that all edges of the map be painted with full intensity.
	
		A helpful tip for painting layer masks in Photoshop: if you Alt+click on a layer mask in the layers panel, photoshop will display the mask image on canvas. Alt+click again to return to normal view.
	

Part Four: Exporting your painted boundaries map
	
	Once you are done painting your boundaries, you need to export your map again into the project.
	
	This time however, make sure the template layer is disabled, so the image consists only of the black background and the three colour layers. (the template layer is only used for alignment)
	Don't delete the template layer though, as it will still be useful later if you ever need to make adjustments to the map. 

	When exporting, you should overwrite the template map you created earlier. Otherwise, you'll have to reconfigure the new image import settings, and replace the texture reference in the Scn_MapBoundaries component.
	
	After exporting, return to unity. If you didn't delete your preview plane earlier, you should see the newly exported boundaries map overlaid on top of your scene.
	You can now disable or clear the preview quad. You can always create it again through the Scn_MapBoundaries component if you need it. 

	
	
Part Five: Scn_MapBoundaries configuration

	The Scn_MapBoundaries component is pre-configured by default, but it has a few user-configurable parameters. These are explained here:
	
	Map: points to the boundaries map texture asset.
	
	Map Size: controls the world size of the map (usually this is automatically set by placing the preview plane)
	Map Offset: controls the location of the map's bottom-left corner. (also normally set using the preview plane)
	
	Preview Quad: Reference to the preview quad object, if it exists. Can be left null.
	
	Player/Vehicle Thresholds: Contain values that determine the threshold levels for boundary enforcement. 
		Warn: Values above Warn will trigger a warning message (or signal loss effect for vehicles). 
		Move: Values above Move will trigger the player or vehicle being relocated back to a valid position (in case of vehicles, signal is lost and the vehicle is reset back to the player)
		ColorMask: Determines which colour channel on the map is used for player or vehicle boundaries. By cnvention, players should use the red channel and vehicles should use blue.
					(it is possible to use multiple channels. In such cases, the sampled value will be the average of the selected channels)
		

