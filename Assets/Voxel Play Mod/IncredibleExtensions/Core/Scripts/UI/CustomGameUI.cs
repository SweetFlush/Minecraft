using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoxelPlay;
using UnityEditor.EditorTools;


namespace IncredibleExtensions.VPAddons{
public partial class CustomGameUI : VoxelPlayUI {
								
		/// <summary>
		/// Returns true if the console is visible
		/// </summary>
		public override bool IsConsoleVisible {
			get {
				if (console != null) {
					return console.activeSelf;
				}
				return false;
			}
		}

		public void Test(){

		}

		/// <summary>
		/// NEW: 
		/// Returns true if the hotbar is visible`
		/// OLD: Returns true if the inventory is visible
		/// </summary>
		public override bool IsInventoryVisible {
			get {
				if (inventoryWithCrafting != null) {
					return inventoryWithCrafting.activeSelf;
				}
				return false;
			}
		}

		#if HAS_CHEST_ADDON
		/// <summary>
		/// NEW: 
		/// Returns true if the hotbar is visible
		/// OLD: Returns true if the inventory is visible
		/// </summary>
		public bool IsChestInventoryOpen {
			get {
				if (chestUI != null) {
					return chestUI.activeSelf;
				}
				return false;
			}
		}
		#endif


		/// <summary>
		/// Returns true if the inventory (with crafting system) is visible
		// /// </summary>
		//  public override bool IsNewInventoryVisible {
		// 	get {
		// 		if (newInventoryPlaceholder != null) {
		// 			return newInventoryPlaceholder.activeSelf;
		// 		}
		// 		return false;
		// 	}
		// }


		[SerializeField]
		int _inventoryRows = 10;

		public virtual int inventoryRows {
			get { return _inventoryRows; }
			set {
				if (_inventoryRows != value) {
					_inventoryRows = Mathf.Clamp (value, 1, 10);
				}
			}
		}

		[SerializeField]
		int _inventoryColumns = 3;

		public virtual int inventoryColumns {
			get { return _inventoryColumns; }
			set {
				if (_inventoryColumns != value) {
					_inventoryColumns = Mathf.Clamp (value, 1, 10);
				}
			}
		}

		[SerializeField][Tooltip("Maxed at 15 on the code. You can change it by changing the Mathf.Clamp in hotbarSlots ")]
		int _hotbarSlots=9;
		public virtual int hotbarSlots {
			get { return _hotbarSlots; }
			set {
				if (_hotbarSlots != value) {
					_hotbarSlots = Mathf.Clamp (value, 1, 15); //Change the '15' if you want more slots.
				}
			}
		}

        [SerializeField]
        bool _showSelectedItemName = true;
        public virtual bool showSelectedItemName {
            get { return _showSelectedItemName; }
            set {
                if (_showSelectedItemName != value) {
                    _showSelectedItemName = value;
                    ToggleSelectedItemName();
                }
            }
        }

        [SerializeField]
        bool _showItemQuantity = true;
        public virtual bool showItemQuantity {
            get { return _showItemQuantity; }
            set {
                if (_showItemQuantity != value) {
                    _showItemQuantity = value;
                    RefreshInventoryContents();
                }
            }
        }

        [NonSerialized]
        public VoxelPlayEnvironment env;

        [NonSerialized]
        public bool inventoryUIShouldBeRebuilt;


        static readonly char[] SEPARATOR_SPACE = { ' ' };
        // readonly string KEY_CODES = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        readonly string KEY_CODES = "1234567890";

		StringBuilder sb, sbDebug;
		GameObject console, status, debug;
		RawImage selectedItem;
		GameObject selectedItemPlaceholder;
		Text consoleText, debugText, statusText, selectedItemName, selectedItemNameShadow, selectedItemQuantityShadow, selectedItemQuantity, inventoryTitleText, fpsText, fpsShadow, initText;
		GameObject hotBar, inventoryItemTemplate, inventoryTitle, initPanel,baseInventory,pauseMenu;
		//added for the crafting system
        GameObject inventoryWithCrafting;
		Transform initProgress;
		RectTransform rtCanvas;
		string lastCommand;

        InputField inputField;
        bool firstTimeInventory;
        bool firstTimeConsole;
        readonly char[] forbiddenCharacters = { '<', '>' };
		List<GameObject> inventoryItems;
		List<RawImage> inventoryItemsImages;
		int inventoryCurrentPage;
		Image statusBackground;
        int columnToShow;

		bool IsGamePaused{get{return pauseMenu.activeSelf;}}


		float fpsUpdateInterval = 0.5f;

        // FPS accumulated over the interval
        float fpsAccum;

        // Frames drawn over the interval
        int fpsFrames;

        // Left time for current interval
        float fpsTimeleft;

		[SerializeField]
		InventorySwapManager inventorySwapManager;



		#region InitGeneralAddons

		void OnEnable()
		{
			EnableAddons();
			if(inventorySwapManager==null)
				inventorySwapManager=FindObjectOfType<InventorySwapManager>();
			InventorySwapManager.OnItemSwappedEvent.AddListener(OnItemSwapped);
		}

		void OnDisable()
		{
			DisableAddons();
			InventorySwapManager.OnItemSwappedEvent.RemoveListener(OnItemSwapped);
			inventorySwapManager.ClearSelection();

		}

		void Start()
		{
			inventorySwapManager=FindObjectOfType<InventorySwapManager>();
			OnStartAddons();
		}

		#endregion
		partial void CheckChestReferences();
		partial void CheckCraftingReferences();

        public override void InitUI() {
            firstTimeConsole = true;
            inventoryCurrentPage = 0;
            lastCommand = "";
            CheckReferences();
            fpsTimeleft = fpsUpdateInterval;
            fpsFrames = 1000;
        }

		void CheckReferences () {
			if (env == null) {
				env = VoxelPlayEnvironment.instance;
			}

			sb = new StringBuilder (1000);
			sbDebug = new StringBuilder (1000);

			CheckEventSystem ();
			rtCanvas = GetComponent<RectTransform> ();
			selectedItemPlaceholder = transform.Find ("ItemPlaceholder").gameObject;
			selectedItem = selectedItemPlaceholder.transform.Find ("ItemImage").GetComponent<RawImage> ();
			selectedItemName = selectedItemPlaceholder.transform.Find ("ItemName").GetComponent<Text> ();
			selectedItemNameShadow = selectedItemPlaceholder.transform.Find ("ItemNameShadow").GetComponent<Text> ();
			selectedItemQuantity = selectedItemPlaceholder.transform.Find ("QuantityShadow/QuantityText").GetComponent<Text> ();
			selectedItemQuantityShadow = selectedItemPlaceholder.transform.Find ("QuantityShadow").GetComponent<Text> ();
			fpsShadow = transform.Find ("FPSShadow").GetComponent<Text> ();
			fpsText = fpsShadow.transform.Find ("FPSText").GetComponent<Text> ();
			fpsShadow.gameObject.SetActive (env.showFPS);
			console = transform.Find ("Console").gameObject;
			console.GetComponent<Image> ().color = env.consoleBackgroundColor;
			consoleText = transform.Find ("Console/Scroll View/Viewport/ConsoleText").GetComponent<Text> ();
			status = transform.Find ("Status").gameObject;
			statusBackground = status.GetComponent<Image> ();
			statusBackground.color = env.statusBarBackgroundColor;
			statusText = transform.Find ("Status/StatusText").GetComponent<Text> ();
			debug = transform.Find ("Debug").gameObject;
			debug.GetComponent<Image> ().color = env.consoleBackgroundColor;
			debugText = transform.Find ("Debug/Scroll View/Viewport/DebugText").GetComponent<Text> ();
			inputField = transform.Find ("Status/InputField").GetComponent<InputField> ();
			inputField.onEndEdit.AddListener (delegate {
				UserConsoleCommandHandler ();
			});
			hotBar = transform.Find ("HotBar").gameObject;
			baseInventory = transform.Find ("BaseInventory").gameObject;
			inventoryUIShouldBeRebuilt = true;
			initPanel = transform.Find ("InitPanel").gameObject;
			initProgress = initPanel.transform.Find ("Box/Progress").transform;
			initText = initPanel.transform.Find ("StatusText").GetComponent<Text> ();

			pauseMenu = transform.Find ("PauseMenu").gameObject;
			
		
			CheckChestReferences();
			CheckCraftingReferences();

			inventoryItemTemplate = baseInventory.transform.Find("ItemButtonTemplate").gameObject;
			inventoryTitle = baseInventory.transform.Find("Title").gameObject;
			inventoryTitleText = baseInventory.transform.Find("Title/Text").GetComponent<Text>();

            inventoryUIShouldBeRebuilt = true;
            initPanel = transform.Find("InitPanel").gameObject;
            initProgress = initPanel.transform.Find("Box/Progress").transform;
            initText = initPanel.transform.Find("StatusText").GetComponent<Text>();

			//Added: We make sure to toggle the hotbar (the basic inventory is now just the hotbar)
			Invoke("InitHotBar",0.1f);
		}

		void InitHotBar(){
			ToggleHotBar(true);
		}

        void OnDestroy() {
            if (inputField != null) {
                inputField.onEndEdit.RemoveAllListeners();
            }
        }

        void LateUpdate () {
            LateUpdateImpl ();
        }

        protected virtual void LateUpdateImpl() {
			//Should return if it's paused aswell.
            if (env == null) return;
            VoxelPlayInputController input = env.input;
            if (input == null || hotBar == null)
                return;
			
			#if HAS_SURVIVAL_ADDON
			if(isDeathScreenActive) return; //if you have the survival addon, and the player is dead, return
			#endif
            if (input.anyKey) {

                if (env.enableConsole && input.GetButtonDown(InputButtonNames.Console)) {
					Debug.Log("Console pressed! Current state of console: "+ console.activeSelf);
                    ToggleConsoleVisibility(!console.activeSelf);
                } else if (env.enableDebugWindow && input.GetButtonDown(InputButtonNames.DebugWindow)) {
                    ToggleDebugWindow(!debug.activeSelf);
                } else if (input.GetButtonDown(InputButtonNames.Escape)) {
                    if (IsConsoleVisible) {
                        ToggleConsoleVisibility(false);
                    }
					if(!TryToggleCustomInventory()){
						ForceCloseAllMenus();
					} else{
						ToggleHotBar(false);
						PauseGame();

					}
					//Open the pause menu
					// PauseGame();

					
                }else if(IsGamePaused){ 
					return;

				
				//Built In inventory system
				//  else if (env.enableInventory && input.GetButtonDown(InputButtonNames.Inventory)) {
                //     if (!inventoryPlaceholder.activeSelf) {
                //         ToggleInventoryVisibility(true);
				// 	} else if (Input.GetKey (KeyCode.LeftShift)) {
				// 		InventoryPreviousPage ();
                //     } else {
                //         InventoryNextPage();
                //     }

				//Custom inventory system
                } else if (env.enableInventory && input.GetButtonDown(InputButtonNames.Inventory)) {
                    // bool shouldActive = !baseInventory.activeSelf; //Must remove and replace with the TryToggleCustomInventory 
                    bool shouldActive = TryToggleCustomInventory(); //Must remove and replace with the TryToggleCustomInventory 
					if(TryToggleCustomInventory()){ //This will check if any other UI should be disabled instead of activating the Inventory. If nothing is detected, the UI opens normally.
						ForceCloseAllMenus();
						ToggleCustomInventoryVisibility(shouldActive);
					}
					// else if (Input.GetKey(KeyCode.LeftShift)) { //Test to swap pages when in build mode, but the selection of items is weird because of the swap manager. 
					// 	InventoryNextPage ();						//Should find a new way to select items in build mode.
					// }
					else{
						ForceCloseAllMenus();
						ToggleCustomInventoryVisibility(false);
					}
				
				}	
                 else if (Input.GetKeyDown(KeyCode.UpArrow) && IsConsoleVisible) {
                    inputField.text = lastCommand;
                    inputField.MoveTextEnd(false);
                } else if (Input.GetKeyDown(KeyCode.F7)) {
                    ToggleUI();
                } else if (Input.GetKeyDown(KeyCode.F8)) {
                    ToggleFPS();
                } else if (hotBar.activeSelf) {
                    for (int k = 0; k < KEY_CODES.Length; k++) {
                        if (Input.GetKeyDown(KEY_CODES.Substring(k, 1).ToLower())) {
                            SelectItemFromVisibleInventorySlot(k);
                            // ToggleInventoryVisibility(false);
                        }
                    }
                }
            }

			if (debug.activeSelf) {
				UpdateDebugInfo ();
			}

			if (fpsText.enabled) {
				UpdateFPSCounter ();
			}

		}


		void CheckEventSystem () {
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
			if (eventSystem == null) {
				GameObject prefab = Resources.Load<GameObject> ("VoxelPlay/Prefabs/EventSystem");
				if (prefab != null) {
					GameObject go = Instantiate (prefab) as GameObject;
					go.name = "EventSystem";
				}
			}
		}

		void EnableCursor (bool state) {
			if (env.initialized) {
				VoxelPlayFirstPersonController controller = VoxelPlayFirstPersonController.instance;
				if (controller != null) {
					controller.mouseLook.SetCursorLock (!state);
					controller.enabled = !state;
				}
			}
		}

		#region Console

		void PrintKeySheet () {
			if (sb.Length > 0) {
				sb.AppendLine ();
				sb.AppendLine ();
			}
			sb.AppendLine ("<color=orange>** KEY LIST **</color><");
			AppendValue ("W/A/S/D");
			sb.AppendLine (" : Move player (front/left/back/right)");
			AppendValue ("F");
			sb.AppendLine (" : Toggle Flight Mode");
			AppendValue ("Q/E");
			sb.AppendLine (" : Fly up / down");
			AppendValue ("C");
			sb.AppendLine (" : Toggles crouching");
			AppendValue ("Left Shift");
			sb.AppendLine (" : Hold while move to run / fly faster");
			AppendValue ("T");
			sb.AppendLine (" : Interacts with an object");
			AppendValue ("G");
			sb.AppendLine (" : Throws currently selected item");
			AppendValue ("L");
			sb.AppendLine (" : Toggles character light");
			AppendValue ("Mouse Move");
			sb.AppendLine (" : Look around");
			AppendValue ("Mouse Left Button");
			sb.AppendLine (" : Fire / hit blocks");
			AppendValue ("Mouse Right Button");
			sb.AppendLine (" : Build blocks");
			AppendValue ("Tab");
			sb.AppendLine (" : Show inventory and browse items (Tab / Shift-Tab)");
			AppendValue ("Esc");
			sb.AppendLine (" : Closes all windows (inventory, console)");
			AppendValue ("B");
			sb.AppendLine (" : Activate Build mode");
			AppendValue ("F1");
			sb.AppendLine (" : Show / hide console");
			AppendValue ("F2");
			sb.AppendLine (" : Show / hide debug window");
			AppendValue ("Control + F3");
			sb.Append (" : Load Game / ");
			AppendValue ("Control + F4");
			sb.AppendLine (" : Quick save");
			AppendValue ("F8");
			sb.Append (" : Toggle FPS");
			consoleText.text = sb.ToString ();
		}

		void PrintCommands () {
			if (sb.Length > 0) {
				sb.AppendLine ();
				sb.AppendLine ();
			}											
			sb.AppendLine ("<color=orange>** COMMAND LIST **</color>");
			AppendValue ("/help");
			sb.AppendLine (" : Show this list of commands");
			AppendValue ("/keys");
			sb.AppendLine (" : Show available keys and actions");
			AppendValue ("/clear");
			sb.AppendLine (" : Clear the console");
			AppendValue ("/time hh:mm");
			sb.AppendLine (" : Sets time of day in 23:59 hour format");
			AppendValue ("/debug");
			sb.AppendLine (" : Shows debug info about the last voxel hit");
			sb.Append ("Press <color=yellow>F1</color> again or <color=yellow>ESC</color> to return to game.");

			consoleText.text = sb.ToString ();
		}

		void AppendValue (object o) {
			sb.Append ("<color=yellow>");
			sb.Append (o);
			sb.Append ("</color>");
		}


		/// <summary>
		/// Shows/hides the console
		/// </summary>
		/// <param name="state">If set to <c>true</c> state.</param>
		public override void ToggleConsoleVisibility (bool state) {
			if (!env.applicationIsPlaying)
				return;
			Debug.Log("Console button clicked. Should set the console to "+ state);
            if (IsInventoryVisible) {

				Debug.Log("Disabling inventory");
					ToggleInventoryVisibility(false);
			}

			if (statusText == null) {
				CheckReferences ();
				if (statusText == null)
					return;
			}
			Debug.Log("Check1");

			if (firstTimeConsole) {
				firstTimeConsole = false;
				AddConsoleText ("<color=green>Enter <color=yellow>/help</color> for a list of commands.</color>");
			}
			status.SetActive (state);
			console.SetActive (state);
			consoleText.fontSize = statusText.fontSize;

			EnableCursor (state);

			if (state) {
				// ToggleInventoryVisibility (false);
				statusText.text = "";
				FocusInputField ();
			}

			VoxelPlayEnvironment.instance.input.enabled = !state;
		}

		/// <summary>
		/// Adds a custom text to the console
		/// </summary>
		public override void AddConsoleText (string text) {
			if (sb == null || consoleText == null || !env.enableStatusBar)
				return;
			if (sb.Length > 0) {
				sb.AppendLine ();
			}
			if (sb.Length > 12000) {
				sb.Length = 0;
			}
			sb.Append (text);
			consoleText.text = sb.ToString ();
		}

		/// <summary>
		/// Adds a custom message to the status bar and to the console.
		/// </summary>
		public override void AddMessage (string text, float displayTime = 4f, bool flash = true, bool openConsole = false) {
			if (!Application.isPlaying || env == null || !env.enableStatusBar)
				return;

			if (statusText == null) {
				CheckReferences ();
				if (statusText == null)
					return;
			}

			if (text != statusText.text) {
				AddConsoleText (text);

				// If console is not shown, only show this message
				if (!console.activeSelf) {
					if (openConsole) {
						ToggleConsoleVisibility (true);
					} else {
						statusText.text = text;
						status.SetActive (true);
						CancelInvoke (nameof(HideStatusText));
						Invoke (nameof(HideStatusText), displayTime);
                        if (flash && gameObject.activeInHierarchy) {
							StartCoroutine (FlashStatusText ());
						}
					}
				}

                ConsoleNewMessage (text);
			}
		}

		IEnumerator FlashStatusText () {
			if (statusBackground == null)
				yield break;
			float startTime = Time.time;
			float elapsed;
			Color startColor = new Color (0, 1.1f, 1.1f, env.statusBarBackgroundColor.a);
			do {
				elapsed = Time.time - startTime;
				if (elapsed >= 1f)
					elapsed = 1f;
				if (statusBackground == null)
					yield break;
				statusBackground.color = Color.Lerp (startColor, env.statusBarBackgroundColor, elapsed);
				yield return  new WaitForEndOfFrame ();
			} while(elapsed < 1f);
		}


		/// <summary>
		/// Hides the status bar
		/// </summary>
		public override void HideStatusText () {
			if (statusText != null) {
				statusText.text = "";
			}
			if (console != null && console.activeSelf) {
				return;
			}
			if (status != null) {
				status.SetActive (false);
			}
		}

		void UserConsoleCommandHandler () {
			if (inputField == null)
				return;
			string text = inputField.text;
			bool sanitize = false;
			for (int k = 0; k < forbiddenCharacters.Length; k++) {
				if (text.IndexOf (forbiddenCharacters [k]) >= 0) {
					sanitize = true;
					break;
				}
			}
			if (sanitize) {
				string[] temp = text.Split (forbiddenCharacters, StringSplitOptions.RemoveEmptyEntries);
				text = String.Join ("", temp);
			}

			if (!string.IsNullOrEmpty (text)) {
				lastCommand = text;
				if (!ProcessConsoleCommand (text)) {
					env.ShowMessage (text);
				}
				ConsoleNewCommand(inputField.text);
				if (inputField != null) {
					inputField.text = "";
					FocusInputField (); // avoids losing focus
				}
			}
		}

		void FocusInputField () {
			if (inputField == null)
				return;
			inputField.ActivateInputField ();
			inputField.Select ();
		}

        void AdjustWindowPositions() {
            RectTransform rt = hotBar.GetComponent<RectTransform>();
            float startY = rt.anchoredPosition.y + rt.sizeDelta.y + 4;
            if (!IsInventoryVisible) startY = 4;
            rt = status.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY);

            rt = status.GetComponent<RectTransform>();
            startY = rt.anchoredPosition.y + rt.sizeDelta.y + 4;
            rt = console.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY);
        }


		bool ProcessConsoleCommand (string command) {
			string upperCommand = command.ToUpper ();
			if (upperCommand.IndexOf ("/CLEAR") >= 0) {
				sb.Length = 0;
                consoleText.text = "";
                return true;
            }
            if (upperCommand.IndexOf("/KEYS") >= 0) {
                PrintKeySheet();
                return true;
            }
            if (upperCommand.IndexOf("/HELP") >= 0) {
                PrintCommands();
                return true;
            }
            if (upperCommand.IndexOf("/DEBUG") >= 0) {
                ToggleDebugWindow(!debug.activeSelf);
                return true;
            }
            if (upperCommand.IndexOf("/TIME ") >= 0) {
                ProcessTimeCommand(command);
                return true;
            }
            if (upperCommand.IndexOf("/FOG ") >= 0) {
                ProcessFogCommand(command);
                return true;
            }

			return false;
		}

		void ProcessInvokeCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 3) {
				string goName = args [1];
				string cmdParams = args [2];
				GameObject go = GameObject.Find (goName);
				if (go == null) {
					AddMessage ("GameObject '" + goName + "' not found.");
				} else {
					go.SendMessage (cmdParams, SendMessageOptions.DontRequireReceiver);
					ToggleConsoleVisibility (false);
				}
			}
		}

		void ProcessSaveCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				string saveFilename = args [1];
				if (!string.IsNullOrEmpty (saveFilename)) {
					env.saveFilename = args [1];
				}
			}
			env.SaveGameBinary ();
		}

		void ProcessLoadCommand (string command) {
            string[] args = command.Split(SEPARATOR_SPACE, StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				string saveFilename = args [1];
				if (!string.IsNullOrEmpty (saveFilename)) {
					env.saveFilename = args [1];
				}
			}
			// use invoke to ensure all pending UI events are processed before destroying UI, console, etc. and avoid errors with EventSystem, etc.
			Invoke (nameof(LoadGame), 0.1f);
		}

		void LoadGame() {
			if (!env.LoadGameBinary (false)) {
				AddMessage ("<color=red>Load error:</color><color=orange> Game '<color=white>" + env.saveFilename + "</color>' could not be loaded.</color>");
			}
		}

		void ProcessFloodCommand (string command) {
			string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
			if (args.Length >= 2) {
				string mode = args [1].ToUpper ();
				env.enableWaterFlood = "ON".Equals (mode);
			}
			AddMessage ("<color=green>Flood is <color=yellow>" + (env.enableWaterFlood ? "ON" : "OFF") + "</color></color>");
		}


		void ProcessTeleportCommand (string command) {
			try {
				string[] args = command.Split (SEPARATOR_SPACE, System.StringSplitOptions.RemoveEmptyEntries);
				if (args.Length >= 3) {
					float x = float.Parse (args [1]);
					float y = float.Parse (args [2]);
					float z = float.Parse (args [3]);
					env.characterController.transform.position = new Vector3 (x + 0.5f, y, z + 0.5f);
					ToggleConsoleVisibility (false);
				}
			} catch {
				AddInvalidCommandError ();
			}
		}

	
       void ProcessTimeCommand(string command) {
            try {
                string[] args = command.Split(SEPARATOR_SPACE, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length >= 2) {
                    env.SetTimeOfDay(args[1]);
                }
            } catch {
                AddInvalidCommandError();
            }
        }

        void ProcessFogCommand(string command) {
            try {
                string[] args = command.Split(SEPARATOR_SPACE, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length >= 2) {
                    env.enableFogSkyBlending = "1".Equals(args[1]);
                    env.UpdateMaterialProperties();
                }
            } catch {
                AddInvalidCommandError();
            }
        }



		void AddInvalidCommandError () {
			AddMessage ("<color=orange>Invalid command.</color>");
		}

		#endregion

		#region Inventory related

		/// <summary>
		/// Show/hide inventory
		/// </summary>
		/// <param name="state">If set to <c>true</c> visible.</param>
		public override void ToggleInventoryVisibility (bool state) {
            // ToggleHotBar(!state);
            ToggleCustomInventoryVisibility(state);

			// if (!state) {
			// 	inventoryPlaceholder.SetActive (false);
			// } else {
            //     if (IsConsoleVisible) ToggleConsoleVisibility(false);
			// 	CheckInventoryUI ();
			// 	RefreshInventoryContents ();
			// 	inventoryPlaceholder.SetActive (true);
			// 	if (firstTimeInventory) {
			// 		firstTimeInventory = false;
			// 		if (!env.isMobilePlatform) {
			// 			env.ShowMessage ("<color=green>Press <color=yellow>Number</color> to select an item, <color=yellow>Tab</color> to toggle belt.</color>" , 10);
			// 		}
					
			// 	}
			// }
			// ToggleSelectedItemName ();
            // AdjustWindowPositions();
			// env.input.enabled = true;

		}

		/// <summary>
		/// Advances to next inventory page
		/// </summary>
		public override void InventoryNextPage () {
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			if ((inventoryCurrentPage + 1) * itemsPerPage < VoxelPlayPlayer.instance.items.Count) {
				inventoryCurrentPage++;
				RefreshInventoryContents ();
			} else {
				inventoryCurrentPage = 0;
				ToggleInventoryVisibility (false);
			}
		}

		// /// <summary>
		// /// Shows previous inventory page
		// /// </summary>
		// public override void InventoryPreviousPage () {
		// 	if (inventoryCurrentPage > 0) {
		// 		inventoryCurrentPage--;
		// 	} else {
		// 		int itemsPerPage = _inventoryRows * _inventoryColumns;
		// 		inventoryCurrentPage = VoxelPlayPlayer.instance.items.Count / itemsPerPage;
		// 	}
		// 	RefreshInventoryContents ();
		// }

		/// <summary>
		/// Builds the inventory UI elements
		/// </summary>
		void CheckInventoryUI () {

            const float itemSize = 48;
            const float padding = 3;

			//temp
			int _inventoryColumns=9;
			int _inventoryRows=1;
			 //
            bool landscape = _inventoryColumns > _inventoryRows;

			
            while(_inventoryColumns * _inventoryRows > 36) {
                if (landscape) _inventoryRows--; else _inventoryColumns--;
            }

			float panelWidth = padding + _inventoryColumns * (itemSize + padding);
			float panelHeight;
			bool refit;
			do {
				refit = false;
				// panelHeight = padding + _inventoryRows * (itemSize + padding);
				panelHeight = _inventoryRows * (itemSize + padding);
				if (_inventoryRows > 3 && panelHeight * rtCanvas.localScale.y > Screen.height * 0.9f) {
					refit = true;
					inventoryUIShouldBeRebuilt = true;
					_inventoryRows--;
				}
			} while (refit); 

			if (!inventoryUIShouldBeRebuilt)
				return;
			Transform root = hotBar.transform.Find ("Root");
			if (root != null)
				DestroyImmediate (root.gameObject);
			GameObject rootGO = new GameObject ("Root");
			root = rootGO.transform;
			root.SetParent (hotBar.transform, false);


			if (inventoryItems == null)
				inventoryItems = new List<GameObject> ();
			else
				inventoryItems.Clear ();
												
			if (inventoryItemsImages == null)
				inventoryItemsImages = new List<RawImage> ();
			else
				inventoryItemsImages.Clear ();

            hotBar.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth, panelHeight);
            int i = 0;
            for (int c = 0; c < _inventoryColumns; c++) {
                float x = padding + c * (itemSize + padding);
                for (int r = 0; r < _inventoryRows; r++) {
                    // float y = padding + r * (itemSize + padding);
                    float y = itemSize/2 + padding;
                    // float y = 27 ;
                    GameObject itemButton = Instantiate(inventoryItemTemplate);
                    inventoryItems.Add(itemButton);
                    itemButton.transform.SetParent(root, false);
                    RectTransform rt = itemButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(x, panelHeight - y);
                    itemButton.SetActive(true);
                    string keyCode = i < KEY_CODES.Length ? KEY_CODES.Substring(i, 1) : "";
                    Text t = itemButton.transform.Find("KeyCodeShadow/KeyCodeText").GetComponent<Text>();
                    t.text = keyCode;
                    inventoryItemsImages.Add(itemButton.GetComponent<RawImage>());
                    int aux = i; // dummy assignation so the lambda expression takes the appropiate value and not always the last item
                    itemButton.GetComponent<Button>().onClick.AddListener(delegate () {
                        // InventoryImageClick(aux);
                    });
                    i++;
                }
            }
		}

		//Used before the SwapManager was created might be deleted
		// void InventoryImageClick (int inventoryImageIndex) {
		// 	int itemsPerPage = _inventoryRows * _inventoryColumns;
		// 	int itemIndex = inventoryCurrentPage * itemsPerPage + inventoryImageIndex;
		// 	VoxelPlayPlayer.instance.selectedItemIndex = itemIndex;
		// 	ToggleInventoryVisibility (false);
		// }

		/// <summary>
		/// Refreshs the inventory contents.
		/// </summary>
		public override void RefreshInventoryContents () {
			if (inventoryItemsImages == null || env == null)
				return;
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			int selectedItemIndex = VoxelPlayPlayer.instance.selectedItemIndex;
			List<InventoryItem> playerItems = VoxelPlayPlayer.instance.items;
			int playerItemsCount = playerItems != null ? playerItems.Count : 0;
			if (inventoryCurrentPage * itemsPerPage > playerItemsCount) {
				inventoryCurrentPage = 0;
			}
			int inventoryItemsImagesCount = inventoryItemsImages.Count;
			for (int k = 0; k < itemsPerPage; k++) {
				int itemIndex = inventoryCurrentPage * itemsPerPage + k;
				if (k >= inventoryItemsImagesCount)
					continue;
				RawImage img = inventoryItemsImages [k];
				if (img == null)
					continue;
				Text quantityShadow = img.transform.Find ("QuantityShadow").GetComponent<Text> ();
				Text quantityText = img.transform.Find ("QuantityShadow/QuantityText").GetComponent<Text> ();
                GameObject keyHint = img.transform.Find("KeyCodeShadow").gameObject;
				img.gameObject.SetActive (true);
				if (itemIndex < playerItemsCount) {
					InventoryItem inventoryItem = playerItems [itemIndex];
					if (inventoryItem.item != null) {
						img.color = inventoryItem.item.color;
						img.texture = inventoryItem.item.icon;
					} else {
						img.texture = null;
					}
					float quantity = inventoryItem.quantity;
					// show quantity if greater than 1
                    if (quantity <= 0 || env.buildMode || !_showItemQuantity) {
						quantityText.enabled = false;
						quantityShadow.enabled = false;
					} else {
						string quantityStr = String.Format ("{0:0.##}", quantity);
						quantityText.text = quantityStr;
						quantityShadow.text = quantityStr;
						quantityText.enabled = true;
						quantityShadow.enabled = true;
					}
					// Mark selected item
					img.transform.Find ("SelectedBorder").gameObject.SetActive (k + itemsPerPage * inventoryCurrentPage == selectedItemIndex);
                    keyHint.SetActive(true);
				} else {
					img.texture = Texture2D.whiteTexture;
					img.color = new Color (0, 0, 0, 0.25f);
					quantityText.enabled = false;
					quantityShadow.enabled = false;
					// Hide selected border
					img.transform.Find ("SelectedBorder").gameObject.SetActive (false);
                    keyHint.SetActive(false);
				}
			}

			if (inventoryTitle != null) {
				if (playerItemsCount == 0) {
					inventoryTitle.SetActive (true);
					inventoryTitleText.text = "Empty.";
				} else if (playerItemsCount > itemsPerPage) {
					inventoryTitle.SetActive (true);
					int totalPages = (playerItemsCount - 1) / itemsPerPage + 1;
					if (totalPages < 0)
						totalPages = 1;
					inventoryTitleText.text = "Belt " + (inventoryCurrentPage + 1) + "/" + totalPages;
				} else {
					inventoryTitle.SetActive (false);
				}
			}

		}


		void SelectItemFromVisibleInventorySlot (int itemIndex) {
			int slotIndex = itemIndex + columnToShow * _inventoryRows;
			int itemsPerPage = _inventoryRows * _inventoryColumns;
			int selectedItemIndex = inventoryCurrentPage * itemsPerPage + slotIndex;
			VoxelPlayPlayer.instance.selectedItemIndex = selectedItemIndex;
		}


		/// <summary>
		/// Updates selected item representation on screen
		/// </summary>
		public override void ShowSelectedItem (InventoryItem inventoryItem) {
			if (selectedItemPlaceholder == null || env == null || !env.enableInventory)
				return;
			ItemDefinition item = inventoryItem.item;
			selectedItem.texture = item.icon;
			selectedItem.color = item.color;
			string txt = item.title;
			if (string.IsNullOrEmpty (txt) && item.voxelType != null) {
				txt = item.voxelType.name;
			}
			selectedItemName.text = txt;
			selectedItemNameShadow.text = txt;
			selectedItemPlaceholder.SetActive (true);
			string quantity = inventoryItem.quantity.ToString ();
            bool quantityVisible = showItemQuantity && !VoxelPlayEnvironment.instance.buildMode;
			selectedItemQuantityShadow.enabled = quantityVisible;
			selectedItemQuantityShadow.text = quantity;
			selectedItemQuantity.enabled = quantityVisible;
			selectedItemQuantity.text = quantity;
			RefreshInventoryContents ();
			ToggleSelectedItemName ();
		}

		void ToggleSelectedItemName () {
            bool showItemName = _showSelectedItemName;
			selectedItemName.enabled = showItemName;
			selectedItemNameShadow.enabled = showItemName;
		}

		/// <summary>
		/// Hides selected item graphic
		/// </summary>
		public override void HideSelectedItem () {
			if (selectedItemPlaceholder == null)
				return;
			selectedItemPlaceholder.SetActive (false);
			RefreshInventoryContents ();
		}

		#endregion


		#region Custom Inventory

        bool copyingItem;
        int indexCopied=-1;
        InventoryItem inventoryItemCopied;
        // InventorySlot inventorySlotCopied;

        GameObject itemButtonToReset;

        //should toggle the hotbar

        void ToggleHotBar(bool state){
            // Debug.Log("Hotbar: "+state );
			if(IsConsoleVisible)
				hotBar.SetActive (false);

            if (!state) {
				hotBar.SetActive (false);
			} else {
                // if (IsConsoleVisible) ToggleConsoleVisibility(false);
                if (IsConsoleVisible) return;
				CheckInventoryUI ();
				RefreshInventoryContents ();
				hotBar.SetActive (true);
				if (firstTimeInventory) {
					firstTimeInventory = false;
					if (!env.isMobilePlatform) {
						env.ShowMessage ("<color=green>Press <color=yellow>Number</color> to select an item, <color=yellow>Tab</color> to toggle belt.</color>" , 10);
					}
					
				}
			}
			ToggleSelectedItemName ();
            AdjustWindowPositions();

        }

		bool TryToggleCustomInventory(){
			if(baseInventory.activeSelf) return false;
			if(IsGamePaused) return false;
			#if HAS_CHEST_ADDON
				if(IsChestInventoryOpen){
					return false;
				}
			#endif	

			#if HAS_CRAFTING_ADDON
				if(IsInventoryWithCraftingOpen){
					return false;
				}
			#endif
			#if HAS_MACHINERY_ADDON
				if(IsMachineUIOpen){
					return false;
				}
			#endif
				return true;
		}




    void ForceCloseAllMenus(){ //Close every menus, excluding the pause menu, return to the normal hotbar (gameplay) view
			// #if HAS_SURVIVAL_ADDON
			// if(isDeathScreenActive)return; //if you have the survival addon, and the player is dead, you can avoid calling this (the playe)
			// #endif
			baseInventory.SetActive(false);
			#if HAS_CHEST_ADDON
			CloseChest();
			#endif
			#if HAS_CRAFTING_ADDON
			CloseCraftingMenu();
			#endif
			#if HAS_MACHINERY_ADDON
			CloseMachineUI();
			#endif

			ToggleHotBar(true);
		}

		partial void TryForceCraftingUI();

        public void ToggleCustomInventoryVisibility(bool state) {
            // Debug.Log("Inventory: "+state );
			ToggleHotBar(!state);
            // ToggleInventoryVisibility(!state);
            if(state){ // If we should activate the inventory we make sure to do all the necessary stuff to update that before activating
                if (IsConsoleVisible) ToggleConsoleVisibility(false);
				NewCheckInventoryUI(baseInventory.transform);
                // NewCheckInventoryUI();
                InitInventoryContents();
            }
            if(!state){
                copyingItem=false;
                indexCopied=-1;
                // if(itemButtonToReset!=null){
                //     ResetItemButton();
                // }
			// #if HAS_CRAFTING_ADDON
            //     craftingSystem.Reset();
			// #endif
				if(!state){ //If we disable the inventory, we clear any selection on the swap manager.
					inventorySwapManager.ClearSelection();
				}
				if (IsConsoleVisible) ToggleConsoleVisibility(true);
            }
            // Debug.Log("Toggle new inventory");
            // inventoryWithCrafting.SetActive(state); Should check this for the crafting

            baseInventory.SetActive(state);
			if(state){
				TryForceCraftingUI();
			}
			if(state){
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
				env.input.enabled = false;
			}else
				env.input.enabled = true;
        }


        void NewCheckInventoryUI() {
            // Debug.Log("Creating item uis");
            const float itemSize = 48;
            const float padding = 3;

            bool landscape = _inventoryColumns > _inventoryRows;
            while(_inventoryColumns * _inventoryRows > 36) {
                if (landscape) _inventoryRows--; else _inventoryColumns--;
            }

            float panelWidth = padding + _inventoryColumns * (itemSize + padding);
            float panelHeight;
            bool refit;
            do {
                refit = false;
                panelHeight = padding + _inventoryRows * (itemSize + padding);
                if (_inventoryRows > 3 && panelHeight * rtCanvas.localScale.y > Screen.height * 0.9f) {
                    refit = true;
                    inventoryUIShouldBeRebuilt = true;
                    _inventoryRows--;
                }
            } while (refit);

            if (!inventoryUIShouldBeRebuilt) {
                return;
            }
			//OLD
            // Transform root = inventoryWithCrafting.transform.Find("Root");
            // if (root != null) {
            //     DestroyImmediate(root.gameObject);
            // }
            // GameObject rootGO = new GameObject("Root");
            // root = rootGO.transform;
            // root.SetParent(inventoryWithCrafting.transform, false);


            // if (inventoryItems == null)
            //     inventoryItems = new List<GameObject>();
            // else
            //     inventoryItems.Clear();

            // if (inventoryItemsImages == null)
            //     inventoryItemsImages = new List<RawImage>();
            // else
            //     inventoryItemsImages.Clear();

            // inventoryWithCrafting.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth, panelHeight);
            // int i = 0;
            // for (int c = 0; c < _inventoryColumns; c++) {
            //     float x = padding + c * (itemSize + padding);
            //     for (int r = 0; r < _inventoryRows; r++) {
            //         float y = padding + r * (itemSize + padding);
            //         GameObject itemButton = Instantiate(inventoryItemTemplate);
            //         inventoryItems.Add(itemButton);
            //         itemButton.transform.SetParent(root, false);
            //         RectTransform rt = itemButton.GetComponent<RectTransform>();
            //         rt.anchoredPosition = new Vector2(x, panelHeight * 0.5f - y);
            //         itemButton.SetActive(true);
            //         string keyCode = i < KEY_CODES.Length ? KEY_CODES.Substring(i, 1) : "";
            //         Text t = itemButton.transform.Find("KeyCodeShadow/KeyCodeText").GetComponent<Text>();
            //         t.text = keyCode;
            //         inventoryItemsImages.Add(itemButton.GetComponent<RawImage>());
            //         int aux = i; // dummy assignation so the lambda expression takes the appropiate value and not always the last item
            //         itemButton.GetComponent<Button>().onClick.AddListener(delegate () {
            //             CopyInventoryItem(aux,itemButton);
            //         });
            //         i++;
            //     }
            // }
			//End Old

			//New
			Transform root = baseInventory.transform.Find("Root");
            if (root != null) {
                DestroyImmediate(root.gameObject);
            }
            GameObject rootGO = new GameObject("Root");
            root = rootGO.transform;
            root.SetParent(baseInventory.transform, false);


            if (inventoryItems == null)
                inventoryItems = new List<GameObject>();
            else
                inventoryItems.Clear();

            if (inventoryItemsImages == null)
                inventoryItemsImages = new List<RawImage>();
            else
                inventoryItemsImages.Clear();

            baseInventory.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth, panelHeight);
            int i = 0;
            for (int c = 0; c < _inventoryColumns; c++) {
                float x = padding + c * (itemSize + padding);
                for (int r = 0; r < _inventoryRows; r++) {
                    float y = padding + r * (itemSize + padding);
                    GameObject itemButton = Instantiate(inventoryItemTemplate);
                    inventoryItems.Add(itemButton);
                    itemButton.transform.SetParent(root, false);
                    RectTransform rt = itemButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(x, panelHeight * 0.5f - y);
                    itemButton.SetActive(true);
                    string keyCode = i < KEY_CODES.Length ? KEY_CODES.Substring(i, 1) : "";
                    Text t = itemButton.transform.Find("KeyCodeShadow/KeyCodeText").GetComponent<Text>();
                    t.text = keyCode;
                    inventoryItemsImages.Add(itemButton.GetComponent<RawImage>());
                    int aux = i; // dummy assignation so the lambda expression takes the appropiate value and not always the last item
                	itemButton.GetComponent<InventorySlot>().SetInventorySlot(aux,VoxelPlayPlayer.instance as IInventoryContainer,true);
                    // itemButton.GetComponent<Button>().onClick.AddListener(delegate () {
                    //     CopyInventoryItem(aux,itemButton);
                    // });
                    i++;
                }
            }
        }
		/// <summary>
		/// Used from new Addons to instantiate the root on their own Canvas, without rewriting the code
		/// </summary>
		/// <param name="rootParentObject"></param>
		void NewCheckInventoryUI(Transform rootParentObject) {
            // Debug.Log("Creating item uis");
            const float itemSize = 48;
            const float padding = 3;

            bool landscape = _inventoryColumns > _inventoryRows;
            while(_inventoryColumns * _inventoryRows > 36) {
                if (landscape) _inventoryRows--; else _inventoryColumns--;
            }

            float panelWidth = padding + _inventoryColumns * (itemSize + padding);
            float panelHeight;
            bool refit;
            do {
                refit = false;
                panelHeight = padding + _inventoryRows * (itemSize + padding);
                if (_inventoryRows > 3 && panelHeight * rtCanvas.localScale.y > Screen.height * 0.9f) {
                    refit = true;
                    inventoryUIShouldBeRebuilt = true;
                    _inventoryRows--;
                }
            } while (refit);

            if (!inventoryUIShouldBeRebuilt) {
                return;
            }
            Transform root = rootParentObject.transform.Find("Root");
            if (root != null) {
                DestroyImmediate(root.gameObject);
            }
            GameObject rootGO = new GameObject("Root");
            root = rootGO.transform;
            root.SetParent(rootParentObject.transform, false);


            if (inventoryItems == null)
                inventoryItems = new List<GameObject>();
            else
                inventoryItems.Clear();

            if (inventoryItemsImages == null)
                inventoryItemsImages = new List<RawImage>();
            else
                inventoryItemsImages.Clear();

            rootParentObject.GetComponent<RectTransform>().sizeDelta = new Vector2(panelWidth, panelHeight);
            int i = 0;
            for (int c = 0; c < _inventoryColumns; c++) {
                float x = padding + c * (itemSize + padding);
                for (int r = 0; r < _inventoryRows; r++) {
                    float y = padding + r * (itemSize + padding);
                    GameObject itemButton = Instantiate(inventoryItemTemplate);
                    inventoryItems.Add(itemButton);
                    itemButton.transform.SetParent(root, false);
                    RectTransform rt = itemButton.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(x, panelHeight * 0.5f - y);
                    itemButton.SetActive(true);
                    string keyCode = i < KEY_CODES.Length ? KEY_CODES.Substring(i, 1) : "";
                    Text t = itemButton.transform.Find("KeyCodeShadow/KeyCodeText").GetComponent<Text>();
                    t.text = keyCode;
                    inventoryItemsImages.Add(itemButton.GetComponent<RawImage>());
                    int aux = i; // dummy assignation so the lambda expression takes the appropiate value and not always the last item
                	itemButton.GetComponent<InventorySlot>().SetInventorySlot(aux,VoxelPlayPlayer.instance as IInventoryContainer,true);

                    // itemButton.GetComponent<Button>().onClick.AddListener(delegate () {
                    //     CopyInventoryItem(aux,itemButton);
                    // });
					// var test= itemButton.GetComponent<InventorySlot>();
					// if(test!=null) Debug.Log("Test exist");
                    i++;
                }
            }
        }

		InventoryItem selectedInventoryItem;
		public InventoryItem SelectedInventoryItem{get{return selectedInventoryItem;}}
		public InventoryItem SetSelectedInventoryItem{set{selectedInventoryItem=value;}}


        void CopyInventoryItem(int inventoryImageIndex, GameObject _itemButton){
            // Debug.Log("CopyInventoryItem"+ inventoryImageIndex) ;
            
            int itemsPerPage = _inventoryRows * _inventoryColumns;
            // int itemIndex = inventoryCurrentPage * itemsPerPage + inventoryImageIndex;
            int itemIndex = _itemButton.GetComponent<InventorySlot>().PlayerInventoryIndex;
            // IVoxelPlayPlayer player = VoxelPlayPlayer.instance;
            
            if(!VoxelPlayPlayer.instance.HasItemWithIndex(inventoryImageIndex)){
                // Debug.Log("Clicking an object outside of index!");
                if(copyingItem){
					itemButtonToReset=null;

					copyingItem=false;
					selectedInventoryItem.item=null;
                }
				CopyWithSwapManager(inventoryImageIndex);
            }
            else{
                // VoxelPlayPlayer.instance.selectedItemIndex = itemIndex;
                // InventoryItem itemSelected = VoxelPlayPlayer.instance.GetSelectedItem();
                copyingItem=true;
                RefreshItemButton(_itemButton);
                itemButtonToReset=_itemButton;
                // Debug.Log("Object Copied! " + itemSelected.item.name +itemSelected.quantity );
				// selectedInventoryItem=itemSelected;
				CopyWithSwapManager(inventoryImageIndex);

            }
        }

		void CopyWithSwapManager(int targetSlotIndex){
            Debug.Log("Should select item with swap manager");
            InventorySwapManager.Instance.SelectItem(VoxelPlayPlayer.instance as IInventoryContainer,targetSlotIndex,true);
        }

        void CopyButtonToNewOne(GameObject newButton){
            
        }


        void NewInventoryImageClick(int inventoryImageIndex) {
            int itemsPerPage = _inventoryRows * _inventoryColumns;
            int itemIndex = inventoryCurrentPage * itemsPerPage + inventoryImageIndex;
            VoxelPlayPlayer.instance.selectedItemIndex = itemIndex;
            // ToggleInventoryVisibility(false);
        }

        void RefreshItemButton(GameObject _itemButton){
            if(_itemButton.GetComponent<InventorySlot>()._inventoryItem!=null){
                InventoryItem inventoryItem= _itemButton.GetComponent<InventorySlot>()._inventoryItem;
                RawImage img = _itemButton.GetComponent<RawImage>();
                
                Text quantityShadow = img.transform.Find("QuantityShadow").GetComponent<Text>();
                Text quantityText = img.transform.Find("QuantityShadow/QuantityText").GetComponent<Text>();
                GameObject keyHint = img.transform.Find("KeyCodeShadow").gameObject;
                if (inventoryItem.item != null) {
                        img.color = inventoryItem.item.color;
                        img.texture = inventoryItem.item.icon;
                    } else {
                        img.texture = null;
                    }
                    float quantity = inventoryItem.quantity;
                    // show quantity if greater than 1
                    if (quantity <= 0 || env.buildMode || !_showItemQuantity) {
                        quantityText.enabled = false;
                        quantityShadow.enabled = false;
                    } else {
                        string quantityStr = string.Format("{0:0.##}", quantity);
                        quantityText.text = quantityStr;
                        quantityShadow.text = quantityStr;
                        quantityText.enabled = true;
                        quantityShadow.enabled = true;
                    }
            }
        }


        void InitInventoryContents() {
            if (inventoryItemsImages == null || env == null)
                return;
            int itemsPerPage = _inventoryRows * _inventoryColumns;
            int selectedItemIndex = VoxelPlayPlayer.instance.selectedItemIndex;
            List<InventoryItem> playerItems = VoxelPlayPlayer.instance.items;
            int playerItemsCount = playerItems != null ? playerItems.Count : 0;
            if (inventoryCurrentPage * itemsPerPage > playerItemsCount) {
                inventoryCurrentPage = 0;
            }
            int inventoryItemsImagesCount = inventoryItemsImages.Count;
            for (int k = 0; k < itemsPerPage; k++) {
                int itemIndex = inventoryCurrentPage * itemsPerPage + k;
                //Commented for testing
                // if (k >= inventoryItemsImagesCount)
                //     continue;

                RawImage img = inventoryItemsImages[k];
                if (img == null)
                    continue;
                Text quantityShadow = img.transform.Find("QuantityShadow").GetComponent<Text>();
                Text quantityText = img.transform.Find("QuantityShadow/QuantityText").GetComponent<Text>();
                GameObject keyHint = img.transform.Find("KeyCodeShadow").gameObject;
                img.gameObject.SetActive(true);
                if (itemIndex < playerItemsCount) {
                    InventoryItem inventoryItem = playerItems[itemIndex];
                    if (inventoryItem.item != null) {
                        img.color = inventoryItem.item.color;
                        img.texture = inventoryItem.item.icon;
                        img.GetComponent<InventorySlot>()._inventoryItem=inventoryItem;
                        img.GetComponent<InventorySlot>().PlayerInventoryIndex=itemIndex;
                    } else {
                        img.texture = null;
                    }
                    float quantity = inventoryItem.quantity;
                    // show quantity if greater than 1
                    if (quantity <= 0 || env.buildMode || !_showItemQuantity) {
                        quantityText.enabled = false;
                        quantityShadow.enabled = false;
                    } else {
                        string quantityStr = string.Format("{0:0.##}", quantity);
                        quantityText.text = quantityStr;
                        quantityShadow.text = quantityStr;
                        quantityText.enabled = true;
                        quantityShadow.enabled = true;
                    }
                    // Mark selected item
                    img.transform.Find("SelectedBorder").gameObject.SetActive(k + itemsPerPage * inventoryCurrentPage == selectedItemIndex);
                    keyHint.SetActive(true);
                } else {
                    img.texture = Texture2D.whiteTexture;
                    img.color = new Color(0, 0, 0, 0.25f);
                    quantityText.enabled = false;
                    quantityShadow.enabled = false;
                    // Hide selected border
                    img.transform.Find("SelectedBorder").gameObject.SetActive(false);
                    keyHint.SetActive(false);
                }
                // Debug.Log("Finished refreshing item slot: "+img.gameObject.name);
            }

            if (inventoryTitle != null) {
                if (playerItemsCount == 0) {
                    inventoryTitle.SetActive(true);
                    inventoryTitleText.text = "Empty.";
                } else if (playerItemsCount > itemsPerPage) {
                    inventoryTitle.SetActive(true);
                    int totalPages = (playerItemsCount - 1) / itemsPerPage + 1;
                    if (totalPages < 0)
                        totalPages = 1;
                    inventoryTitleText.text = "Page " + (inventoryCurrentPage + 1) + "/" + totalPages;
                } else {
                    inventoryTitle.SetActive(false);
                }
            }
        }

        void ResetItemButton(){
            
        }

        

        #endregion


		#region Initialization Panel

		public override void ToggleInitializationPanel (bool visible, string text = "", float progress = 0) {
			if (!Application.isPlaying)
				return;

			if (initProgress == null) {
				CheckReferences ();
			}
			if (progress > 1)
				progress = 1f;
			initProgress.localScale = new Vector3 (progress, 1, 1);
			if (visible) {
				initText.text = text;
			}
			initPanel.SetActive (visible);
		}

		#endregion

		#region Debug Window

		public override void ToggleDebugWindow (bool visible) {
			debug.SetActive (visible);
		}

		void UpdateDebugInfo () {

			sbDebug.Length = 0;

			if (env.playerGameObject != null) {
				Vector3 pos = env.playerGameObject.transform.position;
				sbDebug.Append ("Player Position: X=");
				AppendValueDebug (pos.x.ToString("F2"));

				sbDebug.Append (", Y=");
				AppendValueDebug (pos.y.ToString("F2"));

                sbDebug.Append(", Z=");
                AppendValueDebug(pos.z.ToString("F2"));

				sbDebug.Append(", Angle=");
				float angle = env.playerGameObject.transform.eulerAngles.y;
				AppendValueDebug(angle.ToString("0"));
			}

			VoxelChunk currentChunk = env.GetCurrentChunk ();
			if (currentChunk != null) {

				sbDebug.AppendLine ();

                sbDebug.Append ("Current Chunk: Id=");
                AppendValueDebug (currentChunk.poolIndex);

                sbDebug.Append(", X=");
				AppendValueDebug (currentChunk.position.x);

				sbDebug.Append (", Y=");
				AppendValueDebug (currentChunk.position.y);

				sbDebug.Append (", Z=");
				AppendValueDebug (currentChunk.position.z);
			}
			VoxelChunk hitChunk = env.lastHitInfo.chunk;
			if (hitChunk != null) {
				int voxelIndex = env.lastHitInfo.voxelIndex;

				sbDebug.AppendLine ();

                sbDebug.Append ("Last Chunk Hit: Id=");
                AppendValueDebug (hitChunk.poolIndex);

                sbDebug.Append(", X=");
				AppendValueDebug (hitChunk.position.x);

				sbDebug.Append (", Y=");
				AppendValueDebug (hitChunk.position.y);

				sbDebug.Append (", Z=");
				AppendValueDebug (hitChunk.position.z);

				sbDebug.Append (", AboveTerrain=");
				AppendValueDebug (hitChunk.isAboveSurface);

				if (hitChunk.modified) {
					sbDebug.Append (" (modified)");
				}

				int px, py, pz;
				env.GetVoxelChunkCoordinates (voxelIndex, out px, out py, out pz);

				sbDebug.AppendLine ();

				sbDebug.Append ("Last Voxel Hit: X=");
				AppendValueDebug (px);

				sbDebug.Append (", Y=");
				AppendValueDebug (py);

				sbDebug.Append (", Z=");
				AppendValueDebug (pz);

				sbDebug.Append (", Index=");
				AppendValueDebug (env.lastHitInfo.voxelIndex);

				sbDebug.Append (", Light=");
				AppendValueDebug (env.lastHitInfo.voxel.lightOrTorch);

				sbDebug.Append (", Light Above=");
				AppendValueDebug (env.GetVoxel (env.lastHighlightInfo.voxelCenter + Misc.vector3up).lightOrTorch); 

				if (env.lastHitInfo.voxel.typeIndex != 0) {
					sbDebug.AppendLine ();
					sbDebug.Append ("     Voxel Type=");
					AppendValueDebug (env.lastHitInfo.voxel.type.name);

					sbDebug.Append (", Pos: X=");
					Vector3 v = env.GetVoxelPosition (hitChunk.position, px, py, pz);
					AppendValueDebug (v.x);

					sbDebug.Append (", Y=");
					AppendValueDebug (v.y);

					sbDebug.Append (", Z=");
					AppendValueDebug (v.z);
				}


			}
			debugText.text = sbDebug.ToString ();
		}

		void AppendValueDebug (object o) {
			sbDebug.Append ("<color=yellow>");
			sbDebug.Append (o);
			sbDebug.Append ("</color>");
		}

		#endregion

		#region FPS

        void ToggleUI() {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null) {
                canvas.enabled = !canvas.enabled;
            }
        }

        void ToggleFPS() {
            fpsShadow.gameObject.SetActive(!fpsShadow.gameObject.activeSelf);
        }

		void UpdateFPSCounter () {
			fpsTimeleft -= Time.deltaTime;
			fpsAccum += Time.timeScale / Time.deltaTime;
			++fpsFrames;
			if (fpsTimeleft <= 0.0) {
				if (fpsText != null && fpsShadow != null) {
					int fps = (int)(fpsAccum / fpsFrames);
					fpsText.text = fps.ToString ();
					fpsShadow.text = fpsText.text;
					if (fps < 30)
						fpsText.color = Color.yellow;
					else if (fps < 10)
						fpsText.color = Color.red;
					else
						fpsText.color = Color.green;
				}
				fpsTimeleft = fpsUpdateInterval;
				fpsAccum = 0.0f;
				fpsFrames = 0;
			}
		}


		#endregion


		///<Summary>
		/// Might be changed, and transfered to other classes of addons/core
		///</Summary>
		#region NewMethods

		public void ReplaceSlotItem(InventoryItem _newInventoryItem){
			var tempNewItem = new InventoryItem();
			tempNewItem.item= _newInventoryItem.item;
			tempNewItem.quantity= _newInventoryItem.quantity;

			_newInventoryItem.item=selectedInventoryItem.item;
			_newInventoryItem.quantity=selectedInventoryItem.quantity;

			selectedInventoryItem.item=tempNewItem.item;
			selectedInventoryItem.quantity=tempNewItem.quantity;

			NewCheckInventoryUI();
        }

		


		void OnItemSwapped(ItemSwapEventData itemSwapEventData){
			
			NewCheckInventoryUI();
			InitInventoryContents();
		}
		public void PauseGame(){
			pauseMenu.SetActive(true);
			InputHandler.DisableAllInputs();
			Time.timeScale=0f; //Set the timescale at 0 so the game stops
		}

		public void ResumeGame(){
			pauseMenu.SetActive(false);
			InputHandler.EnableAllInputs();
			ToggleHotBar(true);
			Time.timeScale=1f; //Resume the game
		}

		public void QuitGame(){
			Debug.Log("Should quit to menu");
		}




		#endregion
	
	}


}