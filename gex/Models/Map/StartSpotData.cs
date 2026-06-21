using Dapper.ColumnMapper;
using gex.Code;
using gex.Code.ExtensionMethods;
using Lua.CodeAnalysis.Syntax.Nodes;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace gex.Models.Map {

    [DapperColumnsMapped]
    public class StartSpotData {

        private const float PLAYER_START_SPOT_MAX_DISTANCE = 300f * 300f;

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("min_timestamp")]
        public DateTime MinTimestamp { get; set; }

        [ColumnMapping("max_timestamp")]
        public DateTime? MaxTimestamp { get; set; }

        /// <summary>
        ///     list of positions in the start spot data
        /// </summary>
        public List<StartSpotPosition> Positions { get; set; } = [];

        /// <summary>
        ///     list of configurations provided
        /// </summary>
        public List<StartSpotConfiguration> Configurations { get; set; } = [];

        public JsonElement Raw { get; set; } = JsonSerializer.Deserialize<JsonElement>("{}");

        public static bool operator ==(StartSpotData? left, StartSpotData? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(StartSpotData? left, StartSpotData? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            if (obj is not StartSpotData data
                    || MapFilename != data.MapFilename
                    || Positions.Count != data.Positions.Count
                    || Configurations.Count != data.Configurations.Count) {
                return false;
            }

            return JsonElement.DeepEquals(Raw, data.Raw);
        }

        public override int GetHashCode() {
            return HashCode.Combine(MapFilename, Version, Timestamp, MinTimestamp, MaxTimestamp);
        }

        public StartSpotConfiguration? GetByTeamCount(int teamCount) {
            foreach (StartSpotConfiguration config in Configurations) {
                if (config.TeamCount == teamCount) {
                    return config;
                }
            }

            return null;
        }

        public StartSpotSideStart? GetNearestStartSpot(int teamCount, float x, float z) {
            StartSpotConfiguration? config = GetByTeamCount(teamCount);
            if (config == null) {
                return null;
            }

            string? closestSpot = null;
            double closestDist = double.MaxValue;

            foreach (StartSpotPosition startSpot in Positions) {
                // yes, it's Z and Y for the second one. the start spot data mixes the axis
                double distanceSquared = Math.Pow(x - startSpot.X, 2) + Math.Pow(z - startSpot.Y, 2);

                if (Math.Pow(startSpot.MaxRadius ?? 300, 2) < distanceSquared) {
                    continue;
                }

                if (distanceSquared < closestDist) {
                    closestDist = distanceSquared;
                    closestSpot = startSpot.Name;
                }
            }

            StartSpotSideStart? start = null;
            if (closestSpot != null) {
                foreach (StartSpotSide side in config.Sides) {
                    start = side.Starts.Find(iter => iter.SpawnPoint == closestSpot || iter.BaseCenter == closestSpot);

                    if (start != null) {
                        break;
                    }
                }

                if (start == null) {
                    Debug.Fail($"failed to find start spot from side [closestSpot={closestSpot}]");
                }
            }

            return start;
        }

    }

    [DapperColumnsMapped]
    public class StartSpotPosition {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("x")]
        public float X { get; set; }

        [ColumnMapping("y")]
        public float Y { get; set; }

        public float? MaxRadius { get; set; }

        public static bool operator ==(StartSpotPosition? left, StartSpotPosition? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(StartSpotPosition? left, StartSpotPosition? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            return obj is StartSpotPosition position &&
                   MapFilename == position.MapFilename &&
                   Name == position.Name &&
                   X == position.X &&
                   Y == position.Y;
        }

        public override int GetHashCode() {
            return HashCode.Combine(MapFilename, Version, Name, X, Y);
        }

    }

    [DapperColumnsMapped]
    public class StartSpotConfiguration {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("players_per_team")]
        public int PlayersPerTeam { get; set; }

        [ColumnMapping("team_count")]
        public int TeamCount { get; set; }

        public List<StartSpotSide> Sides { get; set; } = [];

        public static bool operator ==(StartSpotConfiguration? left, StartSpotConfiguration? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(StartSpotConfiguration? left, StartSpotConfiguration? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            if (obj is not StartSpotConfiguration configuration
                   || MapFilename != configuration.MapFilename
                   || PlayersPerTeam != configuration.PlayersPerTeam
                   || TeamCount != configuration.TeamCount
                   || Sides.Count != configuration.Sides.Count) {

                return false;
            }

            for (int i = 0; i < Sides.Count; ++i) {
                if (Sides[i] != configuration.Sides[i]) {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() {
            return HashCode.Combine(MapFilename, Version, PlayersPerTeam, TeamCount);
        }

    }

    [DapperColumnsMapped]
    public class StartSpotSide {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("players_per_team")]
        public int PlayersPerTeam { get; set; }

        [ColumnMapping("team_count")]
        public int TeamCount { get; set; }

        [ColumnMapping("index")]
        public int Index { get; set; }

        public List<StartSpotSideStart> Starts { get; set; } = [];

        public static bool operator ==(StartSpotSide? left, StartSpotSide? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(StartSpotSide? left, StartSpotSide? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            if (obj is not StartSpotSide side
                   || MapFilename != side.MapFilename
                   || PlayersPerTeam != side.PlayersPerTeam
                   || TeamCount != side.TeamCount
                   || Index != side.Index
                   || Starts.Count != side.Starts.Count) {

                return false;
            }

            for (int i = 0; i < Starts.Count; ++i) {
                if (Starts[i] != side.Starts[i]) {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() {
            return HashCode.Combine(MapFilename, Version, PlayersPerTeam, TeamCount, Index);
        }

    }

    [DapperColumnsMapped]
    public class StartSpotSideStart {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("side_index")]
        public int SideIndex { get; set; }

        [ColumnMapping("role")]
        public string Role { get; set; } = "";

        public string BaseRole { get; set; } = "";

        [ColumnMapping("spawn_point")]
        public string SpawnPoint { get; set; } = "";

        [ColumnMapping("base_center")]
        public string? BaseCenter { get; set; }

        public static bool operator ==(StartSpotSideStart? left, StartSpotSideStart? right) {
            if (left is null) {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(StartSpotSideStart? left, StartSpotSideStart? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            return obj is StartSpotSideStart start &&
                   MapFilename == start.MapFilename &&
                   SideIndex == start.SideIndex &&
                   Role == start.Role &&
                   SpawnPoint == start.SpawnPoint &&
                   BaseCenter == start.BaseCenter;
        }

        public override int GetHashCode() {
            return HashCode.Combine(MapFilename, Version, SideIndex, Role, SpawnPoint, BaseCenter);
        }

    }

    [DapperColumnsMapped]
    public class StartSpotSideStartRoleOverride {

        [ColumnMapping("map_filename")]
        public string MapFilename { get; set; } = "";

        [ColumnMapping("version")]
        public int Version { get; set; }

        [ColumnMapping("position")]
        public string Position { get; set; } = "";

        [ColumnMapping("role")]
        public string Role { get; set; } = "";

        [ColumnMapping("max_radius")]
        public float? MaxRadius { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

    }

}
