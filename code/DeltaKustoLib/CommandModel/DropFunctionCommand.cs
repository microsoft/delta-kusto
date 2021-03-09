using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public class DropFunctionCommand : CommandBase
    {
        public override string ObjectFriendlyTypeName => ".drop function";

        public DropFunctionCommand(string functionName)
            : base(functionName)
        {
        }

        internal static CommandBase FromCode(CustomCommand customCommand)
        {
            var customNode = customCommand.GetUniqueImmediateDescendant<CustomNode>("Custom node");
            var nameReference = customNode.GetUniqueImmediateDescendant<NameReference>("Name reference");

            return new DropFunctionCommand(nameReference.Name.SimpleName);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.ObjectName == ObjectName;

            return areEqualed;
        }

        public override string ToScript()
        {
            return $".drop function ['{ObjectName}']";
        }
    }
}