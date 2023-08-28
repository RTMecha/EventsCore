using System.Reflection;

using UnityEngine;

using LSFunctions;

using System;

namespace EventsCore.Functions
{
    public static class EventExtensions
	{
		public static bool Try(this object from, object item, Action<object> action)
        {
			if (item != null)
            {
				action(item);
				return true;
            }
			return false;
        }

		public static T GetItem<T>(this T _list, int index)
		{
			var list = _list.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_list) as T[];

			return list[index];
		}

		public static Color InvertColorHue(Color color)
		{
			double num;
			double saturation;
			double value;
			LSColors.ColorToHSV(color, out num, out saturation, out value);
			return LSColors.ColorFromHSV(num - 180.0, saturation, value);
		}

		public static Color InvertColorValue(Color color)
		{
			double num;
			double sat;
			double val;
			LSColors.ColorToHSV(color, out num, out sat, out val);

			if (val < 0.5)
			{
				val = -val + 1;
			}
			else
			{
				val = -(val - 1);
			}

			return LSColors.ColorFromHSV(num, sat, val);
		}

	}
}
