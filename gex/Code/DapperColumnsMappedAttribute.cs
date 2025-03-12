using System;

namespace gex.Code {

    /// <summary>
    ///     attribute to mark that a class uses <see cref="Dapper.ColumnMapper.ColumnMappingAttribute"/>,
    ///     allowing for automatic mapping during startup
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DapperColumnsMappedAttribute : Attribute { }

}
