using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelPlay {


	public enum InputButtonNames {
		Button1,
		Button2,
		Jump,
		Up,
		Down,
		LeftControl,
		LeftShift,
		LeftAlt,
		Build,
		Fly,
		Crouch,
		Inventory,
		Light,
		ThrowItem,
		Action,
		MiddleButton,
		SeeThroughUp,
		SeeThroughDown,
        Escape,
        DebugWindow,
        Console,
		Thrust,
		MicroVoxels,
		Rotate,
		Custom1,
		Custom2,
		Custom3,
		Custom4,
		Custom5,
		Custom6,
		Custom7,
		Custom8,
		Custom9
    }

	public enum InputButtonPressState {
		Idle,
		Down,
		Up,
		Pressed
	}

	public struct InputButtonState {
		public InputButtonPressState pressState;
		public float pressStartTime;
	}


	[DefaultExecutionOrder(-100)]
	public abstract class VoxelPlayInputController: MonoBehaviour {
		/// <summary>
		/// 수평 입력축
		/// </summary>
		[NonSerialized]
		public float horizontalAxis;

		/// <summary>
		/// 수직 입력축
		/// </summary>
		[NonSerialized]
		public float verticalAxis;

		/// <summary>
		/// 수직 입력축
		/// </summary>
		[NonSerialized]
		public bool anyAxisButtonPressed;

		/// <summary>
		/// 수평 마우스 축
		/// </summary>
		[NonSerialized]
		public float mouseX;

		/// <summary>
		/// 수직 마우스 축
		/// </summary>
		[NonSerialized]
		public float mouseY;

		/// <summary>
		/// 수직 마우스 축
		/// </summary>
		[NonSerialized]
		public float mouseScrollWheel;

		/// <summary>
		/// 화면상의 커서 위치
		/// </summary>
		[NonSerialized]
		public Vector3 screenPos;

		/// <summary>
		/// 커서가 화면 안에 있는 경우
		/// </summary>
		[NonSerialized]
		public bool focused = true;

        /// <summary>
        /// 아무 버튼이나 키를 누르면 true를 반환합니다.
        /// </summary>
        [NonSerialized]
        public bool anyKey;

        /// <summary>
        /// 클릭을 알리기 위해 버튼을 눌렀다가 떼는 사이의 최대 시간(초)
        /// </summary>
        public float clickTime = 0.2f;

		/// <summary>
		/// 이동/회전 컨트롤이 비활성화되었지만 나머지 입력 버튼은 여전히 ​​콘솔, Esc 등과 같이 처리됩니다.
		/// </summary>
		public virtual new bool enabled { get; set; }

		[NonSerialized]
		public bool initialized;

		protected InputButtonState[] buttons;

		protected virtual bool Initialize () {
			return true;
		}

		protected abstract void UpdateInputState ();

		public bool GetButton (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Pressed;
		}

		public bool GetButtonDown (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Down;
		}

		public bool GetButtonUp (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Up;
		}

		public bool GetButtonClick (InputButtonNames button) {
			return initialized && buttons [(int)button].pressState == InputButtonPressState.Up && (Time.time - buttons [(int)button].pressStartTime) < clickTime;
		}

		bool ignoreThisFrame;


        public void Init () {
			int buttonCount = Enum.GetNames (typeof(InputButtonNames)).Length;
			buttons = new InputButtonState[buttonCount];
			initialized = Initialize ();
			enabled = true;
			ignoreThisFrame = true;
		}

        void Update () {
			if (!initialized)
				return;
			anyKey = Input.anyKey;
			for (int k = 0; k < buttons.Length; k++) {
				buttons [k].pressState = InputButtonPressState.Idle;
			}
            if (!enabled) {
                mouseScrollWheel = mouseX = mouseY = horizontalAxis = verticalAxis = 0;
            }

			if (ignoreThisFrame) {
				ignoreThisFrame = false;
			} else {
				UpdateInputState ();
			}
			if (!anyKey) {
				for (int k = 0; k < buttons.Length; k++) {
					if (buttons [k].pressState != InputButtonPressState.Idle) {
						anyKey = true;
						break;
					}
				}
			}
		}


		protected void ReadButtonState (InputButtonNames button, string buttonName) {
			if (Input.GetButtonDown (buttonName)) {
				buttons [(int)button].pressStartTime = Time.time;
				buttons [(int)button].pressState = InputButtonPressState.Down;
			} else if (Input.GetButtonUp (buttonName)) {
				buttons [(int)button].pressState = InputButtonPressState.Up;
			} else if (Input.GetButton (buttonName)) {
				buttons [(int)button].pressState = InputButtonPressState.Pressed;
			}
		}


		protected void ReadKeyState (InputButtonNames button, KeyCode keyCode) {
			if (Input.GetKeyDown (keyCode)) {
				buttons [(int)button].pressStartTime = Time.time;
				buttons [(int)button].pressState = InputButtonPressState.Down;
			} else if (Input.GetKeyUp (keyCode)) {
				buttons [(int)button].pressState = InputButtonPressState.Up;
			} else if (Input.GetKey (keyCode)) {
				buttons [(int)button].pressState = InputButtonPressState.Pressed;
			}
		}
	
	}



}
