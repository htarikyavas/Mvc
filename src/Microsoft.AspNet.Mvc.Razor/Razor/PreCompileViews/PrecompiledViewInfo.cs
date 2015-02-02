// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the result of <see cref="IPrecompiledViewsProvider"/>.
    /// </summary>
    public class PrecompiledViewInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PrecompiledViewInfo"/>.
        /// </summary>
        /// <param name="razorFileInfo">The <see cref="Razor.RazorFileInfo"/> for the precompiled file.</param>
        /// <param name="compiledType">The compiled <see cref="Type"/>.</param>
        public PrecompiledViewInfo([NotNull] RazorFileInfo razorFileInfo, [NotNull] Type compiledType)
        {
            RazorFileInfo = razorFileInfo;
            CompiledType = compiledType;
        }

        /// <summary>
        /// Gets the <see cref="Razor.RazorFileInfo"/> for the precompiled file.
        /// </summary>
        public RazorFileInfo RazorFileInfo { get; }

        /// <summary>
        /// Gets the compiled <see cref="Type"/>.
        /// </summary>
        public Type CompiledType { get; }
    }
}