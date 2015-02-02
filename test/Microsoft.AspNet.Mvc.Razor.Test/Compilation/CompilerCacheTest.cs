// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNet.FileProviders;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class CompilerCacheTest
    {
        private const string ViewPath = "view-path";

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfFileIsNotFoundInFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var type = GetType();

            // Act
            var result = cache.GetOrAdd("/some/path", _ => { throw new Exception("Shouldn't be called"); });

            // Assert
            Assert.Same(CompilerCacheResult.FileNotFound, result);
            Assert.Null(result.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_ReturnsCompilationResultFromFactory()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var type = GetType();
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            var result = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Same(expected, actual);
            Assert.Equal("hello world", actual.CompiledContent);
            Assert.Same(type, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundIfFileWasDeleted()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var type = typeof(RuntimeCompileIdentical);
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected, result1.CompilationResult);

            // Act 2
            // Delete the file from the file system and set it's expiration trigger.
            fileProvider.DeleteFile(ViewPath);
            fileProvider.GetTrigger(ViewPath).IsExpired = true;
            var result2 = cache.GetOrAdd(ViewPath, _ => { throw new Exception("shouldn't be called."); });

            // Assert 2
            Assert.Same(CompilerCacheResult.FileNotFound, result2);
            Assert.Null(result2.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_ReturnsNewResultIfFileWasModified()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var type = typeof(RuntimeCompileIdentical);
            var expected1 = UncachedCompilationResult.Successful(type, "hello world");
            var expected2 = UncachedCompilationResult.Successful(type, "different content");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected1);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected1, result1.CompilationResult);

            // Act 2
            fileProvider.GetTrigger(ViewPath).IsExpired = true;
            var result2 = cache.GetOrAdd(ViewPath, _ => expected2);

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            Assert.Same(expected2, result2.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_DoesNotQueryFileSystem_IfCachedFileTriggerWasNotSet()
        {
            // Arrange
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            fileProvider.AddFile(ViewPath, "some content");
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var type = typeof(RuntimeCompileIdentical);
            var expected = UncachedCompilationResult.Successful(type, "hello world");

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath, _ => expected);

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.Same(expected, result1.CompilationResult);

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath, _ => { throw new Exception("shouldn't be called"); });

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            Assert.IsType<CompilationResult>(result2.CompilationResult);
            Assert.Same(type, result2.CompilationResult.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());
        }

        private abstract class View
        {
            public abstract string Content { get; }
        }

        private class PreCompile : View
        {
            public override string Content { get { return "Hello World it's @DateTime.Now"; } }
        }

        private class RuntimeCompileIdentical : View
        {
            public override string Content { get { return new PreCompile().Content; } }
        }

        private class RuntimeCompileDifferent : View
        {
            public override string Content { get { return new PreCompile().Content.Substring(1) + " "; } }
        }

        private class RuntimeCompileDifferentLength : View
        {
            public override string Content
            {
                get
                {
                    return new PreCompile().Content + " longer because it was modified at runtime";
                }
            }
        }

        private static Stream GetMemoryStream(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);

            return new MemoryStream(bytes);
        }

        [Theory]
        [InlineData(10000)]
        [InlineData(11000)]
        public void GetOrAdd_UsesFilesFromCache_IfTimestampDiffers_ButContentAndLengthAreTheSame(long fileTimeUTC)
        {
            // Arrange
            var instance = new RuntimeCompileIdentical();
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileProvider = new TestFileProvider();
            var viewsProvider = GetViewsProvider(GetDefaultViewInfo());
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));
            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("Shouldn't be called."); });

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(typeof(PreCompile), actual.CompiledType);
        }

        [Theory]
        [InlineData(typeof(RuntimeCompileDifferent), 11000)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 10000)]
        [InlineData(typeof(RuntimeCompileDifferentLength), 11000)]
        public void GetOrAdd_RecompilesFile_IfContentAndLengthAreChanged(
            Type resultViewType,
            long fileTimeUTC)
        {
            // Arrange
            var instance = (View)Activator.CreateInstance(resultViewType);
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileProvider = new TestFileProvider();
            var viewsProvider = GetViewsProvider(GetDefaultViewInfo());
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = DateTime.FromFileTimeUtc(fileTimeUTC),
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => CompilationResult.Successful(resultViewType));

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(resultViewType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_UsesValueFromCache_IfViewStartHasNotChanged()
        {
            // Arrange
            var instance = (View)Activator.CreateInstance(typeof(PreCompile));
            var length = Encoding.UTF8.GetByteCount(instance.Content);
            var fileProvider = new TestFileProvider();

            var lastModified = DateTime.UtcNow;

            var fileInfo = new TestFileInfo
            {
                Length = length,
                LastModified = lastModified,
                Content = instance.Content
            };
            fileProvider.AddFile(ViewPath, fileInfo);

            var viewStartContent = "viewstart-content";
            var viewStartFileInfo = new TestFileInfo
            {
                Content = viewStartContent,
                LastModified = DateTime.UtcNow
            };
            fileProvider.AddFile("_ViewStart.cshtml", viewStartFileInfo);
            var viewStartRazorFileInfo = new RazorFileInfo
            {
                Hash = Crc32.Calculate(GetMemoryStream(viewStartContent)).ToString(CultureInfo.InvariantCulture),
                HashAlgorithmVersion = 1,
                LastModified = viewStartFileInfo.LastModified,
                Length = viewStartFileInfo.Length,
                RelativePath = "_ViewStart.cshtml",
                FullTypeName = typeof(RuntimeCompileIdentical).FullName
            };
            var viewStartViewInfo = new PrecompiledViewInfo(viewStartRazorFileInfo, typeof(RuntimeCompileIdentical));
            var viewsProvider = GetViewsProvider(GetDefaultViewInfo(), viewStartViewInfo);
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(typeof(PreCompile), actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_ReturnsFileNotFoundResult_IfPrecompiledViewWasRemovedFromFileSystem()
        {
            // Arrange
            var fileProvider = new TestFileProvider();
            var viewsProvider = GetViewsProvider(GetDefaultViewInfo());
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            // Act
            var result = cache.GetOrAdd(ViewPath,
                                        compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert
            Assert.Same(CompilerCacheResult.FileNotFound, result);
            Assert.Null(result.CompilationResult);
        }

        [Fact]
        public void GetOrAdd_DoesNotReadFileFromFileSystemAfterPrecompiledViewIsVerified()
        {
            // Arrange
            var mockFileProvider = new Mock<TestFileProvider> { CallBase = true };
            var fileProvider = mockFileProvider.Object;
            var viewInfo = GetDefaultViewInfo();
            var fileInfo = new TestFileInfo
            {
                Length = viewInfo.RazorFileInfo.Length,
                LastModified = viewInfo.RazorFileInfo.LastModified,
            };
            fileProvider.AddFile(ViewPath, fileInfo);
            var viewsProvider = GetViewsProvider(viewInfo);
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            // Act 1
            var result1 = cache.GetOrAdd(ViewPath,
                                         compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());

            // Act 2
            var result2 = cache.GetOrAdd(ViewPath,
                                         compile: _ => { throw new Exception("shouldn't be invoked"); });

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(typeof(PreCompile), actual2.CompiledType);
            mockFileProvider.Verify(v => v.GetFileInfo(ViewPath), Times.Once());
        }

        [Fact]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButViewStartWasAdedSinceTheCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var fileProvider = new TestFileProvider();
            var precompiledView = GetDefaultViewInfo();
            precompiledView.RazorFileInfo.RelativePath = "Views\\home\\index.cshtml";
            var viewsProvider = GetViewsProvider(precompiledView);
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));
            var testFile = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledView.RazorFileInfo.LastModified,
                PhysicalPath = precompiledView.RazorFileInfo.RelativePath
            };
            fileProvider.AddFile(precompiledView.RazorFileInfo.RelativePath, testFile);
            var relativeFile = new RelativeFileInfo(testFile, testFile.PhysicalPath);

            // Act 1
            var result1 = cache.GetOrAdd(testFile.PhysicalPath,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            var viewStartTrigger = fileProvider.GetTrigger("Views\\_ViewStart.cshtml");
            viewStartTrigger.IsExpired = true;
            var result2 = cache.GetOrAdd(testFile.PhysicalPath,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        [Fact]
        public void GetOrAdd_IgnoresCachedValueIfFileIsIdentical_ButViewStartWasDeletedSinceCacheWasCreated()
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var fileProvider = new TestFileProvider();

            var precompiledView = GetDefaultViewInfo();
            precompiledView.RazorFileInfo.RelativePath = "Views\\Index.cshtml";
            var viewFileInfo = new TestFileInfo
            {
                Content = new PreCompile().Content,
                LastModified = precompiledView.RazorFileInfo.LastModified,
                PhysicalPath = precompiledView.RazorFileInfo.RelativePath
            };
            fileProvider.AddFile(viewFileInfo.PhysicalPath, viewFileInfo);

            var viewStartFileInfo = new TestFileInfo
            {
                PhysicalPath = "Views\\_ViewStart.cshtml",
                Content = "viewstart-content",
                LastModified = lastModified
            };
            var viewStart = new RazorFileInfo
            {
                FullTypeName = typeof(RuntimeCompileIdentical).FullName,
                RelativePath = viewStartFileInfo.PhysicalPath,
                LastModified = viewStartFileInfo.LastModified,
                Hash = RazorFileHash.GetHash(viewStartFileInfo, hashAlgorithmVersion: 1),
                HashAlgorithmVersion = 1,
                Length = viewStartFileInfo.Length
            };
            fileProvider.AddFile(viewStartFileInfo.PhysicalPath, viewStartFileInfo);

            var viewStartInfo = new PrecompiledViewInfo(viewStart, typeof(RuntimeCompileIdentical));
            var viewsProvider = GetViewsProvider(precompiledView, viewStartInfo);
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            // Act 1
            var result1 = cache.GetOrAdd(viewFileInfo.PhysicalPath,
                                        compile: _ => { throw new Exception("should not be called"); });

            // Assert 1
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            var actual1 = result1.CompilationResult;
            Assert.NotNull(actual1);
            Assert.Equal(typeof(PreCompile), actual1.CompiledType);

            // Act 2
            var trigger = fileProvider.GetTrigger(viewStartFileInfo.PhysicalPath);
            trigger.IsExpired = true;
            var result2 = cache.GetOrAdd(viewFileInfo.PhysicalPath,
                                         compile: _ => CompilationResult.Successful(expectedType));

            // Assert 2
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);
            var actual2 = result2.CompilationResult;
            Assert.NotNull(actual2);
            Assert.Equal(expectedType, actual2.CompiledType);
        }

        public static IEnumerable<object[]> GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreatedData
        {
            get
            {
                var viewStartContent = "viewstart-content";
                var contentStream = GetMemoryStream(viewStartContent);
                var lastModified = DateTime.UtcNow;
                int length = Encoding.UTF8.GetByteCount(viewStartContent);
                var path = "Views\\_ViewStart.cshtml";

                var razorFileInfo = new RazorFileInfo
                {
                    Hash = Crc32.Calculate(contentStream).ToString(CultureInfo.InvariantCulture),
                    HashAlgorithmVersion = 1,
                    LastModified = lastModified,
                    Length = length,
                    RelativePath = path
                };

                // Length does not match
                var testFileInfo1 = new TestFileInfo
                {
                    Length = 7732
                };

                yield return new object[] { razorFileInfo, testFileInfo1 };

                // Content and last modified do not match
                var testFileInfo2 = new TestFileInfo
                {
                    Length = length,
                    Content = "viewstart-modified",
                    LastModified = lastModified.AddSeconds(100),
                };

                yield return new object[] { razorFileInfo, testFileInfo2 };
            }
        }

        [Theory]
        [MemberData(nameof(GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreatedData))]
        public void GetOrAdd_IgnoresCachedValue_IfViewStartWasChangedSinceCacheWasCreated(
            RazorFileInfo viewStartRazorFileInfo, TestFileInfo viewStartFileInfo)
        {
            // Arrange
            var expectedType = typeof(RuntimeCompileDifferent);
            var lastModified = DateTime.UtcNow;
            var viewStartLastModified = DateTime.UtcNow;
            var content = "some content";
            var fileInfo = new TestFileInfo
            {
                Length = 1020,
                Content = content,
                LastModified = lastModified,
                PhysicalPath = "Views\\home\\index.cshtml"
            };

            var fileProvider = new TestFileProvider();
            fileProvider.AddFile(fileInfo.PhysicalPath, fileInfo);
            fileProvider.AddFile(viewStartRazorFileInfo.RelativePath, viewStartFileInfo);
            var viewsProvider = GetViewsProvider(GetDefaultViewInfo());
            var cache = new CompilerCache(viewsProvider, GetOptionsAccessor(fileProvider));

            // Act
            var result = cache.GetOrAdd(fileInfo.PhysicalPath,
                                        compile: _ => CompilationResult.Successful(expectedType));

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result);
            var actual = result.CompilationResult;
            Assert.NotNull(actual);
            Assert.Equal(expectedType, actual.CompiledType);
        }

        [Fact]
        public void GetOrAdd_DoesNotCacheCompiledContent_OnCallsAfterInitial()
        {
            // Arrange
            var lastModified = DateTime.UtcNow;
            var fileProvider = new TestFileProvider();
            var cache = new CompilerCache(GetViewsProvider(), GetOptionsAccessor(fileProvider));
            var fileInfo = new TestFileInfo
            {
                PhysicalPath = "test",
                LastModified = lastModified
            };
            fileProvider.AddFile("test", fileInfo);
            var type = GetType();
            var uncachedResult = UncachedCompilationResult.Successful(type, "hello world");

            // Act
            cache.GetOrAdd("test", _ => uncachedResult);
            var result1 = cache.GetOrAdd("test", _ => uncachedResult);
            var result2 = cache.GetOrAdd("test", _ => uncachedResult);

            // Assert
            Assert.NotSame(CompilerCacheResult.FileNotFound, result1);
            Assert.NotSame(CompilerCacheResult.FileNotFound, result2);

            var actual1 = result1.CompilationResult;
            var actual2 = result2.CompilationResult;
            Assert.NotSame(uncachedResult, actual1);
            Assert.NotSame(uncachedResult, actual2);
            var result = Assert.IsType<CompilationResult>(actual1);
            Assert.Null(actual1.CompiledContent);
            Assert.Same(type, actual1.CompiledType);

            result = Assert.IsType<CompilationResult>(actual2);
            Assert.Null(actual2.CompiledContent);
            Assert.Same(type, actual2.CompiledType);
        }

        private static IOptions<RazorViewEngineOptions> GetOptionsAccessor(IFileProvider provider)
        {
            var options = new RazorViewEngineOptions
            {
                FileProvider = provider
            };

            var optionsAccessor = new Mock<IOptions<RazorViewEngineOptions>>();
            optionsAccessor.SetupGet(a => a.Options)
                           .Returns(options);

            return optionsAccessor.Object;
        }

        private static IPrecompiledViewsProvider GetViewsProvider(params PrecompiledViewInfo[] precompiledViews)
        {
            var provider = new Mock<IPrecompiledViewsProvider>();
            provider.SetupGet(p => p.PrecompiledViews)
                    .Returns(precompiledViews ?? Enumerable.Empty<PrecompiledViewInfo>());

            return provider.Object;
        }

        private static PrecompiledViewInfo GetDefaultViewInfo()
        {
            var content = new PreCompile().Content;
            var length = Encoding.UTF8.GetByteCount(content);

            var fileInfo = new RazorFileInfo
            {
                FullTypeName = typeof(PreCompile).FullName,
                Hash = Crc32.Calculate(GetMemoryStream(content)).ToString(CultureInfo.InvariantCulture),
                HashAlgorithmVersion = 1,
                LastModified = DateTime.FromFileTimeUtc(10000),
                Length = length,
                RelativePath = ViewPath,
            };

            return new PrecompiledViewInfo(fileInfo, typeof(PreCompile));
        }
    }
}