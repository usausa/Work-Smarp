﻿namespace Smart.Navigation
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class GroupAttribute : Attribute
    {
        public object Id { get; }

        public GroupAttribute(object id)
        {
            Id = id;
        }
    }
}
