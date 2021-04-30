using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DeltaKustoLib.KustoModel
{
    public class MappingModel
    {
        public QuotedText MappingName { get; }

        public string MappingKind { get; }

        public QuotedText MappingAsJson { get; }

        public MappingModel(
            QuotedText mappingName,
            string mappingKind,
            QuotedText mappingAsJson)
        {
            MappingName = mappingName;
            MappingKind = mappingKind;
            MappingAsJson = mappingAsJson;
        }

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

            return createCommands
                .Cast<CommandBase>()
                .Concat(dropCommands);
        }

        internal CreateMappingCommand ToCreateMappingCommand(EntityName tableName)
        {
            return new CreateMappingCommand(
                tableName,
                MappingKind,
                MappingName,
                MappingAsJson);
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