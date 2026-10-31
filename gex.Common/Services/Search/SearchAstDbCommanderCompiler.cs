using gex.Common.Code;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Models.Search;
using gex.Common.Services.Repository.Match;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Search {

    public class SearchAstDbCommanderCompiler {

        private readonly ILogger<SearchAstDbCommanderCompiler> _Logger;

        private readonly BarMatchPlayerRepository _PlayerRepository;

        public SearchAstDbCommanderCompiler(ILogger<SearchAstDbCommanderCompiler> logger,
            BarMatchPlayerRepository playerRepository) {

            _Logger = logger;
            _PlayerRepository = playerRepository;
        }

        public async Task<Result<DbCommand, string>> Compile(SearchAst ast, DbConnection conn, long? currentUserID, bool isDev, CancellationToken cancel) {
            if (conn.State != System.Data.ConnectionState.Open) {
                await conn.OpenAsync(cancel);
            }

            DbCommand sqlCmd = conn.CreateCommand();
            sqlCmd.CommandType = System.Data.CommandType.Text;

            string cmd = $@"
                SELECT
                    *
                FROM (
                    SELECT DISTINCT ON (m.id) *
                        FROM bar_match m
                            LEFT JOIN match_pool_entry mpe ON mpe.match_id = m.id
                            LEFT JOIN match_pool mpool ON mpe.pool_id = mpool.id
            ";

            QuerySetup query = new();

            Result<Maybe<string>, string> compiled = await _Compile(ast.Root, query, null, cancel);
            if (compiled.IsOk == false) {
                return Result<DbCommand, string>.Err(compiled.Error);
            }

            if (compiled.Value.Has() == false) {
                throw new InvalidOperationException($"expected compiled root to have a value");
            }

            cmd += string.Join(" \n ", query.Joins);
            cmd += "WHERE 1=1 AND \n";

            if (isDev == false) {
                string cond = $"(mpool.hide_until IS NULL OR mpool.hide_until <= NOW() at time zone 'utc' ";
                if (currentUserID != null) {
                    cond += $" OR mpool.created_by_id = {addParameter(query, currentUserID.Value)} ";
                }

                cond += ")\n AND\n";
                cmd += cond;
            }
            cmd += compiled.Value.Get();

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////
            // no more WHERE clause stuff after this
            foreach (object? param in query.Parameters) {
                sqlCmd.AddParameter(param);
            }

            cmd += ") sub_query";

            cmd += $" ORDER BY {query.OrderBy.Value} {query.OrderByDirection.Value} \n";
            cmd += $" OFFSET {query.Offset} \n";
            cmd += $" LIMIT {query.Limit} \n";

            sqlCmd.CommandText = cmd;
            return sqlCmd;
        }

        private async Task<Result<Maybe<string>, string>> _Compile(SearchNode node, QuerySetup query, string? fieldPrefix, CancellationToken cancel) {

            string? cmd = null;

            // AND // OR
            if (node.Type == SearchNodeType.AND || node.Type == SearchNodeType.OR) {
                cmd = " ( ";
                List<string> childCmds = [];

                foreach (SearchNode child in node.Children) {
                    Result<Maybe<string>, string> cc = await _Compile(child, query, fieldPrefix, cancel);
                    if (cc.IsOk == false) {
                        return Result<Maybe<string>, string>.Err(cc.Error);
                    }

                    if (cc.Value.Has() == true) {
                        childCmds.Add(cc.Value.Get());
                    }
                }

                if (node.Type == SearchNodeType.AND) {
                    if (childCmds.Count == 0) {
                        childCmds.Add(" 1 = 1 ");
                    }

                    cmd += string.Join(" AND ", childCmds);
                } else {
                    cmd += string.Join(" OR ", childCmds);
                }

                cmd += ")\n";
            }

            // BASIC_FIELD
            else if (node.Type == SearchNodeType.BASIC_FIELD) {
                if (node.Children.Count != 3) {
                    return $"FIELD expected to have 3 children, had {node.Children.Count} instead";
                }

                string fieldName = node.Children[0].Token.Value;
                if (fieldPrefix != null) {
                    fieldName = $"{fieldPrefix}.{fieldName}";
                }

                if (FieldNameAliases.ContainsKey(fieldName)) {
                    fieldName = FieldNameAliases.GetValueOrDefault(fieldName)
                        ?? throw new InvalidOperationException($"expected alias to exist");
                }

                if (FieldNameMappings.ContainsKey(fieldName) == false) {
                    return $"invalid field name '{fieldName}'";
                }

                SearchNode fieldOp = node.Children[1];
                SearchNode fieldValue = node.Children[2];

                // sort | order
                if (fieldName == "sort" || fieldName == "order") {
                    if (fieldOp.Token.Value != "=") {
                        return $"for a 'sort'|'order' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'sort'|'order' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.OrderBy = BarMatchSearchParameters.ParseOrderBy(fieldValue.Token.Value)
                        ?? throw new ArgumentException($"invalid OrderBy value '{fieldValue.Token.Value}'");
                }

                // order_dir | sort_dir
                else if (fieldName == "order_dir" || fieldName == "sort_dir") {
                    if (fieldOp.Token.Value != "=") {
                        return $"for a 'sort_dir'|'order_dir' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'sort_dir'|'order_dir' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.OrderByDirection = BarMatchSearchParameters.ParseOrderByDirection(fieldValue.Token.Value)
                        ?? throw new ArgumentException($"invalid OrderByDirection value '{fieldValue.Token.Value}'");
                }

                // limit
                else if (fieldName == "limit") {
                    if (fieldOp.Token.Value != "=") {
                        return $"for a 'limit' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'limit' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.Limit = uint.Parse(fieldValue.Token.Value);

                    if (query.Limit == 0) {
                        throw new ArgumentException($"limit must be greater than 0");
                    }
                    if (query.Limit > 1000) {
                        throw new ArgumentException($"limit must be less than or equal to 1000");
                    }
                }

                // offset | start
                else if (fieldName == "offset" || fieldName == "start") {
                    if (fieldOp.Token.Value != "=") {
                        return $"for a 'offset' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'offset' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.Offset = uint.Parse(fieldValue.Token.Value);

                    if (query.Offset > 100000) {
                        throw new ArgumentException($"offset must be 100000 or less");
                    }
                }

                // replayed
                else if (fieldName == "replayed") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);

                    cmd += @$" m.id IN (
                        SELECT game_id
                        FROM bar_match_processing
                        WHERE actions_ms IS {(value == true ? "NOT NULL" : "NULL")}
                    ";
                }

                // ranked
                else if (fieldName == "ranked") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);
                    cmd += $" m.game_settings->>'ranked_game' = {(value == true ? '1' : '0')}\n";
                }

                // player.name
                else if (fieldName == "player.user_name") {
                    if (query.PlayerLookupCache.TryGetValue(fieldValue.Token.Value, out List<BarMatchPlayer>? players) == false || players == null) {
                        players = await _PlayerRepository.SearchByName(fieldValue.Token.Value, cancel);
                        query.PlayerLookupCache.Add(fieldValue.Token.Value, players);
                    }

                    cmd += $" player.user_id IN (";
                    
                    foreach (BarMatchPlayer player in players) {
                        cmd += $" {addParameter(query, player.UserID)} ";
                    }

                    cmd += ")\n";
                }

                // fallback
                else {
                    Type fieldType = FieldNameMappings.GetValueOrDefault(fieldName)
                        ?? throw new InvalidOperationException($"expected type mapping for fieldName '{fieldName}' to exist");

                    object value = parseValue(fieldType, fieldValue.Token.Value, fieldName);
                    cmd += $" {fieldName} {fieldOp.Token.Value} {addParameter(query, value)}";
                }
            }

            // COMPLEX_FIELD
            else if (node.Type == SearchNodeType.COMPLEX_FIELD) {
                if (node.Children.Count != 3) {
                    return $"FIELD expected to have 3 children, had {node.Children.Count} instead";
                }

                string fieldName = FieldNameAliases.GetValueOrDefault(node.Children[0].Token.Value) ?? node.Children[0].Token.Value;
                SearchNode fieldOp = node.Children[1];
                SearchNode fieldValue = node.Children[2];

                string? prefix = null;

                if (fieldName == "player") {
                    prefix = "player";

                    cmd += @$" m.id IN (
                        SELECT distinct(player.game_id) as game_id 
                        FROM bar_match_player player
                            INNER JOIN bar_match_team team ON player.game_id = team.game_id AND player.team_id = team.team_id
                        WHERE
                    ";
                }

                // map
                else if (fieldName == "map") {
                    prefix = "map";

                    cmd += @" m.map_name IN (
                        SELECT distinct(filename)
                        FROM bar_map map
                        WHERE
                    ";
                }

                // fallback
                else {
                    return $"invalid complex field: '{fieldName}'";
                }

                if (fieldValue.Children.Count <= 1) {
                    SearchNode basicField;

                    if (fieldValue.Children.Count == 0) {
                        if (ComplexFieldImplicitField.ContainsKey(fieldName) == false) {
                            return $"missing implicit field name for complex field '{fieldName}'";
                        }

                        SearchToken nameToken = new(SearchTokenType.WORD, ComplexFieldImplicitField.GetValueOrDefault(fieldName)!);

                        basicField = new(SearchNodeType.BASIC_FIELD, node.Token, null);
                        basicField.Children.Add(new SearchNode(SearchNodeType.FIELD_NAME, nameToken, null));
                        basicField.Children.Add(fieldOp);
                        basicField.Children.Add(fieldValue);
                    } else {
                        basicField = fieldValue.Children[0];
                    }

                    Result<Maybe<string>, string> cc = await _Compile(basicField, query, prefix, cancel);
                    if (cc.IsOk == false) {
                        return cc.Error;
                    }

                    if (cc.Value.Has() == true) {
                        cmd += cc.Value.Get();
                    }

                    cmd += " \n";
                } else {
                    throw new InvalidOperationException($"expected 0 or 1 children in {fieldName}, got {fieldValue.Children.Count} instead");
                }

                cmd += " ) \n"; // close the sub-query
            }

            // fallback
            else {
                throw new NotImplementedException($"unhandled node type '{node.Type}' (from {node})");
            }

            return Result<Maybe<string>, string>.Ok(cmd == null ? Maybe<string>.None() : Maybe<string>.Some(cmd));
        }

        /// <summary>
        ///     add a DB parameter to the query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string addParameter(QuerySetup query, object? value) {
            query.Parameters.Add(value);
            return $"${query.Parameters.Count}";
        }

        private static object parseValue(Type fieldType, string input, string fieldName) {
            object value;

            if (fieldType == typeof(string)) {
                value = input;
            } else if (fieldType == typeof(int)) {
                value = int.Parse(input);
            } else if (fieldType == typeof(long)) {
                value = long.Parse(input);
            } else if (fieldType == typeof(uint)) {
                value = uint.Parse(input);
            } else if (fieldType == typeof(double)) {
                value = double.Parse(input);
            } else if (fieldType == typeof(bool)) {
                string v = input.ToLower();
                if (v == "1" || v == "y" || v == "yes" || v == "true" || v == "t") {
                    value = true;
                } else if (v == "0" || v == "n" || v == "no" || v == "false" || v == "f") {
                    value = false;
                } else {
                    throw new FormatException($"invalid boolean value {v}");
                }
            } else if (fieldType == typeof(TimeSpan)) {
                value = DurationStringUtil.Parse(input);
            } else {
                throw new InvalidOperationException($"unhandled type ({fieldType.FullName}) for field ({fieldName})");
            }

            return value;
        }

        /// <summary>
        ///     maps field names and their data types to parse the string value into
        /// </summary>
        private static Dictionary<string, Type> FieldNameMappings = new Dictionary<string, Type>() {

            // meta
            { "sort", typeof(string) },
            { "order", typeof(string) },
            { "sort_dir", typeof(string) },
            { "order_dir", typeof(string) },
            { "limit", typeof(uint) },
            { "offset", typeof(uint) },
            { "start", typeof(uint) },

            // match fields
            { "ranked", typeof(bool) },
            { "duration", typeof(TimeSpan) },
            { "average_os", typeof(double) },
            { "min_os", typeof(double) },
            { "max_os", typeof(double) },
            { "start_time", typeof(DateTime) },
            { "engine", typeof(string) },
            { "game_version", typeof(string) },
            { "gamemode", typeof(string) },
            { "uploaded_by", typeof(long) },
            { "player_count", typeof(int) },

            // player fields
            { "player.user_name", typeof(string) },
            { "team.start_spot_label", typeof(string) },
            { "team.faction", typeof(string) },
            { "player.skill", typeof(string) },
            { "player.skill_uncertainity", typeof(string) },

            // map fields
            { "map.name", typeof(string) },
            { "map.width", typeof(int) },
            { "map.height", typeof(int) },
            { "map.minimum_wind", typeof(int) },
            { "map.maximum_wind", typeof(int) },
            { "map.tidal_strength", typeof(int) },
            { "map.author", typeof(string) },
            { "map.extractor_radius", typeof(int) },
        };

        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<string, string> FieldNameAliases = new Dictionary<string, string>() {
            // player fields
            { "player.position", "team.start_spot_label" },
            { "player.faction", "team.faction" },
            { "player.name", "player.user_name" },

            // map fields
            { "map.min_wind", "map.minimum_wind" },
            { "ma.max_wind", "map.maximum_wind" },
            { "map.tidal", "map.tidal_strength" },
        };

        /// <summary>
        ///     maps complex field names to the implicit field used
        /// </summary>
        private static Dictionary<string, string> ComplexFieldImplicitField = new() {
            { "player", "user_name" },
            { "map", "name" }
        };

        private class QuerySetup {

            public List<object?> Parameters { get; set; } = [];

            public List<string> Joins { get; set; } = [];

            public HashSet<string> Joined { get; set; } = [];

            public OrderBy OrderBy { get; set; } = OrderBy.START_TIME;

            public OrderByDirection OrderByDirection { get; set; } = OrderByDirection.DESC;

            public uint Offset { get; set; } = 0;

            public uint Limit { get; set; } = 24;

            public Dictionary<string, List<BarMatchPlayer>> PlayerLookupCache = [];

        }


    }
}
