using CfgFields;

using Scenarios;
using Scenarios.Data;

using System;
using System.Collections.Generic;

using UI.MMX.Data;


using UnityEngine.Networking;

namespace BalsaPlugin
{

	/*  You can also create new Scenario Modules in Plugins. 
	 *  This is an example implementation of a basic scenario module.
	 */

	[ScenarioModule(listName = "My Scenario Module")]
	public class ModScenarioModule : Scenarios.ScenarioModule
	{


		// --------- Overview of Scenario Module architecture ------------
		//
		// Scenario Modules are designed so as to separate the module's Data from the actual logic (monobehaviour) component.
		// ScenarioModule subclasses are the component, which actually executes the module's behaviour.
		// However, ScenarioModule instances only exist while the mission stage in which they are defined is active and running. 
		// This means the module instances don't contain any persistent data themselves. Only their Data does.

		// Module instances always have a non-null reference to the Data that created them, so modules can (and should, as much as possible in practive)
		// read data directly from the ModuleData object, in order for changes made in the Scenario Editor to have an immediate effect, without having to restart the mission.


		// There are two ways to bind a data type to a scenario module type.
		// the quickest way is to simply declare your data class inside the module class, and call it 'Data'
		// nested classes called Data inside ScenarioModules will automatically get bound to the module.

		// Alternatively, you can also just declare your data class anywhere and use the 'dataType' parameter in the ScenarioModule attribute to bind it.
		// just remember Data types must always inherit from ScenarioModuleData



		public class Data : ScenarioModuleData
		{
			//### subclasses of ScenarioModuleData should contain all the data needed to initialize the behaviour.
			// You can use CFGFields here to automatically mark data as persistent:

			// unlike with PartModules, the CfgContext value has no effect here (which is to say, all CfgFields save under the Config context when mission files are saved)

			[CfgField]
			public int aPersistentValue = 0;

			// cfgfield supports most common types (bool, int, float, double, string, Vector2/3/4, Quaternions, etc)
			[CfgField]
			public bool anotherValue = false;

			// keep in mind that reference types need to be non-null for CfgFields to work, so it's best to always initialize them with some default value.
			// (default constructors will do.)
			[CfgField]
			public TestDataType aPersistentObject = new TestDataType();


			[CfgListField]
			public List<TestDataType> aListOfPersistentData = new List<TestDataType>();


			//### Scenario module triggers are also defined in Data, so they can be shown in the ScenarioEditor.
			// You can declare triggers and trigger listeners on your data types, as shown below.
			// In this example, we'll implement a very simple trigger 'forwarding' behaviour, where an input trigger will cause an output trigger to be sent out

			/// <summary>
			/// ScnTriggerReferences store a link to triggers from other bits of mission data. These need to be marked with CfgField to make them persistent.
			/// </summary>
			[CfgField]
			public ScnTriggerInput myInputTrigger = new ScnTriggerInput();

			/// <summary>
			/// ScnTriggers are trigger 'sources'. These are invoked from this module. These don't need to use CfgField, as they don't save any data themselves.
			/// </summary>
			public ScnTrigger myOutputTrigger = new ScnTrigger("OnModulePokedAt");





			// ScenarioModuleData automatically saves and loads CfgField members, but you can override the Save/Load methods if you need to.
			public override void Save(ConfigNode node, CfgContext context)
			{
				base.Save(node, context);
			}




			//### The Scenario Data class is also where you can define the controls that are presented in the Scenario Editor UI for this Module
			protected override void OnGetScenarioEditorData(ContentData content)
			{
				content.AddRange(new MMXItemData[]
				{
					// you can add your own UI controls here to allow UI control over your module's values:
					new Button()
					{
						caption = "A Button",
						OnClicked = (i) =>
						{
							// when clicked, this gets called.
							
							// and just for the sake of this demo, this gets toggled.
							anotherValue =!anotherValue;


							// you can also ask the UI to redraw when you need it to update.
							content.NeedRebuild();
						},

						// this expression only gets evaluated when the UI is building, so calling content.NeedRebuild above is needed in order for this to update on clicking
						tooltip = !anotherValue ? "Yep, it's a button" : "Still a button.",
					},

				

					// you can also control the layouting of your controls by using layout groups. 
					new TableLayout(0.1f, 0.9f),

						new Label("This"),
						new Toggle()
						{
							caption = "Is a toggle button",
							initialState = this.aPersistentValue > 0,
							OnStateChanged = (st, i) =>
							{
								this.aPersistentValue = st ? 100 : 0;
																

								// these could be methods instead of lambda expressions, but you gotta love C# closures!
							},
						},

					// just remember to close any layout groups you open with EndLayout
					new EndLayout(),
				});


				// the base method takes care of drawing the type's outgoing triggers. By convention, that should go at the bottom, so call it last.
				base.OnGetScenarioEditorData(content);
			}


			// if your module stores paths to any external files, you should override this method and add it as a MissionExternalAsset to the list.
			// this is used when publishing mods, to pack up any external assets that can't be resolved to a game asset (or another mod's asset), so it gets uploaded with the mission files.
			protected override void OnGatherExternalAssets(List<MissionExternalAsset> exts)
			{
				// no need to call base here. 

				exts.Add(new MissionExternalAsset(aPersistentObject.aPersistentString_CouldBeAFilePath, (str) =>
				{
					// if the packing process for the mission does end up moving files around, just make sure to update your original value here.
					aPersistentObject.aPersistentString_CouldBeAFilePath = str;
				}));
			}
		}




		Data data;

		// ScenarioModules are NetworkedBehaviours, so you can use SyncVars here to automatically update clients when the value on the server changes.
		[SyncVar]
		public int aNetworkedValue;



		protected override void OnScenarioStarting(ScenarioModuleData mData, bool asServer)
		{
			// you will almost always want to cast the received mData to your Data type here, like this
			data = mData as Data;

			// data should always be non-null, but just in case, C#'s null coalescing operators are THE greatest thing since sliced bread.
			if (data?.enabled ?? false)
			{
				// here is also a great place to initialize any triggers on your data.
				// calling InitTrigger takes care of all the listener management, you just need to provide a handler to execute when the trigger comes in.
				// (If you want to fall back to OnStageStart if no trigger source is hooked up, use data.myInputTrigger.InitTriggerOrFallback instead)

				data.myInputTrigger.InitTrigger(mData, OnInputTrigger);


				if (asServer)
				{
					// Mind that SyncVars update when the _server_ value changes, not when the client-with-authority does. For uploading values from the authoritative client to the server, use [Command] methods.
					aNetworkedValue = data.aPersistentValue;



					// when working with filepaths (as shown above), you can use this method to resolve the path to a full one.					
					string fullPath = MissionPackingUtil.ResolveMissionAssetPath(mData.mission, data.aPersistentObject.aPersistentString_CouldBeAFilePath);
				}
			}
		}

		private void OnInputTrigger()
		{
			// trigger listener methods receive a parameter to let you know whether this instance is running on the server.
			// this way you can have your module do --or not do-- things on remote clients.

			// to make this module 'forward' the trigger event, let's just invoke our output trigger directly from here.
			// it's good practive to always use ?. to access the Invoke method, because it may or may not be connected to anything on the other end.
			data.myOutputTrigger?.Invoke();
		}


		// Scenario Modules can also use unity's usual Update, LateUpdate and FixedUpdate methods.
		public void Update()
		{
			// this runs every frame.
		}



		// remember that ScenarioModules are also based on MonoBehaviours, so your filename here needs to match the class name.
	}


	public class TestDataType : IConfigNodeAdv
	{
		/* You can use CfgField with your own types by implementing one of the IConfigNode, IConfigNodeAdv or IConfigValue interfaces.
		 * You can even define additional CfgFields inside them. Just save them using the CfgFieldsUtil.Save/Load methods, like shown below: 
		 */


		// strings are also reference types in C#, so initialize them to empty at least.
		[CfgField]
		public string aPersistentString_CouldBeAFilePath = string.Empty;


		// Use the CfgFieldUtil methods to autoamtically save and load any CfgField attributed fields in your own types. 
		// You can also save and load values 'manually'
		public void Save(ConfigNode node, CfgContext context) => CfgFieldUtil.SaveCfgFields(node, this, context);
		public void Load(ConfigNode node, CfgContext context) => CfgFieldUtil.TryLoadCfgFields(node, this, context);
	}




}
