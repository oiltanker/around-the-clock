using System;
using System.Collections.Generic;
using System.Reflection;

namespace AroundTheClock.Core {

  // Reflection access to internal/private game members. Unity Mono ignores
  // IgnoresAccessChecksTo, so direct IL access to non-public members throws
  // MethodAccessException; reflection does not.
  public static class Access {
    private static readonly Dictionary<string, FieldInfo> Fields = new();
    private static readonly Dictionary<string, MethodInfo> Methods = new();
    private static readonly Dictionary<string, object> Statics = new();

    public static object GetField(object obj, string name) {
      if (obj == null) {
        throw new ArgumentNullException(nameof(obj), $"Access.GetField('{name}') on null instance");
      }
      var field = FieldOf(obj.GetType(), name);
      if (field == null) {
        throw new MissingMemberException(obj.GetType().FullName, name);
      }
      return field.GetValue(obj);
    }

    public static T GetField<T>(object obj, string name) {
      return (T)GetField(obj, name);
    }

    public static object GetStatic(Type type, string name) {
      var key = type.FullName + "::" + name;
      if (!Statics.TryGetValue(key, out var value)) {
        value = FieldOf(type, name).GetValue(null);
        Statics[key] = value;
      }
      return value;
    }

    public static object Invoke(object obj, string name, params object[] args) {
      var key = obj.GetType().FullName + "::" + name;
      if (!Methods.TryGetValue(key, out var method)) {
        method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Methods[key] = method;
      }
      return method.Invoke(obj, args);
    }

    public static object Invoke(Type type, string name, object target, params object[] args) {
      var key = type.FullName + "::" + name;
      if (!Methods.TryGetValue(key, out var method)) {
        method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Methods[key] = method;
      }
      return method.Invoke(target, args);
    }

    private static FieldInfo FieldOf(Type type, string name) {
      var key = type.FullName + "::" + name;
      if (!Fields.TryGetValue(key, out var field)) {
        field = type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        Fields[key] = field;
      }
      return field;
    }
  }
}
