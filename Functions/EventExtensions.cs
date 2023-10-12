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
	}
}
