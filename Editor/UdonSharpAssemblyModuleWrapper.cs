using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UdonSharpProfiler {
    public class UdonSharpAssemblyModuleWrapper {
        public object Module{ get; private set; }
        private readonly Type _moduleType;

        private object RootTable => _moduleType.GetProperty("RootTable").GetValue(Module);

        private IEnumerable<object> RootTableValues =>
            ((IList)RootTable.GetType().GetProperty("Values").GetValue(RootTable)).Cast<object>();

        public void AddPush(object value) {
            var method = _moduleType.GetMethod("AddPush");
            method.Invoke(Module, new[] { value });
        }

        public void AddExtern(object context, string eventName) {
            var targetType = ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.ExternSynthesizedMethodSymbol");
            var constructor = targetType.GetConstructors()[1];
            
            var externValue = constructor.Invoke(new object[] {
                context, eventName,
                Array.CreateInstance(ReflectionHelper.ByName("UdonSharp.Compiler.Symbols.TypeSymbol"), 0), null,
                false, false
            });

            var method = _moduleType.GetMethod("AddExtern");
            method.Invoke(Module, new[] { externValue });
        }

        public object GetValue(string uniqueID) {
            return RootTableValues.FirstOrDefault(v =>
                (string)v.GetType().GetProperty("UniqueID").GetValue(v) == uniqueID);
        }

        public object GetValueDefault(string uniqueID) {
            var value = GetValue(uniqueID);
            return value.GetType().GetProperty("DefaultValue").GetValue(value);
        }

        public void SetValueDefault(string uniqueID, object val) {
            var value = GetValue(uniqueID);
            value.GetType().GetProperty("DefaultValue").SetValue(value, val);
        }

        public UdonSharpAssemblyModuleWrapper(object module) {
            Module = module;
            _moduleType = Module.GetType();
        }

        public void AddCommentTag(string message) {
            var method = _moduleType.GetMethod("AddCommentTag");
            method.Invoke(Module, new object[] { message });
        }

        public object GetTypeSymbol(object context, Type type) {
            var method = ReflectionHelper.GetMethod(context.GetType(), "GetTypeSymbol", new []{ typeof(Type) }, BindingFlags.Public | BindingFlags.Instance);
            return method.Invoke(context, new object[] { type });
        }

        public object GetConstantValue(object context, Type type, object constant) {
            var method = RootTable.GetType().GetMethod("GetConstantValue");
            return method.Invoke(RootTable, new object[] { GetTypeSymbol(context, type), constant, null });
        }
    }
}