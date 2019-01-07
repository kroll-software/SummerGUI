using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using KS.Foundation;

namespace SummerGUI
{		
	public class AnimationService : DisposableObject
	{
		public enum AnimationStatus
		{
			Idle,
			Started,
			Paused,
			Canceled
		}
			
		public bool Enabled { get; set; }

		private class AnimationTarget : IComparable<AnimationTarget>
		{
			public object AnimationObject { get; set; }
			public string PropertyName { get; set; }
			public double StartValue { get; set; }
			public double EndValue { get; set; }
			public int Steps { get; set; }
			public int CurrentStep { get; private set; }
			public double CurrentValue { get; private set; }
			public Type ValueType { get; private set; }

			private double DeltaValue = 0;

			private bool IsCanceled;
			private bool IsInitialized;

			public int CompareTo(AnimationTarget other)
			{
				if (other == null || other.AnimationObject == null || this.AnimationObject == null)
					return 0;

				return this.AnimationObject.GetHashCode ().CompareTo (other.AnimationObject.GetHashCode());
			}

			private void SetValue(double value)
			{
				object newValue = null;

				switch (ValueType.Name) {
				case "Double":
					newValue = value;
					break;
				case "Single":			
					newValue = value.SafeFloat ();
					break;
				case "Int32":					
					newValue = value.SafeInt ();
					break;
				case "Int64":					
					newValue = value.SafeLong();
					break;
				case "Decimal":					
					newValue = value.SafeDecimal();
					break;
					//case "Short":
					//break;
				default:
					// Unsupported type, Stop Animation
					IsCanceled = true;
					this.LogWarning ("Unsupported Animation Type: {0}", ValueType.Name);
					return;
				}

				if (newValue != null) {
					try {
						ReflectionUtils.SetPropertyValue (AnimationObject, PropertyName, newValue);
						CurrentValue = value;	
					} catch (Exception ex) {
						IsCanceled = true;
						ex.LogError ();
					}
				}
			}

			public void Initialize()
			{
				if (IsInitialized)
					return;

				IsInitialized = true;

				if (AnimationObject == null) {
					IsCanceled = true;
				} else {
					try {
						DeltaValue = (EndValue - StartValue) / Steps;
						CurrentStep = 0;
						ValueType = ReflectionUtils.GetPropertyType (
							AnimationObject.GetType (), PropertyName);

						SetValue (StartValue);
					} catch (Exception ex) {
						IsCanceled = true;
						ex.LogError ();
					}					
				}
			}

			public bool Completed
			{
				get{
					return CurrentStep >= Steps || IsCanceled;
				}
			}

			public void Animate()
			{
				if (IsInitialized && !Completed) {
					CurrentStep++;
					if (CurrentStep < Steps)
						SetValue (CurrentValue + DeltaValue);
					else {
						SetValue (EndValue);
						Widget widget = AnimationObject as Widget;
						if (widget != null) {
							try {
								widget.OnAnimationCompleted ();	
							} catch (Exception ex) {
								ex.LogWarning ();
							}
						}
					}
				}
			}
		}


		public AnimationStatus Status { get; private set; }
		private ClassicLinkedList<AnimationTarget> AnimationTargets;
		//private LinkedList<AnimationTarget> AnimationTargets;

		public int Count
		{
			get{
				lock (SyncObject) {
					return AnimationTargets.Count;
				}
			}
		}

		public void Start()
		{
			Status = AnimationStatus.Started;
		}

		public void Pause()
		{
			Status = AnimationStatus.Paused;
		}

		public void Cancel()
		{
			Status = AnimationStatus.Canceled;
			lock (SyncObject) {
				AnimationTargets.Clear ();
			}
		}			

		public void Cancel(object AnimationObject)
		{			
			lock (SyncObject) {
				if (AnimationTargets.Count == 0)
					return;
				var node = AnimationTargets.Head;
				while (node != null && node.Value.AnimationObject != AnimationObject)
					node = node.Next;				
				if (node != null) {
					AnimationTargets.Remove (node);
				}
			}
		}

		public bool IsStarted
		{
			get{
				return Status == AnimationStatus.Started;
			}
		}

		public double FrameRate { get; set; }

		public AnimationService (double frameRate)
		{
			FrameRate = Math.Max(10, Math.Min(360, frameRate));
			// SortedLinkedList has a 'robust' iterator
			// no such 'Collection was modified ..' exceptions anymore :)
			AnimationTargets = new ClassicLinkedList<AnimationTarget> ();
			Enabled = true;
		}			

		public void AddAnimation(object animationObject, string propertyName, double startValue, double endValue, double seconds)
		{
			if (!Enabled || FrameRate < 10)
				return;

			int steps = (int)(seconds * FrameRate);

			lock (SyncObject) {
				AnimationTarget target = new AnimationTarget {
					AnimationObject = animationObject,
					PropertyName = propertyName,
					StartValue = startValue,
					EndValue = endValue,
					Steps = steps
				};

				target.Initialize ();
				AnimationTargets.AddFirst (target);

				if (Status == AnimationStatus.Idle)
					Status = AnimationStatus.Started;
			}
		}
			
		public void Animate()
		{
			if (IsStarted) {
				lock (SyncObject) {
					try {
						foreach (AnimationTarget t in AnimationTargets) {
							if (t != null) {
								t.Animate ();
							}
						}
						while (AnimationTargets.Count > 0 && AnimationTargets.Current.Value.Completed)
							AnimationTargets.RemoveLast();
						if (AnimationTargets.Count == 0)
							Status = AnimationStatus.Idle;
					} catch (Exception ex) {
						ex.LogError ();
					}
				}
			}
		}

		protected override void CleanupUnmanagedResources ()
		{
			Cancel ();
			base.CleanupUnmanagedResources ();
		}
	}
}

