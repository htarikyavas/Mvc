// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Default implementation for <see cref="IPrecompiledViewsProvider"/>.
    /// </summary>
    public class DefaultPrecompiledViewsProvider : IPrecompiledViewsProvider
    {
        private static readonly TypeInfo RazorFileInfoCollectionType = typeof(RazorFileInfoCollection).GetTypeInfo();
        private readonly IAssemblyProvider _assemblyProvider;
        private readonly IAssemblyLoadContext _loadContext;

        /// <summary>
        /// Initializes a new instance of <see cref="DefaultPrecompiledViewsProvider"/>.
        /// </summary>
        /// <param name="loaderContextAccessor">The <see cref="IAssemblyLoadContextAccessor"/>.</param>
        /// <param name="assemblyProvider">The <see cref="IAssemblyProvider"/> that provides assemblies
        /// for precompiled view discovery.</param>
        public DefaultPrecompiledViewsProvider(IAssemblyLoadContextAccessor loaderContextAccessor,
                                               IAssemblyProvider assemblyProvider)
        {
            _loadContext = loaderContextAccessor.GetLoadContext(RazorFileInfoCollectionType.Assembly);
            _assemblyProvider = assemblyProvider;
        }

        /// <inheritdoc />
        public IEnumerable<PrecompiledViewInfo> PrecompiledViews
        {
            get
            {
                var precompiledViews = new List<PrecompiledViewInfo>();
                var razorFileInfoCollections = GetRazorFileInfoCollections();
                foreach (var viewCollection in razorFileInfoCollections)
                {
                    var viewAssembly = LoadPrecompiledViewsAssembly(viewCollection);
                    foreach (var fileInfo in viewCollection.FileInfos)
                    {
                        var viewType = viewAssembly.GetType(fileInfo.FullTypeName);
                        precompiledViews.Add(new PrecompiledViewInfo(fileInfo, viewType));
                    }
                }

                return precompiledViews;
            }
        }

        private IEnumerable<RazorFileInfoCollection> GetRazorFileInfoCollections()
        {
            return _assemblyProvider.CandidateAssemblies
                                    .SelectMany(a => a.ExportedTypes)
                                    .Where(Match)
                                    .Select(c => (RazorFileInfoCollection)Activator.CreateInstance(c));
        }

        // Internal for unit testing.
        internal static bool Match(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (RazorFileInfoCollectionType.IsAssignableFrom(typeInfo))
            {
                var hasParameterlessConstructor = type.GetConstructor(Type.EmptyTypes) != null;

                return hasParameterlessConstructor &&
                       !typeInfo.IsAbstract &&
                       !typeInfo.ContainsGenericParameters;
            }

            return false;
        }

        private Assembly LoadPrecompiledViewsAssembly(RazorFileInfoCollection viewCollection)
        {
            var viewCollectionAssembly = viewCollection.GetType().GetTypeInfo().Assembly;
            using (var assemblyStream = viewCollectionAssembly.GetManifestResourceStream(
                viewCollection.AssemblyResourceName))
            {
                Stream symbolsStream = null;
                if (!string.IsNullOrEmpty(viewCollection.SymbolsResourceName))
                {
                    symbolsStream = viewCollectionAssembly.GetManifestResourceStream(
                        viewCollection.SymbolsResourceName);
                }

                using (symbolsStream)
                {
                    return _loadContext.LoadStream(assemblyStream, symbolsStream);
                }
            }
        }
    }
}