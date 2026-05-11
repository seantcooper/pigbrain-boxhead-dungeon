using System;
using System.Collections;
using System.Collections.Generic;

namespace pigbrain.core.Project
{
    public static class ProjectInterface
    {
        public enum Filter { Project, Hierarchy }

        [AttributeUsage(AttributeTargets.Class, Inherited = true)]
        public class ControlAttribute : Attribute
        {
            public Filter filter;
            public int priority;
            public bool group;
            public ControlAttribute(Filter filter) => this.filter = filter;
            public ControlAttribute(Filter filter, int priority) : this(filter) => this.priority = priority;
            public ControlAttribute(Filter filter, bool group, int priority = 0) : this(filter, priority) => this.group = group;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = true)]
        public class ButtonAttribute : Attribute
        {
            public string name;
            public bool validation;
            public Filter filter;
            public ButtonAttribute(string name, bool validation = false)
            {
                this.name = name;
                this.validation = validation;
            }
        }
    }

    public interface IBuildStripper
    {
        IEnumerable<UnityEngine.Object> Prebuild();
        void Postbuild(UnityEngine.Object[] objects);
    }
}