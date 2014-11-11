using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Sleipner.Core.Util;

namespace Sleipner.Core
{
    public class IlGeneratorProxyGenerator
    {
        private static readonly AssemblyBuilder AssemblyBuilder;
        private static readonly ModuleBuilder ModuleBuilder;
        private static readonly IDictionary<Type, Type> TypeCache = new ConcurrentDictionary<Type, Type>();

        static IlGeneratorProxyGenerator()
        {
            var currentDomain = AppDomain.CurrentDomain;
            var dynamicAssemblyName = new AssemblyName
            {
                Name = "Sleipner2CacheProxies",
            };

            AssemblyBuilder = currentDomain.DefineDynamicAssembly(dynamicAssemblyName, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Sleipner2CacheProxies", "Sleipner2CacheProxies.dll");
        }

        public static Type CreateProxyFor<TInterface>() where TInterface : class
        {
            var interfaceType = typeof(TInterface);
            Type proxyDelegatorType;
            if (!TypeCache.TryGetValue(interfaceType, out proxyDelegatorType))
            {
                proxyDelegatorType = EmitProxyFor<TInterface>();
                TypeCache[interfaceType] = proxyDelegatorType;
            }

            return proxyDelegatorType;
        }

        private static Type EmitProxyFor<TInterface>() where TInterface : class
        {
            var interfaceType = typeof(TInterface);
            var proxyBuilder = ModuleBuilder.DefineType("Proxies." + interfaceType.FullName + "__sleipner_proxy", TypeAttributes.Class | TypeAttributes.Public, null, new[] { interfaceType });

            //Create two class members: handler and realInstance
            var implementationField = proxyBuilder.DefineField("implementation", interfaceType, FieldAttributes.Private);
            var handlerField = proxyBuilder.DefineField("handler", typeof(IProxyHandler<TInterface>), FieldAttributes.Private);

            //Create the constructor
            var cTorBuilder = proxyBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { interfaceType, typeof(IProxyHandler<TInterface>) });
            cTorBuilder.DefineParameter(1, ParameterAttributes.None, "implementation");
            cTorBuilder.DefineParameter(2, ParameterAttributes.None, "handler");

            //Create constructor body
            var cTorBody = cTorBuilder.GetILGenerator();

            cTorBody.Emit(OpCodes.Ldarg_0);
            cTorBody.Emit(OpCodes.Call, typeof(Object).GetConstructor(Type.EmptyTypes));

            cTorBody.Emit(OpCodes.Ldarg_0);                         //Load this on stack
            cTorBody.Emit(OpCodes.Ldarg_1);                         //Load the first parameter (the realInstance parameter) of the constructor on stack
            cTorBody.Emit(OpCodes.Stfld, implementationField);      //Store parameter reference in realInstanceField

            cTorBody.Emit(OpCodes.Ldarg_0);                         //Load this on stack
            cTorBody.Emit(OpCodes.Ldarg_2);                         //Load second parameter on stack (the IProxyHandler parameter).
            cTorBody.Emit(OpCodes.Stfld, handlerField);             //Store parameter refrence in handlerField

            cTorBody.Emit(OpCodes.Ret);                             //Return
            foreach (var method in interfaceType.GetMethods())
            {
                var parameterTypes = method.GetParameters().Select(a => a.ParameterType).ToArray();
                var proxyMethod = proxyBuilder.DefineMethod(
                    method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual,
                    CallingConventions.HasThis,
                    method.ReturnType,
                    parameterTypes);

                if (method.IsGenericMethod)
                {
                    var genericTypes = method.GetGenericArguments();
                    foreach (var type in genericTypes)
                    {
                        var isReferenceConstrainted = type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint);
                        if (!isReferenceConstrainted)
                            throw new SleipnerGenericParameterMustBeReferenceException(method, type);   
                    }

                    proxyMethod.DefineGenericParameters(genericTypes.Select(a => a.Name).ToArray());
                }

                var methodIndex = interfaceType.GetMethods().ToList().IndexOf(method);
                var methodBody = proxyMethod.GetILGenerator();

                if (method.ReturnType == typeof(void) || (typeof(Task).IsAssignableFrom(method.ReturnType) && !method.ReturnType.IsGenericType))
                {
                    methodBody.Emit(OpCodes.Ldarg_0);                           //Load this on the stack
                    methodBody.Emit(OpCodes.Ldfld, implementationField);        //Load the real instance on the stack
                    for (var i = 0; i < parameterTypes.Length; i++)             //Load all parameters on the stack
                    {
                        methodBody.Emit(OpCodes.Ldarg, i + 1);                  //Method parameter on stack
                    }

                    methodBody.Emit(OpCodes.Callvirt, method);                  //Call the method in question on the instance
                    methodBody.Emit(OpCodes.Ret);                               //Return to caller.

                    continue;
                }

                /* Load the methodinfo of the current method into a local variable */

                var methodInfoLocal = methodBody.DeclareLocal(typeof(MethodInfo));
                methodBody.Emit(OpCodes.Ldtoken, typeof(TInterface));                                            //typeof(T)
                methodBody.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));             //typeof(T) NOTICE USE OF CALL INSTEAD OF CALLVIRT
                methodBody.Emit(OpCodes.Callvirt, typeof(Type).GetMethod("GetMethods", new Type[0]));   //.GetMethods(new Type[0])
                methodBody.Emit(OpCodes.Ldc_I4, methodIndex);                                           //Read Array Index x
                methodBody.Emit(OpCodes.Ldelem, typeof(MethodInfo));                                    //As an methodinfo
                if (method.IsGenericMethod)
                {
                    var genericTypes = method.GetGenericArguments();
                    var genericTypesArray = methodBody.DeclareLocal(typeof(Type[]));
                    methodBody.Emit(OpCodes.Ldc_I4, genericTypes.Length);
                    methodBody.Emit(OpCodes.Newarr, typeof(Type));
                    methodBody.Emit(OpCodes.Stloc, genericTypesArray);

                    for (var i = 0; i < genericTypes.Length; i++)
                    {
                        var genericType = genericTypes[i];
                        methodBody.Emit(OpCodes.Ldloc, genericTypesArray);
                        methodBody.Emit(OpCodes.Ldc_I4, i);
                        methodBody.Emit(OpCodes.Ldtoken, genericType);
                        methodBody.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                        methodBody.Emit(OpCodes.Stelem_Ref);
                    }

                    methodBody.Emit(OpCodes.Ldloc, genericTypesArray);
                    methodBody.Emit(OpCodes.Callvirt, typeof(MethodInfo).GetMethod("MakeGenericMethod"));
                }
                methodBody.Emit(OpCodes.Stloc, methodInfoLocal);                                    //And store it

                /* The below code creates an array that contains all the values
                 * that were passed into the method we're proxying.
                 */
                var methodParameterArray = methodBody.DeclareLocal(typeof(object[]));

                methodBody.Emit(OpCodes.Ldc_I4, parameterTypes.Length);     //Push array size on stack
                methodBody.Emit(OpCodes.Newarr, typeof(object));            //Create array
                methodBody.Emit(OpCodes.Stloc, methodParameterArray);       //Store array in local variable

                for (var i = 0; i < parameterTypes.Length; i++)             //Load all parameters on the stack
                {
                    var parameterType = parameterTypes[i];
                    methodBody.Emit(OpCodes.Ldloc, methodParameterArray);   //Push array reference
                    methodBody.Emit(OpCodes.Ldc_I4, i);                     //Push array index index
                    methodBody.Emit(OpCodes.Ldarg, i + 1);                  //Push array index value

                    if (parameterType.IsValueType)                          //TODO: Some day, see if we can make this determined at runtime
                    {
                        methodBody.Emit(OpCodes.Box, parameterType);        //Value types need to be boxed
                    }

                    methodBody.Emit(OpCodes.Stelem_Ref);                    //Store element in array
                }

                /* This creates a proxy request object */
                var isAsync = typeof(Task).IsAssignableFrom(method.ReturnType) && method.ReturnType.IsGenericType;
                Type returnType = method.ReturnType;
                if (isAsync)
                {
                    returnType = method.ReturnType.GetGenericArguments().FirstOrDefault();
                }
                var proxyRequestType = typeof(ProxiedMethodInvocation<,>).MakeGenericType(typeof(TInterface), returnType);
                var proxyRequest = methodBody.DeclareLocal(proxyRequestType);
                var proxyRequestCtor = proxyRequestType.GetConstructor(new[] { typeof(MethodInfo), typeof(object[]) });

                methodBody.Emit(OpCodes.Ldloc, methodInfoLocal);
                methodBody.Emit(OpCodes.Ldloc, methodParameterArray);
                methodBody.Emit(OpCodes.Newobj, proxyRequestCtor);
                methodBody.Emit(OpCodes.Stloc, proxyRequest);

                var proxyCallMethod = typeof(IProxyHandler<TInterface>).GetMethod(isAsync ? "HandleAsync" : "Handle");
                proxyCallMethod = proxyCallMethod.MakeGenericMethod(new[] { returnType });

                /* This generates a method call to the proxyMethod handler field. */
                var cachedItem = methodBody.DeclareLocal(method.ReturnType);
                methodBody.Emit(OpCodes.Ldarg_0);
                methodBody.Emit(OpCodes.Ldfld, handlerField);                                   //Load this on the stack
                methodBody.Emit(OpCodes.Ldloc, proxyRequest);
                methodBody.Emit(OpCodes.Callvirt, proxyCallMethod);                             //Call the interceptMethod
                methodBody.Emit(OpCodes.Stloc, cachedItem);                                     //Store the result of the method call in a local variable. This also pops it from the stack.
                methodBody.Emit(OpCodes.Ldloc, cachedItem);                                     //Load cached item on the stack
                methodBody.Emit(OpCodes.Ret);                                                   //Return to caller

                proxyBuilder.DefineMethodOverride(proxyMethod, method);
            }

            var createdType = proxyBuilder.CreateType();
            //AssemblyBuilder.Save("Sleipner2CacheProxies.dll");
            return createdType;
        }
    }
}
