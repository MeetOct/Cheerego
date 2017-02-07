using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Cheerego.Core.Reflection
{
	/// <summary>
	/// 有时间研究一下Emit
	/// </summary>
	public class ReappearMember
	{
		public static Action<object, object> CreatePropertySetter(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (!property.CanWrite)
				return null;

			MethodInfo setMethod = property.GetSetMethod();
			DynamicMethod dm = new DynamicMethod("PropertySetter", null, new Type[] { typeof(object), typeof(object) }, property.DeclaringType, true);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			Type type = property.PropertyType;
			if (type.IsValueType)
				il.Emit(OpCodes.Unbox_Any, type);
			else
				il.Emit(OpCodes.Castclass, type);
			if (property.DeclaringType.IsValueType)
				il.EmitCall(OpCodes.Call, setMethod, null);
			else
				il.EmitCall(OpCodes.Callvirt, setMethod, null);
			il.Emit(OpCodes.Ret);

			return dm.CreateDelegate(typeof(Action<object, object>)) as Action<object, object>;
		}

		public static Func<object, object> CreatePropertyGetter(PropertyInfo property)
		{
			if (property == null)
				throw new ArgumentNullException("property");
			if (!property.CanWrite)
				return null;

			MethodInfo getMethod = property.GetGetMethod();
			DynamicMethod dm = new DynamicMethod("PropertyGetter", typeof(object), new Type[] { typeof(object) }, property.DeclaringType, true);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			if (property.DeclaringType.IsValueType)
				il.EmitCall(OpCodes.Call, getMethod, null);
			else
				il.EmitCall(OpCodes.Callvirt, getMethod, null);
			Type type = property.PropertyType;
			if (type.IsValueType)
				il.Emit(OpCodes.Box, type);
			else
				il.Emit(OpCodes.Castclass, type);
			il.Emit(OpCodes.Ret);

			return dm.CreateDelegate(typeof(Func<object, object>)) as Func<object, object>;
		}
	}
}
