using gex.Common.Code;
using gex.Common.Code.Exceptions;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Match;
using gex.Common.Models.Search;
using gex.Common.Models.User;
using gex.Common.Services.Repository;
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
        private readonly BarUserRepository _UserRepository;
        private readonly BarMapRepository _MapRepository;

        public SearchAstDbCommanderCompiler(ILogger<SearchAstDbCommanderCompiler> logger,
            BarMatchPlayerRepository playerRepository, BarMapRepository mapRepository,
            BarUserRepository userRepository) {

            _Logger = logger;
            _PlayerRepository = playerRepository;
            _MapRepository = mapRepository;
            _UserRepository = userRepository;
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
                    cond += $" OR mpool.created_by_id = {addParameter(query, currentUserID.Value, null)} ";
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

        /// <summary>
        ///     compile a node, and all its children
        /// </summary>
        /// <param name="node">node to be compiled</param>
        /// <param name="query">query being built</param>
        /// <param name="fieldPrefix">optional field prefix, used for complex fields</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns>
        ///     
        /// </returns>
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

                string fieldName = node.Children[0].Token.Value.ToLower();
                if (fieldPrefix != null) {
                    fieldName = $"{fieldPrefix}.{fieldName}".ToLower();
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
                    if (fieldOp.Token.Value != SearchOp.EQ) {
                        return $"for a 'sort'|'order' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'sort'|'order' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.OrderBy = BarMatchSearchParameters.ParseOrderBy(fieldValue.Token.Value)
                        ?? throw new SearchAstArgumentException($"invalid OrderBy value '{fieldValue.Token.Value}'");
                }

                // order_dir | sort_dir
                else if (fieldName == "order_dir" || fieldName == "sort_dir") {
                    if (fieldOp.Token.Value != SearchOp.EQ) {
                        return $"for a 'sort_dir'|'order_dir' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'sort_dir'|'order_dir' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.OrderByDirection = BarMatchSearchParameters.ParseOrderByDirection(fieldValue.Token.Value)
                        ?? throw new SearchAstArgumentException($"invalid OrderByDirection value '{fieldValue.Token.Value}'");
                }

                // limit
                else if (fieldName == "limit") {
                    if (fieldOp.Token.Value != SearchOp.EQ) {
                        return $"for a 'limit' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'limit' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.Limit = uint.Parse(fieldValue.Token.Value);

                    if (query.Limit == 0) {
                        throw new SearchAstArgumentException($"limit must be greater than 0");
                    }
                    if (query.Limit > 1000) {
                        throw new SearchAstArgumentException($"limit must be less than or equal to 1000");
                    }
                }

                // offset | start
                else if (fieldName == "offset" || fieldName == "start") {
                    if (fieldOp.Token.Value != SearchOp.EQ) {
                        return $"for a 'offset' field, the operation can only be '='";
                    }
                    if (fieldValue.Type != SearchNodeType.FIELD_VALUE || fieldValue.Children.Count > 0) {
                        return $"for a 'offset' field, the value must be a FIELD_VALUE and have no children";
                    }

                    query.Offset = uint.Parse(fieldValue.Token.Value);

                    if (query.Offset > 100000) {
                        throw new SearchAstArgumentException($"offset must be 100000 or less");
                    }
                }

                // downloaded
                else if (fieldName == "downloaded") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);

                    cmd += @$" m.id IN (
                        SELECT game_id
                        FROM bar_match_processing
                        WHERE fetch_ms IS {(value == true ? "NOT NULL" : "NULL")} )
                    ";
                }

                // parsed
                else if (fieldName == "parsed") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);

                    cmd += @$" m.id IN (
                        SELECT game_id
                        FROM bar_match_processing
                        WHERE parse_ms IS {(value == true ? "NOT NULL" : "NULL")} )
                    ";
                }

                // replayed
                else if (fieldName == "replayed") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);

                    cmd += @$" m.id IN (
                        SELECT game_id
                        FROM bar_match_processing
                        WHERE replay_ms IS {(value == true ? "NOT NULL" : "NULL")} )
                    ";
                }

                // replayed
                else if (fieldName == "actions_parsed") {
                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);

                    cmd += @$" m.id IN (
                        SELECT game_id
                        FROM bar_match_processing
                        WHERE action_ms IS {(value == true ? "NOT NULL" : "NULL")} )
                    ";
                }

                // ranked
                else if (fieldName == "ranked") {
                    ValidateOperator(fieldOp, ValidTypeOperations.GetValueOrDefault(typeof(bool))!);

                    bool value = (bool)parseValue(typeof(bool), fieldValue.Token.Value, fieldName);
                    cmd += $" m.game_settings->>'ranked_game' = {(value == true ? "'1'" : "'0'")}\n";
                }

                // gamemode
                else if (fieldName == "gamemode") {
                    string value = (string)parseValue(typeof(string), fieldValue.Token.Value, fieldName);

                    if (value.StartsWith("d", StringComparison.OrdinalIgnoreCase) || value == "1v1") { // duel
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 1 ";
                    } else if (value.StartsWith("s", StringComparison.OrdinalIgnoreCase)) { // small team
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 2 ";
                    } else if (value.StartsWith("l", StringComparison.OrdinalIgnoreCase)) { // large team
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 3 ";
                    } else if (value.StartsWith("f", StringComparison.OrdinalIgnoreCase)) { // ffa
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 4 ";
                    } else if (value.StartsWith("t", StringComparison.OrdinalIgnoreCase)) { // team ffa
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 5 ";
                    } else if (value.StartsWith("o", StringComparison.OrdinalIgnoreCase)) { // other/default
                        cmd += $" m.gamemode {GetDbOperation(fieldOp)} 0 ";
                    } else {
                        return $"invalid gamemode: {value}";
                    }
                }

                // duration_time / duration
                else if (fieldName == "duration_time") {
                    TimeSpan duration = DurationStringUtil.Parse(fieldValue.Token.Value);

                    cmd += $" m.duration_ms {GetDbOperation(fieldOp)} {(int)duration.TotalMilliseconds}";
                }

                // pool
                else if (fieldName == "pool") {
                    if (long.TryParse(fieldValue.Token.Value, out long poolID) == true) {
                        cmd += $" mpool.id {GetDbOperation(fieldOp)} {poolID} ";
                    } else {
                        cmd += $" lower(mpool.name) {GetDbOperation(fieldOp)} {addParameter(query, fieldValue.Token.Value.ToLower(), fieldOp)} ";
                    }
                }

                // player.name
                else if (fieldName == "player.user_name") {
                    if (query.PlayerLookupCache.TryGetValue(fieldValue.Token.Value, out List<BarMatchPlayer>? players) == false || players == null) {
                        players = (await _PlayerRepository.SearchByName(fieldValue.Token.Value, cancel)).DistinctBy(iter => iter.UserID).ToList();
                        if (players.Count > 1000) {
                            return $"player name '{fieldValue.Token.Value}' returns too many results ({players.Count})";
                        }

                        query.PlayerLookupCache.Add(fieldValue.Token.Value, players);
                    }

                    if (players.Count == 0) {
                        cmd += " 1 = 0 ";
                    } else {
                        cmd += $" player.user_id IN (";
                        cmd += $" {string.Join(", ", players.Select(iter => $" {addParameter(query, iter.UserID, fieldOp)} "))} ";
                        cmd += ")\n";
                    }
                }

                // player.current_name
                else if (fieldName == "player.current_name") {
                    List<UserSearchResult> users = await _UserRepository.SearchByName(fieldValue.Token.Value, false, cancel);

                    if (users.Count == 0) {
                        cmd += " 1 = 0 ";
                    } else {
                        cmd += $" player.user_id IN (";
                        cmd += $" {string.Join(", ", users.Select(iter => $" {addParameter(query, iter.UserID, fieldOp)} "))} ";
                        cmd += ")\n";
                    }
                }

                // player.game_name
                else if (fieldName == "player.game_name") {
                    List<BarMatchPlayer> players = await _PlayerRepository.SearchByName(fieldValue.Token.Value, cancel);

                    if (players.Count == 0) {
                        cmd += " 1 = 0 ";
                    } else {
                        cmd += $" player.game_id IN (";
                        cmd += $" {string.Join(", ", players.Select(iter => $" {addParameter(query, iter.GameID, fieldOp)} "))} ";
                        cmd += ")\n";
                    }
                }

                // player.user_id
                else if (fieldName == "player.user_id") {
                    long userID = (long)parseValue(typeof(long), fieldValue.Token.Value, "player.user_id");

                    cmd += $" player.user_id {GetDbOperation(fieldOp)} {addParameter(query, userID, fieldOp)} ";
                }

                // player.faction / team.faction
                else if (fieldName == "team.faction") {

                    string faction = "";
                    if (fieldValue.Token.Value.StartsWith("a", StringComparison.OrdinalIgnoreCase)) {
                        faction = "Armada";
                    } else if (fieldValue.Token.Value.StartsWith("c", StringComparison.OrdinalIgnoreCase)) {
                        faction = "Cortex";
                    } else if (fieldValue.Token.Value.StartsWith("l", StringComparison.OrdinalIgnoreCase)) {
                        faction = "Legion";
                    } else if (fieldValue.Token.Value.StartsWith("r", StringComparison.OrdinalIgnoreCase)) {
                        faction = "Random";
                    } else {
                        return $"invalid faction: {fieldValue.Token.Value}";
                    }

                    cmd += $" team.faction {GetDbOperation(fieldOp)} {addParameter(query, faction, fieldOp)} ";
                }

                // map.name
                else if (fieldName == "map.name") {
                    query.MapCache ??= await _MapRepository.GetAll(cancel);

                    IEnumerable<BarMap> matchedMaps = query.MapCache.Where(iter => {
                        return iter.Name.Contains(fieldValue.Token.Value, StringComparison.OrdinalIgnoreCase)
                            || iter.FileName.Contains(fieldValue.Token.Value, StringComparison.OrdinalIgnoreCase);
                    });

                    if (matchedMaps.Count() == 0) {
                        cmd += " 0 = 1 ";
                    } else {
                        cmd += $" map.filename IN ( ";
                        cmd += $" {string.Join(", ", matchedMaps.Select(iter => $" {addParameter(query, iter.FileName, fieldOp)} "))} ";
                        cmd += ")\n";
                    }
                }

                // fallback
                else {
                    Type fieldType = FieldNameMappings.GetValueOrDefault(fieldName)
                        ?? throw new InvalidOperationException($"expected type mapping for fieldName '{fieldName}' to exist");

                    object value = parseValue(fieldType, fieldValue.Token.Value, fieldName);
                    //cmd += $" {fieldName} {GetDbOperation(fieldOp)} {addParameter(query, value, fieldOp)}";
                    cmd += $" {fieldName} {addOpAndParam(query, value, fieldOp, fieldType)} ";
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

                    cmd += @$" m.id {(fieldOp.Token.Value == SearchOp.NOT ? "NOT" : "")} IN (
                        SELECT distinct(player.game_id) as game_id 
                        FROM bar_match_player player
                            INNER JOIN bar_match_team team ON player.game_id = team.game_id AND player.team_id = team.team_id
                            INNER JOIN bar_match_ally_team ally_team ON team.ally_team_id = ally_team.ally_team_id AND team.game_id = ally_team.game_id
                        WHERE
                    ";
                }

                // map
                else if (fieldName == "map") {
                    prefix = "map";

                    cmd += @$" m.map_name {(fieldOp.Token.Value == "!" ? "NOT" : "")} IN (
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
        ///     add an operator and parameter to a query, also ensuring that the operator is valid for the type of field value
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <param name="op"></param>
        /// <param name="fieldType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SearchAstArgumentException"></exception>
        private static string addOpAndParam(QuerySetup query, object? value, SearchNode op, Type fieldType) {
            List<string> validOps = ValidTypeOperations.GetValueOrDefault(fieldType)
                ?? throw new InvalidOperationException($"unchecked field type {fieldType.Name}");

            if (validOps.Contains(op.Token.Value) == false) {
                throw new SearchAstArgumentException($"invalid operator '{op.Token.Value}' for field type {fieldType.Name}");
            }

            return $" {GetDbOperation(op)} {addParameter(query, value, op)}";
        }

        /// <summary>
        ///     add a DB parameter to the query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string addParameter(QuerySetup query, object? value, SearchNode? op) {

            if (op != null && (op.Token.Value == SearchOp.STARTS_WITH || op.Token.Value == SearchOp.ENDS_WITH || op.Token.Value == SearchOp.CONTAINS)) {
                if (value is not string str) {
                    throw new SearchAstArgumentException($"value was a {value?.GetType().Name}, not a {typeof(string).Name} (for a {op.Token.Value} operator)");
                }

                if (op.Token.Value == SearchOp.STARTS_WITH) {
                    query.Parameters.Add($"{value}%");
                } else if (op.Token.Value == SearchOp.ENDS_WITH) {
                    query.Parameters.Add($"%{value}");
                } else if (op.Token.Value == SearchOp.CONTAINS) {
                    query.Parameters.Add($"%{value}%");
                } else {
                    throw new InvalidOperationException($"invalid operator {op.Token.Value}, expected starts with, ends with or contains (and was in an exhaustive check)");
                }
            } else {
                query.Parameters.Add(value);
            }

            return $"${query.Parameters.Count}";
        }

        /// <summary>
        ///     parse an input string into the wanted <paramref name="fieldType"/>
        /// </summary>
        /// <param name="fieldType">type to turn the input into</param>
        /// <param name="input">input string</param>
        /// <param name="fieldName">name of the field</param>
        /// <returns>the <paramref name="input"/> as the type of <paramref name="fieldType"/></returns>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
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
            } else if (fieldType == typeof(DateTime)) {
                value = DateTime.Parse(input);
            } else {
                throw new InvalidOperationException($"unhandled type ({fieldType.FullName}) for field ({fieldName})");
            }

            return value;
        }

        private static void ValidateOperator(SearchNode op, IEnumerable<string> validOps) {
            if (validOps.Contains(op.Token.Value) == false) {
                throw new SearchAstArgumentException($"invalid operator '{op.Token.Value}', valid operators are: {string.Join(", ", validOps)}");
            }
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
            { "pool", typeof(string) },

            // match fields
            { "ranked", typeof(bool) },
            { "duration", typeof(TimeSpan) },
            { "duration_time", typeof(TimeSpan) },
            { "duration_frames", typeof(long) },
            { "duration_frame_count", typeof(long) },
            { "average_os", typeof(double) },
            { "min_os", typeof(double) },
            { "max_os", typeof(double) },
            { "start_time", typeof(DateTime) },
            { "engine", typeof(string) },
            { "game_version", typeof(string) },
            { "gamemode", typeof(string) },
            { "uploaded_by", typeof(long) },
            { "player_count", typeof(int) },
            { "downloaded", typeof(bool) },
            { "parsed", typeof(bool) },
            { "replayed", typeof(bool) },
            { "actions_parsed", typeof(bool) },

            // player fields
            { "player.user_name", typeof(string) },
            { "team.start_spot_label", typeof(string) },
            { "team.faction", typeof(string) },
            { "ally_team.won", typeof(bool) },
            { "player.skill", typeof(double) },
            { "player.skill_uncertainity", typeof(double) },
            { "player.team_id", typeof(int) },
            { "player.handicap", typeof(double) },
            { "player.game_name", typeof(string) },
            { "player.current_name", typeof(string) },
            { "player.user_id", typeof(long) },

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
        ///     valid types and what operations can be done on them
        /// </summary>
        private static Dictionary<Type, List<string>> ValidTypeOperations = new() {
            { typeof(bool), [ SearchOp.EQ, SearchOp.NOT ] },
            { typeof(TimeSpan), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(DateTime), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(int), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(long), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(uint), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(double), [ SearchOp.EQ, SearchOp.NOT, SearchOp.LT, SearchOp.LTE, SearchOp.GT, SearchOp.GTE ] },
            { typeof(string), [ SearchOp.EQ, SearchOp.NOT, SearchOp.STARTS_WITH, SearchOp.CONTAINS, SearchOp.ENDS_WITH ] },
        };

        /// <summary>
        ///     aliases of fields
        /// </summary>
        private static Dictionary<string, string> FieldNameAliases = new Dictionary<string, string>() {

            // basic fields
            { "duration", "duration_time" },
            { "duration_frames", "duration_frame_count" },
            { "average_skill", "average_os" },
            { "min_skill", "min_os" },
            { "max_skill", "max_os" },

            // player fields
            { "player.position", "team.start_spot_label" },
            { "player.pos", "team.start_spot_label" },
            { "player.role", "team.start_spot_label" },
            { "player.spot", "team.start_spot_label" },
            { "player.faction", "team.faction" },
            { "player.name", "player.user_name" },
            { "player.won", "ally_team.won" },
            { "player.handicap", "team.handicap" },

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

        private static string GetDbOperation(SearchNode op) {
            return op.Token.Value switch {
                SearchOp.EQ => "=",
                SearchOp.LT => "<",
                SearchOp.GT => ">",
                SearchOp.GTE => ">=",
                SearchOp.LTE => "<=",
                SearchOp.NOT => "!=",
                SearchOp.STARTS_WITH => " LIKE ",
                SearchOp.ENDS_WITH => " LIKE ",
                SearchOp.CONTAINS => " LIKE ",
                _ => throw new ArgumentException($"invalid operator {op.Token.Value}")
            };
        }

        private class QuerySetup {

            public List<object?> Parameters { get; set; } = [];

            public List<string> Joins { get; set; } = [];

            public HashSet<string> Joined { get; set; } = [];

            public OrderBy OrderBy { get; set; } = OrderBy.START_TIME;

            public OrderByDirection OrderByDirection { get; set; } = OrderByDirection.DESC;

            public uint Offset { get; set; } = 0;

            public uint Limit { get; set; } = 24;

            public Dictionary<string, List<BarMatchPlayer>> PlayerLookupCache = [];

            public List<BarMap>? MapCache = null;

        }


    }
}
