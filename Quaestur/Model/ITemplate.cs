using System;
using SiteLibrary;

namespace Quaestur
{
    public interface ITemplate
    {
        Organization Organization { get; }
        TemplateAssignmentType AssignmentType { get; }
        Language Language { get; }
        Guid Id { get; }
        string Label { get; }
    }
}
