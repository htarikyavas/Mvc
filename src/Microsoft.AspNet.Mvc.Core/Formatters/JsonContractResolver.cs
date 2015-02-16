// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// The default <see cref="IContractResolver"/> for <see cref="JsonInputFormatter"/> and 
    /// <see cref="JsonOutputFormatter"/>. 
    /// It determines if a member has <see cref="RequiredAttribute"/> and sets the appropriate 
    /// JsonProperty settings.
    /// </summary>
    public class JsonContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonContractResolver"/>.
        /// </summary>
        public JsonContractResolver()
        {
#if ASPNET50
            IgnoreSerializableAttribute = true;
#endif
        }

        // Determines whether a member has "RequiredAttribute" from data annotations and sets the appropriate 
        // JsonProperty settings.
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property =  base.CreateProperty(member, memberSerialization);

            var required = member.GetCustomAttribute(typeof(RequiredAttribute), inherit: true);
            if (required != null)
            {
                property.Required = Required.AllowNull;
                property.DefaultValueHandling = DefaultValueHandling.Include;
                property.NullValueHandling = NullValueHandling.Include;
            }
            
            return property;
        }
    }
}