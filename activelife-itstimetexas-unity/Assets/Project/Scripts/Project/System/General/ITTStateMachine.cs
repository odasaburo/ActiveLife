using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;

namespace ITT.System {
	public abstract class ITTStateMachine : MonoBehaviour {
		[HideInInspector]
		public Transform localTransform;
		[HideInInspector]
		public new Transform transform;
		[HideInInspector]
		public new Rigidbody rigidbody;
		[HideInInspector]
		public new Animation animation;
		[HideInInspector]
		public new Collider collider;
		[HideInInspector]
		public new GameObject gameObject;
		[HideInInspector]
		public ITTStateMachine baseStateMachine;
		
		private float _timeEnteredState;
		public float timeInCurrentState {
			get {
				return Time.time - _timeEnteredState;
			}
		}
		
		bool _exitedState = true;
		bool _enteredState = true;
		
		void Awake() {
			OnAwake();
		}
		
		void Represent(GameObject gameObjectToRepresent) {
			gameObject = gameObjectToRepresent;
			collider = gameObject.GetComponent<Collider>();
			transform = gameObject.transform;
			animation = gameObject.GetComponent<Animation>();
			rigidbody = gameObject.GetComponent<Rigidbody>();
			OnRepresent();
		}
		
		public void Refresh() {
			Represent(gameObject);
		}
		
		protected virtual void OnAwake() {
			state.executingStateMachine = baseStateMachine = this;
			_exitedState = true;
			_enteredState = true;
			localTransform = base.transform;
			Represent(base.gameObject);			
		}
		
		protected virtual void OnRepresent() {
			
		}
		
		static IEnumerator DoNothingCoroutine() {
			yield break;
		}
		
		static void DoNothing() {
			
		}
		
		static void DoNothingCollider(Collider other) {
			
		}
		
		static void DoNothingCollision(Collision other) {
			
		}
		
		#region State Delegate Class Wrapper
		public class State {
			public Action DoUpdate = DoNothing;
			public Action DoLateUpdate = DoNothing;
			public Action DoFixedUpdate = DoNothing;
			public Action<Collider> DoOnTriggerEnter = DoNothingCollider;
			public Action<Collider> DoOnTriggerStay = DoNothingCollider;
			public Action<Collider> DoOnTriggerExit = DoNothingCollider;
			public Action<Collision> DoOnCollisionEnter = DoNothingCollision;
			public Action<Collision> DoOnCollisionStay = DoNothingCollision;
			public Action<Collision> DoOnCollisionExit = DoNothingCollision;
			public Action DoOnMouseEnter = DoNothing;
			public Action DoOnMouseUp = DoNothing;
			public Action DoOnMouseDown = DoNothing;
			public Action DoOnMouseOver = DoNothing;
			public Action DoOnMouseExit = DoNothing;
			public Action DoOnMouseDrag = DoNothing;
			public Action DoOnGUI = DoNothing;
			public Func<IEnumerator> EnterState = DoNothingCoroutine;
			public Func<IEnumerator> ExitState = DoNothingCoroutine;
			
			public Enum currentState;
			public ITTStateMachine executingStateMachine;
		}
		#endregion
		
		[HideInInspector]
		public State state = new State();
		
		public Enum currentState {
			get {
				return state.currentState;
			}
			
			set {
				if(baseStateMachine != this) {
					baseStateMachine.currentState = value;
				} else {
					if(state.currentState == value) {
						return;
					}

					if (_exitedState && _enteredState) {
						_exitedState = false;
						_enteredState = false;
					} else {
						return;
					}

					ChangingState();
					state.currentState = value;
					state.executingStateMachine.state.currentState = value;
					ConfigureCurrentState();
				}
			}
		}
		
		[HideInInspector]
		public Enum lastState;

		public Enum previousState {
			get {
				return lastState;
			}
		}
		
		[HideInInspector]
		public ITTStateMachine lastGameStateMachineBehavior;
		
		public void SetState(Enum stateToActivate, ITTStateMachine useGameStateMachine) {
			if(state.executingStateMachine == useGameStateMachine && stateToActivate == state.currentState) {
				return;
			}

			if (_exitedState && _enteredState) {
				_exitedState = false;
				_enteredState = false;
			} else {
				return;
			}
			
			ChangingState();
			state.currentState = stateToActivate;
			state.executingStateMachine = useGameStateMachine;
			
			if(useGameStateMachine != this) {
				useGameStateMachine.baseStateMachine = this;
			}
			
			state.executingStateMachine.state.currentState = stateToActivate;
			useGameStateMachine.Represent(gameObject);
			ConfigureCurrentState();
		}
		
		void ChangingState() {
			lastState = state.currentState;
			lastGameStateMachineBehavior = state.executingStateMachine;
			_timeEnteredState = Time.time;
		}
		
		void ConfigureCurrentState() {
			StartCoroutine (ExitStateWrapper ());
			
			state.DoUpdate = state.executingStateMachine.ConfigureDelegate<Action>("Update", DoNothing);
			state.DoLateUpdate = state.executingStateMachine.ConfigureDelegate<Action>("LateUpdate", DoNothing);
			state.DoFixedUpdate = state.executingStateMachine.ConfigureDelegate<Action>("FixedUpdate", DoNothing);
			
			state.DoOnTriggerEnter = state.executingStateMachine.ConfigureDelegate<Action<Collider>>("OnTriggerEnter", DoNothingCollider);
			state.DoOnTriggerStay = state.executingStateMachine.ConfigureDelegate<Action<Collider>>("OnTriggerStay", DoNothingCollider);
			state.DoOnTriggerExit = state.executingStateMachine.ConfigureDelegate<Action<Collider>>("OnTriggerExit", DoNothingCollider);
			state.DoOnCollisionEnter = state.executingStateMachine.ConfigureDelegate<Action<Collision>>("OnCollisionEnter", DoNothingCollision);
			state.DoOnCollisionStay = state.executingStateMachine.ConfigureDelegate<Action<Collision>>("OnCollisionStay", DoNothingCollision);
			state.DoOnCollisionExit = state.executingStateMachine.ConfigureDelegate<Action<Collision>>("OnCollisionExit", DoNothingCollision);
			state.DoOnMouseEnter = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseEnter", DoNothing);
			state.DoOnMouseUp = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseUp", DoNothing);
			state.DoOnMouseDown = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseDown", DoNothing);
			state.DoOnMouseOver = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseOver", DoNothing);
			state.DoOnMouseExit = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseExit", DoNothing);
			state.DoOnMouseDrag = state.executingStateMachine.ConfigureDelegate<Action>("OnMouseDrag", DoNothing);
			state.DoOnGUI = state.executingStateMachine.ConfigureDelegate<Action>("OnGUI", DoNothing);
			
			state.EnterState = state.executingStateMachine.ConfigureDelegate<Func<IEnumerator>>("EnterState", DoNothingCoroutine);
			state.ExitState = state.executingStateMachine.ConfigureDelegate<Func<IEnumerator>>("ExitState", DoNothingCoroutine);
			
			EnableGUI();
			
			StartCoroutine (EnterStateWrapper ());
		}

		IEnumerator ExitStateWrapper()
		{
			if(state.ExitState != null) {
				yield return baseStateMachine.StartCoroutine(state.ExitState());
			}
			_exitedState = true;
		}

		IEnumerator EnterStateWrapper()
		{
			if(state.EnterState != null) {
				yield return baseStateMachine.StartCoroutine(state.EnterState());
			}
			_enteredState = true;
		}
		
		Dictionary<Enum, Dictionary<string, Delegate>> _cache = new Dictionary<Enum, Dictionary<string, Delegate>>();
		
		T ConfigureDelegate<T>(string methodRoot, T Default) where T : class {
			Dictionary<string, Delegate> lookup;
			if(!_cache.TryGetValue(state.currentState, out lookup)) {
				_cache[state.currentState] = lookup = new Dictionary<string, Delegate>();
			}
			
			Delegate returnValue;
			if(!lookup.TryGetValue(methodRoot, out returnValue)) {
				var classMethod = GetType().GetMethod(state.currentState.ToString() + "_" + methodRoot, 
				                                      BindingFlags.Instance | 
				                                      BindingFlags.Public | 
				                                      BindingFlags.NonPublic | 
				                                      BindingFlags.InvokeMethod);
				if(classMethod != null) {
					returnValue =  Delegate.CreateDelegate(typeof(T), this, classMethod);
				} else {
					returnValue = Default as Delegate;
				}
				lookup[methodRoot] = returnValue;
			}
			return returnValue as T;
		}
		
		#region Delegate Wrapper Methods
		protected void EnableGUI() {
			useGUILayout = state.DoOnGUI != DoNothing;
		}
		
		protected virtual void Update() {
			state.DoUpdate();
		}
		
		protected virtual void LateUpdate() {
			state.DoLateUpdate();
		}
		
		void FixedUpdate() {
			state.DoFixedUpdate();
		}
		
		void OnTriggerEnter(Collider other) {
			state.DoOnTriggerEnter(other);
		}
		
		void OnTriggerStay(Collider other) {
			state.DoOnTriggerStay(other);
		}
		
		void OnTriggerExit(Collider other) {
			state.DoOnTriggerExit(other);
		}
		
		void OnCollisionEnter(Collision other) {
			state.DoOnCollisionEnter(other);
		}
		
		void OnCollisionStay(Collision other) {
			state.DoOnCollisionStay(other);
		}
		
		void OnCollisionExit(Collision other) {
			state.DoOnCollisionExit(other);
		}
		
		void OnMouseEnter() {
			state.DoOnMouseEnter();
		}
		
		void OnMouseUp() {
			state.DoOnMouseUp();
		}
		
		void OnMouseDown() {
			state.DoOnMouseDown();
		}
		
		void OnMouseExit() {
			state.DoOnMouseExit();
		}
		
		void OnMouseDrag() {
			state.DoOnMouseDrag();
		}
		
		void OnGUI() {
			state.DoOnGUI();
		}
		#endregion
		
		#region Animation Handlers
		public IEnumerator WaitForAnimation(string name, float ratio) {
			var state = animation[name];
			return WaitForAnimation(state, ratio);
		}
		
		public static IEnumerator WaitForAnimation(AnimationState state, float ratio) {
			state.wrapMode = WrapMode.ClampForever;
			state.enabled = true;
			state.speed = state.speed == 0 ? 1 : state.speed;
			while(state.normalizedTime < ratio - float.Epsilon) {
				yield return null;
			}
		}
		
		public IEnumerator PlayAnimation(string name) {
			var state = animation[name];
			return PlayAnimation(state);
		}
		
		public static IEnumerator PlayAnimation(AnimationState state) {
			state.time = 0;
			state.weight = 1;
			state.speed = 1;
			state.enabled = true;
			var wait = WaitForAnimation(state, 1.0f);
			while(wait.MoveNext()) {
				yield return null;
			}
			state.weight = 0;
		}
		#endregion
	}
}

