// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using ModelBindingWebSite.Models;

namespace ModelBindingWebSite.Controllers
{
    public class BindModelController : Controller
    {
        public Address2 CollectionType(Address2 address)
        {
            return address;
        }

        public UserWithAddress NestedCollectionType(UserWithAddress user)
        {
            return user;
        }

        public PeopleModel NestedCollectionOfRecursiveTypes(PeopleModel model)
        {
            return model;
        }

        public bool PostCheckBox(bool isValid)
        {
            return isValid;
        }
    }
}