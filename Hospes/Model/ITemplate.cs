using System;
using SiteLibrary;

namespace Hospes
{
    public interface ITemplate
    {
        Organization Organization { get; }
        TemplateAssignmentType AssignmentType { get; }
        Language Language { get; }
        string Label { get; }
    }
}
