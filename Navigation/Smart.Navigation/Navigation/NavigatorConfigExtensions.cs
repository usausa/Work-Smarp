namespace Smart.Navigation
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Smart.Converter;
    using Smart.Navigation.Attributes;
    using Smart.Navigation.Components;
    using Smart.Navigation.Mappers;
    using Smart.Navigation.Plugins;
    using Smart.Reflection;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Extension method")]
    public static class NavigatorConfigExtensions
    {
        public static NavigatorConfig UseProvider<TProvider>(this NavigatorConfig config)
            where TProvider : INavigationProvider
        {
            config.Configure(c =>
            {
                c.RemoveAll<INavigationProvider>();
                c.Add<INavigationProvider, TProvider>();
            });

            return config;
        }

        public static NavigatorConfig UseProvider(this NavigatorConfig config, INavigationProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            config.Configure(c =>
            {
                c.RemoveAll<INavigationProvider>();
                c.Add(provider);
            });

            return config;
        }

        public static NavigatorConfig UseViewMapper<TViewMapper>(this NavigatorConfig config)
            where TViewMapper : IViewMapper
        {
            config.Configure(c =>
            {
                c.RemoveAll<IViewMapper>();
                c.Add<IViewMapper, TViewMapper>();
            });

            return config;
        }

        public static NavigatorConfig UseViewMapper(this NavigatorConfig config, IViewMapper mapper)
        {
            if (mapper is null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            config.Configure(c =>
            {
                c.RemoveAll<IViewMapper>();
                c.Add(mapper);
            });

            return config;
        }

        public static NavigatorConfig UseDirectViewMapper(this NavigatorConfig config, Type baseType)
        {
            if (baseType != null)
            {
                config.Configure(c =>
                {
                    c.RemoveAll<ITypeConstraint>();
                    c.Add<ITypeConstraint>(new AssignableTypeConstraint(baseType));
                });
            }

            return config.UseViewMapper<DirectViewMapper>();
        }

        public static NavigatorConfig UseDirectViewMapper<TViewBase>(this NavigatorConfig config)
        {
            return config.UseDirectViewMapper(typeof(TViewBase));
        }

        public static NavigatorConfig UseDirectViewMapper(this NavigatorConfig config)
        {
            return config.UseDirectViewMapper(null);
        }

        public static NavigatorConfig UseIdViewMapper(this NavigatorConfig config, Action<IIdViewRegister> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var options = new IdViewMapperOptions
            {
                SetupAction = action
            };

            config.Configure(c =>
            {
                c.RemoveAll<IdViewMapperOptions>();
                c.Add(options);
            });

            return config.UseViewMapper<IdViewMapper>();
        }

        public static void AutoRegister(this IIdViewRegister register, IEnumerable<Type> types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                foreach (var attr in type.GetTypeInfo().GetCustomAttributes<ViewAttribute>())
                {
                    register.Register(attr.Id, type);
                }
            }
        }

        public static NavigatorConfig UsePathViewMapper(this NavigatorConfig config, Action<PathViewMapperOptions> action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var options = new PathViewMapperOptions();
            action(options);

            config.Configure(c =>
            {
                c.RemoveAll<PathViewMapperOptions>();
                c.Add(options);
            });

            return config.UseViewMapper<PathViewMapper>();
        }

        public static NavigatorConfig UseTypeConstraint<TTypeConstraint>(this NavigatorConfig config)
            where TTypeConstraint : ITypeConstraint
        {
            config.Configure(c =>
            {
                c.RemoveAll<ITypeConstraint>();
                c.Add<ITypeConstraint, TTypeConstraint>();
            });

            return config;
        }

        public static NavigatorConfig UseTypeConstraint(this NavigatorConfig config, ITypeConstraint constraint)
        {
            if (constraint is null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            config.Configure(c =>
            {
                c.RemoveAll<ITypeConstraint>();
                c.Add(constraint);
            });

            return config;
        }

        public static NavigatorConfig UseActivator<TActivator>(this NavigatorConfig config)
            where TActivator : IActivator
        {
            config.Configure(c =>
            {
                c.RemoveAll<IActivator>();
                c.Add<IActivator, TActivator>();
            });

            return config;
        }

        public static NavigatorConfig UseActivator(this NavigatorConfig config, IActivator activator)
        {
            if (activator is null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            config.Configure(c =>
            {
                c.RemoveAll<IActivator>();
                c.Add(activator);
            });

            return config;
        }

        public static NavigatorConfig UseActivator(this NavigatorConfig config, Func<Type, object> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return config.UseActivator(new CallbackActivator(callback));
        }

        public static NavigatorConfig UseConverter<TConverter>(this NavigatorConfig config)
            where TConverter : IConverter
        {
            config.Configure(c =>
            {
                c.RemoveAll<IConverter>();
                c.Add<IConverter, TConverter>();
            });

            return config;
        }

        public static NavigatorConfig UseConverter(this NavigatorConfig config, IConverter converter)
        {
            if (converter is null)
            {
                throw new ArgumentNullException(nameof(converter));
            }

            config.Configure(c =>
            {
                c.RemoveAll<IConverter>();
                c.Add(converter);
            });

            return config;
        }

        public static NavigatorConfig UseConverter(this NavigatorConfig config, Func<object, Type, object> callback)
        {
            return config.UseConverter(new CallbackConverter(callback));
        }

        public static NavigatorConfig UseConverter(this NavigatorConfig config, IObjectConverter converter)
        {
            return config.UseConverter(new SmartConverter(converter));
        }

        public static NavigatorConfig AddPlugin<TPlugin>(this NavigatorConfig config)
            where TPlugin : IPlugin
        {
            config.Configure(c => c.Add<IPlugin, TPlugin>());

            return config;
        }

        public static NavigatorConfig AddPlugin(this NavigatorConfig config, IPlugin plugin)
        {
            if (plugin is null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            config.Configure(c => c.Add(plugin));

            return config;
        }

        public static NavigatorConfig UseDelegateFactory<TDelegateFactory>(this NavigatorConfig config)
            where TDelegateFactory : IDelegateFactory
        {
            config.Configure(c =>
            {
                c.RemoveAll<IDelegateFactory>();
                c.Add<IDelegateFactory, TDelegateFactory>();
            });

            return config;
        }

        public static NavigatorConfig UseDelegateFactory(this NavigatorConfig config, IDelegateFactory delegateFactory)
        {
            if (delegateFactory is null)
            {
                throw new ArgumentNullException(nameof(delegateFactory));
            }

            config.Configure(c =>
            {
                c.RemoveAll<IDelegateFactory>();
                c.Add(delegateFactory);
            });

            return config;
        }

        public static Navigator ToNavigator(this INavigatorConfig config)
        {
            return new Navigator(config);
        }
    }
}
