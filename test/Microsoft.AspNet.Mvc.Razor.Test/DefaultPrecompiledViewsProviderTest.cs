// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class DefaultPrecompiledViewsProviderTest
    {
        [Fact]
        public void Match_ReturnsFalse_IfTypeIsAbstract()
        {
            // Arrange
            var type = typeof(AbstractRazorFileInfoCollection);

            // Act
            var result = DefaultPrecompiledViewsProvider.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeHasGenericParameters()
        {
            // Arrange
            var type = typeof(GenericRazorFileInfoCollection<>);

            // Act
            var result = DefaultPrecompiledViewsProvider.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeDoesNotHaveDefaultConstructor()
        {
            // Arrange
            var type = typeof(ParameterConstructorRazorFileInfoCollection);

            // Act
            var result = DefaultPrecompiledViewsProvider.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsFalse_IfTypeDoesNotDeriveFromRazorFileInfoCollection()
        {
            // Arrange
            var type = typeof(NonSubTypeRazorFileInfoCollection);

            // Act
            var result = DefaultPrecompiledViewsProvider.Match(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Match_ReturnsTrue_IfTypeDerivesFromRazorFileInfoCollection()
        {
            // Arrange
            var type = typeof(SubTypeRazorFileInfoCollection);

            // Act
            var result = DefaultPrecompiledViewsProvider.Match(type);

            // Assert
            Assert.True(result);
        }

        private abstract class AbstractRazorFileInfoCollection : RazorFileInfoCollection
        {

        }

        private class GenericRazorFileInfoCollection<TVal> : RazorFileInfoCollection
        {

        }

        private class ParameterConstructorRazorFileInfoCollection :RazorFileInfoCollection
        {
            public ParameterConstructorRazorFileInfoCollection(string value)
            {
            }
        }

        private class NonSubTypeRazorFileInfoCollection : Controller
        {

        }

        private class SubTypeRazorFileInfoCollection : RazorFileInfoCollection
        {

        }
    }
}