using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public class ActionCommandCollection
    {
        #region Inner Types
        public record CommandGroup(Type CommandType, string HeaderComment, IImmutableList<CommandBase> Commands);
        #endregion

        public ActionCommandCollection(bool usePluralForms, IEnumerable<CommandBase> commands)
        {
            //  Use plural commands if required
            var pluralCommands = usePluralForms
                ? ToPlural(commands)
                : commands;
            var groups = commands
                .GroupBy(c => c.GetType())
                .Select(g => (att: GetAttribute(g.Key), group: g))
                //  Sort each type
                .OrderBy(t => t.att.Order)
                .Select(t => new CommandGroup(
                    t.group.Key,
                    t.att.HeaderComment,
                    //  Sort commands within type
                    t.group.OrderBy(c => c.SortIndex).ToImmutableArray()))
                .ToImmutableArray();

            CommandGroups = groups;
            AllCommands = CommandGroups
                .SelectMany(g => g.Commands)
                .ToImmutableArray();
            DataLossCommands = CommandGroups
                .Where(g => g.CommandType == typeof(DropTableCommand)
                || g.CommandType == typeof(DropTableColumnsCommand)
                || g.CommandType == typeof(AlterColumnTypeCommand))
                .SelectMany(g => g.Commands)
                .ToImmutableArray();
        }

        public IImmutableList<CommandGroup> CommandGroups { get; }

        public IImmutableList<CommandBase> AllCommands { get; }

        public IImmutableList<CommandBase> DataLossCommands { get; }

        private static IEnumerable<CommandBase> ToPlural(IEnumerable<CommandBase> commands)
        {
            var pluralCommands = commands
                .GroupBy(c => c.GetType())
                .Select(g => ToPlural(g))
                .SelectMany(c => c);

            return pluralCommands;
        }

        private static IEnumerable<CommandBase> ToPlural(IGrouping<Type, CommandBase> group)
        {
            var singularToPluralCommand = group.First() as ISingularToPluralCommand;

            if (singularToPluralCommand == null)
            {
                return group;
            }
            else
            {
                return singularToPluralCommand.ToPlural(group);
            }
        }

        private static CommandAttribute GetAttribute(Type type)
        {
            var attributes = type.GetCustomAttributes(typeof(CommandAttribute), false);
            var attribute = (CommandAttribute)attributes.First();

            return attribute;
        }
    }
}