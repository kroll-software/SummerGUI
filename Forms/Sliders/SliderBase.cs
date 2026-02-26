using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;

namespace SummerGUI
{
	public abstract class SliderBase : Container
	{
		private float m_Value = 0;
		public float Value
		{ 
			get
			{
				return m_Value;
			}
			set
			{
				value = value.Clamp (MinValue, MaxValue);
				if (value != m_Value)
				{
					m_Value = value;
					OnValueChanged();
				}
			}
		}

		public event EventHandler<EventArgs> ValueChanged;

		protected void OnValueChanged()
		{
			ValueChanged?.Invoke(this, EventArgs.Empty);
		}

		public float MinValue { get; set; }
		public float MaxValue { get; set; }

		public void SetValidValue(float value)
		{
			Value = value.Clamp (MinValue, MaxValue);
		}

		protected float NormalizedValue
		{
			get
			{
				float range = MaxValue - MinValue;
				if (Math.Abs(range) < 0.0001f) return 0f; // Division durch 0 verhindern
				return (Value - MinValue) / range;
			}
		}

		protected SliderBase (string name, Docking dock, IWidgetStyle style)
			: base(name, dock, style)
		{			
			MinValue = 0;
			MaxValue = 1;
			Value = 0;
		}
	}
}

