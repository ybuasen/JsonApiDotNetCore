using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Resources;

namespace JsonApiDotNetCore.QueryStrings
{
    /// <summary>
    /// Reads custom query string parameters for which handlers on <see cref="ResourceDefinition{TResource}"/> are registered
    /// and produces a set of query constraints from it.
    /// </summary>
    public interface IResourceDefinitionQueryableParameterReader : IQueryStringParameterReader, IQueryConstraintProvider
    {
    }
}
