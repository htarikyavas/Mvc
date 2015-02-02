// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies metadata about precompiled views.
    /// </summary>
    public abstract class RazorFileInfoCollection
    {
        /// <summary>
        /// Gets or sets the name of the resource containing the precompiled binary.
        /// </summary>
        public string AssemblyResourceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the resource that contains the symbols (pdb).
        /// </summary>
        public string SymbolsResourceName { get; set; }

        /// <summary>
        /// Gets the <see cref="IList{T}"/> of <see cref="RazorFileInfo"/>s.
        /// </summary>
        public IList<RazorFileInfo> FileInfos { get; } = new List<RazorFileInfo>();
    }
}