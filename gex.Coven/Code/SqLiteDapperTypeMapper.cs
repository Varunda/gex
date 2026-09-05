using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Coven.Code {

    public class SqLiteDapperTypeMapper {

        public class JsonElementHandler : SqlMapper.TypeHandler<JsonElement> {

            public override JsonElement Parse(object value) {
                return JsonElement.Parse("{}");
            }

            public override void SetValue(IDbDataParameter parameter, JsonElement value) {
                throw new NotImplementedException();
            }

        }

    }
}
