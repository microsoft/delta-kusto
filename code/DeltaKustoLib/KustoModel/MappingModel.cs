using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace DeltaKustoLib.KustoModel
{
    public class MappingModel
    {
        #region MyRegion
        private class MappingProperties
        {
            public string Ordinal { get; set; } = string.Empty;

            public string ConstValue { get; set; } = string.Empty;

            public string Path { get; set; } = string.Empty;

            public string Transform { get; set; } = string.Empty;

            public string Field { get; set; } = string.Empty;

            #region Object methods
            public override bool Equals(object? obj)
            {
                var other = obj as MappingProperties;

                return other != null
                    && (other.Ordinal == null ? Ordinal == null : other.Ordinal.Equals(Ordinal))
                    && (other.ConstValue == null ? ConstValue == null : other.ConstValue.Equals(ConstValue))
                    && (other.Path == null ? Path == null : other.Path.Equals(Path))
                    && (other.Transform == null ? Transform == null : other.Transform.Equals(Transform))
                    && (other.Field == null ? Field == null : other.Field.Equals(Field));
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            #endregion
        }

        private class MappingElement : MappingProperties
        {
            public string Column { get; set; } = string.Empty;

            public string Name { get; set; } = string.Empty;

            public string DataType { get; set; } = string.Empty;

            public MappingProperties Properties { get; set; } = new MappingProperties();

            public MappingElement ToNormalize()
            {
                var result = new MappingElement
                {
                    Column = string.IsNullOrWhiteSpace(Name) ? Column : Name,
                    DataType = DataType ?? string.Empty,
                    Properties = new MappingProperties
                    {
                        Ordinal = string.IsNullOrWhiteSpace(Ordinal) ? Properties.Ordinal : Ordinal,
                        ConstValue = string.IsNullOrWhiteSpace(ConstValue) ? Properties.ConstValue : ConstValue,
                        Path = string.IsNullOrWhiteSpace(ConstValue) ? Properties.ConstValue : ConstValue,
                        Transform = string.IsNullOrWhiteSpace(Transform) ? Properties.Transform : Transform,
                        Field = string.IsNullOrWhiteSpace(Field) ? Properties.ConstValue : Field
                    }
                };

                return result;
            }

            #region Object methods
            public override bool Equals(object? obj)
            {
                var other = obj as MappingElement;
                var result = other != null
                    && other.Column.Equals(Column)
                    && other.DataType.Equals(DataType)
                    && other.Properties.Equals(Properties);

                return result;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            #endregion
        }
        #endregion

        private readonly static JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public QuotedText MappingName { get; }

        public string MappingKind { get; }

        public QuotedText MappingAsJson { get; }

        public bool RemoveOldestIfRequired { get; }

        public MappingModel(
            QuotedText mappingName,
            string mappingKind,
            QuotedText mappingAsJson,
            bool removeOldestIfRequired)
        {
            MappingName = mappingName;
            MappingKind = mappingKind.ToLower();
            MappingAsJson = mappingAsJson;
            RemoveOldestIfRequired = removeOldestIfRequired;
        }

        #region Object methods
        public override bool Equals(object? obj)
        {
            var other = obj as MappingModel;
            var result = other != null
                && other.MappingName.Equals(MappingName)
                && other.MappingKind.Equals(MappingKind)
                && MappingAsJsonEquals(other.MappingAsJson)
                && other.RemoveOldestIfRequired == RemoveOldestIfRequired;

            return result;
        }

        public override int GetHashCode()
        {
            return MappingName.GetHashCode()
                ^ MappingKind.GetHashCode()
                ^ MappingAsJson.GetHashCode()
                ^ RemoveOldestIfRequired.GetHashCode();
        }
        #endregion

        internal static IEnumerable<CommandBase> ComputeDelta(
            EntityName tableName,
            IEnumerable<MappingModel> currentMappings,
            IEnumerable<MappingModel> targetMappings)
        {
            var createdModels = DeltaHelper.GetCreated(
                currentMappings,
                targetMappings,
                m => (m.MappingName, m.MappingKind));
            var updatedModels = DeltaHelper.GetUpdated(
                currentMappings,
                targetMappings,
                m => (m.MappingName, m.MappingKind));
            var droppedModels = DeltaHelper.GetDropped(
                currentMappings,
                targetMappings,
                m => (m.MappingName, m.MappingKind));
            var createCommands = createdModels
                .Concat(updatedModels.Select(p => p.after))
                .Select(m => m.ToCreateMappingCommand(tableName));
            var dropCommands = droppedModels
                .Select(m => m.ToDropMappingCommand(tableName));

            return dropCommands
                .Cast<CommandBase>()
                .Concat(createCommands);
        }

        internal CreateMappingCommand ToCreateMappingCommand(EntityName tableName)
        {
            return new CreateMappingCommand(
                tableName,
                MappingKind,
                MappingName,
                MappingAsJson,
                RemoveOldestIfRequired);
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        private bool MappingAsJsonEquals(QuotedText otherMappingAsJson)
        {
            var thisElements = JsonSerializer
                .Deserialize<MappingElement[]>(MappingAsJson.Text, _jsonSerializerOptions)
                !.Select(e => e.ToNormalize())
                .OrderBy(e => e.Column);
            var otherElements = JsonSerializer
                .Deserialize<MappingElement[]>(otherMappingAsJson.Text, _jsonSerializerOptions)
                !.Select(e => e.ToNormalize())
                .OrderBy(e => e.Column);

            return thisElements.SequenceEqual(otherElements);
        }

        private DropMappingCommand ToDropMappingCommand(EntityName tableName)
        {
            return new DropMappingCommand(
                tableName,
                MappingKind,
                MappingName);
        }
    }
}