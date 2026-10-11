using System;

namespace GeminiSwAddin
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class SwAddinAttribute : Attribute
    {
        public string Description { get; set; }
        public string Title { get; set; }
    }
}
