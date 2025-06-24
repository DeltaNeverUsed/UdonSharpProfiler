using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UdonSharpProfiler {
    public class UdonSharpAssemblyModuleWrapper {
        private readonly Type _moduleType;

        public UdonSharpAssemblyModuleWrapper(object module) {
            Module = module;
            _moduleType = Module.GetType();
        }

        public object Module { get; }

        private object RootTable => _moduleType.GetProperty("RootTable").GetValue(Module);

        private IEnumerable<object> RootTableValues =>
            ((IList)RootTable.GetType().GetProperty("Values").GetValue(RootTable)).Cast<object>();

        public void AddPush(object value) {
            MethodInfo method = _moduleType.GetMethod("AddPush");
            method.Invoke(Module, new[] { value });
        }

        public void AddExtern(object context, string eventName) {
            Type targetType = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.ExternSynthesizedMethodSymbol");
            ConstructorInfo constructor = targetType.GetConstructors()[1];

            object externValue = constructor.Invoke(new[] {
                context, eventName,
                Array.CreateInstance(ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.TypeSymbol"), 0), null,
                false, false
            });

            MethodInfo method = _moduleType.GetMethod("AddExtern");
            method.Invoke(Module, new[] { externValue });
        }

        public object GetValue(string uniqueID) {
            return RootTableValues.FirstOrDefault(v =>
                (string)v.GetType().GetProperty("UniqueID").GetValue(v) == uniqueID);
        }

        public object GetValueDefault(string uniqueID) {
            object value = GetValue(uniqueID);
            return value.GetType().GetProperty("DefaultValue").GetValue(value);
        }

        public void SetValueDefault(string uniqueID, object val) {
            object value = GetValue(uniqueID);
            value.GetType().GetProperty("DefaultValue").SetValue(value, val);
        }

        public void AddCommentTag(string message) {
            MethodInfo method = _moduleType.GetMethod("AddCommentTag");
            method.Invoke(Module, new object[] { message });
        }

        public object GetTypeSymbol(object context, Type type) {
            MethodInfo method = ReflectionHelper.GetMethod(context.GetType(), "GetTypeSymbol", new[] { typeof(Type) },
                BindingFlags.Public | BindingFlags.Instance);
            return method.Invoke(context, new object[] { type });
        }

        public object GetConstantValue(object context, Type type, object constant) {
            MethodInfo method = RootTable.GetType().GetMethod("GetConstantValue");
            return method.Invoke(RootTable, new[] { GetTypeSymbol(context, type), constant, null });
        }
    }
}