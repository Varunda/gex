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

        public class DateTimeHandler : SqlMapper.TypeHandler<DateTime> {

            public override DateTime Parse(object value) {
                if (value is not long l) {
                    throw new InvalidCastException($"needed type of value to be a long, got a '{value.GetType().Name}' instead");
                }

                return DateTimeOffset.FromUnixTimeMilliseconds(l).UtcDateTime;
            }

            public override void SetValue(IDbDataParameter parameter, DateTime value) {
                throw new NotImplementedException();
            }

        }

    }
}
