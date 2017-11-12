﻿namespace ConsoleApp.Reflection
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection;
    using Smart.Reflection.Emit;

    /// <summary>
    ///
    /// </summary>
    public static class EmitMethodGenerator
    {
        private const string AssemblyName = "SmartDynamicActivatorAssembly";

        private const string ModuleName = "SmartDynamicActivatorModule";

        private static readonly Type ObjectType = typeof(object);

        private static readonly Type VoidType = typeof(void);

        private static readonly Type BoolType = typeof(bool);

        private static readonly Type StringType = typeof(string);

        private static readonly Type CtorType = typeof(ConstructorInfo);

        private static readonly Type TypeType = typeof(Type);

        private static readonly Type PropertyInfoType = typeof(PropertyInfo);

        private static readonly ConstructorInfo ObjectCotor = typeof(object).GetConstructor(Type.EmptyTypes);

        private static readonly ConstructorInfo NotSupportedExceptionCtor = typeof(NotSupportedException).GetConstructor(Type.EmptyTypes);

        private static readonly MethodInfo PropertyInfoNameGetMethod =
            typeof(PropertyInfo).GetProperty(nameof(PropertyInfo.Name)).GetGetMethod();

        private static readonly MethodInfo PropertyInfoPropertyTypeGetMethod =
            typeof(PropertyInfo).GetProperty(nameof(PropertyInfo.PropertyType)).GetGetMethod();

        // Activator

        private static readonly Type ActivatorType = typeof(IActivator);

        private static readonly MethodInfo ActivatorCreateMethodInfo = typeof(IActivator).GetMethod(nameof(IActivator.Create));

        private static readonly Type[] ActivatorConstructorArgumentTypes = { typeof(ConstructorInfo) };

        private static readonly Type[] ActivatorCreateArgumentTypes = { typeof(object[]) };

        // Accessor

        private static readonly Type AccessorType = typeof(IAccessor);

        private static readonly MethodInfo AccessorGetValueMethodInfo = typeof(IAccessor).GetMethod(nameof(IAccessor.GetValue));

        private static readonly MethodInfo AccessorSetValueMethodInfo = typeof(IAccessor).GetMethod(nameof(IAccessor.SetValue));

        private static readonly Type[] AccessorConstructorArgumentTypes = { typeof(PropertyInfo) };

        private static readonly Type[] AccessorGetValueArgumentTypes = { typeof(object) };

        private static readonly Type[] AccessorSetValueArgumentTypes = { typeof(object), typeof(object) };

        // Member

        private static readonly object Sync = new object();

        private static AssemblyBuilder assemblyBuilder;

        private static ModuleBuilder moduleBuilder;

        /// <summary>
        ///
        /// </summary>
        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                lock (Sync)
                {
                    if (moduleBuilder == null)
                    {
                        assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                            new AssemblyName(AssemblyName),
                            AssemblyBuilderAccess.RunAndSave);
                            //AssemblyBuilderAccess.Run);
                        moduleBuilder = assemblyBuilder.DefineDynamicModule(
                            ModuleName,
                            "test.dll");
                            //ModuleName);
                    }
                    return moduleBuilder;
                }
            }
        }

        //--------------------------------------------------------------------------------
        // Activator
        //--------------------------------------------------------------------------------

        /// <summary>
        ///
        /// </summary>
        /// <param name="ci"></param>
        /// <returns></returns>
        public static IActivator CreateActivator(ConstructorInfo ci)
        {
            if (ci == null)
            {
                throw new ArgumentNullException(nameof(ci));
            }

            var typeBuilder = ModuleBuilder.DefineType(
                $"{ci.DeclaringType.FullName}_DynamicActivator",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

            typeBuilder.AddInterfaceImplementation(ActivatorType);

            // Field
            var sourceField = typeBuilder.DefineField(
                "source",
                CtorType,
                FieldAttributes.Private | FieldAttributes.InitOnly);

            // Property
            DefineActivatorPropertySource(typeBuilder, sourceField);

            // Constructor
            DefineActivatorConstructor(typeBuilder, sourceField);

            // Method
            DefineActivatorMethodCreate(typeBuilder, ci);

            var typeInfo = typeBuilder.CreateTypeInfo();

            return (IActivator)Activator.CreateInstance(typeInfo.AsType(), ci);
        }

        private static void DefineActivatorPropertySource(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var sourceProperty = typeBuilder.DefineProperty(
                "Source",
                PropertyAttributes.None,
                CtorType,
                null);
            var getSourceProperty = typeBuilder.DefineMethod(
                "get_Source",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                CtorType,
                Type.EmptyTypes);
            sourceProperty.SetGetMethod(getSourceProperty);

            var getSourceIl = getSourceProperty.GetILGenerator();

            getSourceIl.Emit(OpCodes.Ldarg_0);
            getSourceIl.Emit(OpCodes.Ldfld, sourceField);
            getSourceIl.Emit(OpCodes.Ret);
        }

        private static void DefineActivatorConstructor(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                ActivatorConstructorArgumentTypes);

            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, ObjectCotor);
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Ldarg_1);
            ctorIl.Emit(OpCodes.Stfld, sourceField);
            ctorIl.Emit(OpCodes.Ret);
        }

        private static void DefineActivatorMethodCreate(TypeBuilder typeBuilder, ConstructorInfo ci)
        {
            var method = typeBuilder.DefineMethod(
                nameof(IActivator.Create),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                ObjectType,
                ActivatorCreateArgumentTypes);
            typeBuilder.DefineMethodOverride(method, ActivatorCreateMethodInfo);

            var ilGenerator = method.GetILGenerator();

            for (var i = 0; i < ci.GetParameters().Length; i++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.EmitLdcI4(i);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
                ilGenerator.EmitTypeConversion(ci.GetParameters()[i].ParameterType);
            }

            ilGenerator.Emit(OpCodes.Newobj, ci);
            ilGenerator.Emit(OpCodes.Ret);
        }

        //--------------------------------------------------------------------------------
        // Accessor
        //--------------------------------------------------------------------------------

        public static IAccessor CreateAccessor(PropertyInfo pi)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"{pi.DeclaringType.FullName}_{pi.Name}_DynamicAccsessor",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

            typeBuilder.AddInterfaceImplementation(AccessorType);

            // Fields
            var sourceField = typeBuilder.DefineField(
                 "source",
                 PropertyInfoType,
                 FieldAttributes.Private | FieldAttributes.InitOnly);

            // Property
            DefineAccessorPropertySource(typeBuilder, sourceField);
            DefineAccessorPropertyName(typeBuilder, sourceField);
            DefineAccessorPropertyType(typeBuilder, sourceField);
            // TODO IValueHolder
            DefineAccessorPropertyAccessibility(typeBuilder, pi.CanRead, nameof(IAccessor.CanRead));
            DefineAccessorPropertyAccessibility(typeBuilder, pi.CanWrite, nameof(IAccessor.CanWrite));

            // Constructor
            DefineAccessorConstructor(typeBuilder, sourceField);

            // Method
            DefineAccessorMethodGetValue(typeBuilder, pi);
            DefineAccessorMethodSetValue(typeBuilder, pi);

            var type = typeBuilder.CreateType();

            assemblyBuilder.Save("test.dll");

            return (IAccessor)Activator.CreateInstance(type, pi);
        }

        private static void DefineAccessorPropertySource(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            var property = typeBuilder.DefineProperty(
                "Source",
                PropertyAttributes.None,
                PropertyInfoType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Source",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                PropertyInfoType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorPropertyName(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            // TODO IValueHolder
            var property = typeBuilder.DefineProperty(
                "Name",
                PropertyAttributes.None,
                StringType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Name",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                StringType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Callvirt, PropertyInfoNameGetMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorPropertyType(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            // TODO IValueHolder
            var property = typeBuilder.DefineProperty(
                "Type",
                PropertyAttributes.None,
                TypeType,
                null);
            var method = typeBuilder.DefineMethod(
                "get_Type",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                TypeType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, sourceField);
            ilGenerator.Emit(OpCodes.Callvirt, PropertyInfoPropertyTypeGetMethod);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorPropertyAccessibility(TypeBuilder typeBuilder, bool enable, string name)
        {
            // TODO IValueHolder
            var property = typeBuilder.DefineProperty(
                name,
                PropertyAttributes.None,
                BoolType,
                null);
            var method = typeBuilder.DefineMethod(
                $"get_{name}",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.Final,
                BoolType,
                Type.EmptyTypes);
            property.SetGetMethod(method);

            var ilGenerator = method.GetILGenerator();

            if (enable)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorConstructor(TypeBuilder typeBuilder, FieldBuilder sourceField)
        {
            // TODO IValueHolder
            var ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                AccessorConstructorArgumentTypes);

            var ilGenerator = ctor.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, ObjectCotor);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, sourceField);
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorMethodGetValue(TypeBuilder typeBuilder, PropertyInfo pi)
        {
            var method = typeBuilder.DefineMethod(
                nameof(IAccessor.GetValue),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                ObjectType,
                AccessorGetValueArgumentTypes);
            typeBuilder.DefineMethodOverride(method, AccessorGetValueMethodInfo);

            var ilGenerator = method.GetILGenerator();

            if (!pi.CanRead)
            {
                ilGenerator.Emit(OpCodes.Newobj, NotSupportedExceptionCtor);
                ilGenerator.Emit(OpCodes.Throw);
                return;
            }

            // TODO IValueHolder
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, pi.DeclaringType);
            ilGenerator.Emit(OpCodes.Callvirt, pi.GetGetMethod());
            if (pi.PropertyType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, pi.PropertyType);
            }
            ilGenerator.Emit(OpCodes.Ret);
        }

        private static void DefineAccessorMethodSetValue(TypeBuilder typeBuilder, PropertyInfo pi)
        {
            var method = typeBuilder.DefineMethod(
                nameof(IAccessor.SetValue),
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
                VoidType,
                AccessorSetValueArgumentTypes);
            typeBuilder.DefineMethodOverride(method, AccessorSetValueMethodInfo);

            var ilGenerator = method.GetILGenerator();

            if (!pi.CanWrite)
            {
                ilGenerator.Emit(OpCodes.Newobj, NotSupportedExceptionCtor);
                ilGenerator.Emit(OpCodes.Throw);
                return;
            }

            // TODO IValueHolder
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Castclass, pi.DeclaringType);

            if (pi.PropertyType.IsValueType)
            {
                // TODO name
                var label1 = ilGenerator.DefineLabel();
                var label2 = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Brfalse_S, label1);

                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Unbox_Any, pi.PropertyType);
                ilGenerator.Emit(OpCodes.Br_S, label2);

                ilGenerator.MarkLabel(label1);
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                ilGenerator.MarkLabel(label2);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Castclass, pi.PropertyType);
            }

            ilGenerator.Emit(OpCodes.Callvirt, pi.GetSetMethod());
            ilGenerator.Emit(OpCodes.Ret);
        }
    }
}