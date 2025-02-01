using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using SharpSchema.Generator.Model;
using Xunit;

namespace SharpSchema.Generator.Tests.Utilities
{
    internal record struct ItemNode : ISchemaNode, IComparable<ItemNode>
    {
        public int Value;

        public static implicit operator ItemNode(int value) => new() { Value = value };

        public static implicit operator int(ItemNode item) => item.Value;

        public readonly long GetSchemaHash() => Value;

        public readonly int CompareTo(ItemNode other) => Value.CompareTo(other.Value);
    }

    public class StructuralArrayTests
    {
        [Fact]
        public void Constructor_WithImmutableArray_WrapsArray()
        {
            ImmutableArray<ItemNode> array = [1, 2, 3];
            StructuralArray<ItemNode> structuralArray = array;

            Assert.Equal(array, (ImmutableArray<ItemNode>)structuralArray);
        }

        [Fact]
        public void Constructor_WithBuilder_WrapsArray()
        {
            var builder = ImmutableArray.CreateBuilder<ItemNode>();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);
            StructuralArray<ItemNode> structuralArray = builder;

            Assert.Collection(
                (ImmutableArray<ItemNode>)structuralArray,
                i => Assert.Equal(1, i.Value),
                i => Assert.Equal(2, i.Value),
                i => Assert.Equal(3, i.Value));
        }

        [Fact]
        public void ImplicitConversion_ToImmutableArray()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            ImmutableArray<ItemNode> result = structuralArray;

            Assert.Equal(array, result);
        }

        [Fact]
        public void ImplicitConversion_FromImmutableArray()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            Assert.Equal(array, (ImmutableArray<ItemNode>)structuralArray);
        }

        [Fact]
        public void ImplicitConversion_FromBuilder()
        {
            var builder = ImmutableArray.CreateBuilder<ItemNode>();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);
            StructuralArray<ItemNode> structuralArray = builder;

            Assert.Collection(
                (ImmutableArray<ItemNode>)structuralArray,
                i => Assert.Equal(1, i.Value),
                i => Assert.Equal(2, i.Value),
                i => Assert.Equal(3, i.Value));
        }

        [Fact]
        public void Indexer_ReturnsElementAtIndex()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            Assert.Equal(2, structuralArray[1].Value);
        }

        [Fact]
        public void Count_ReturnsNumberOfElements()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            Assert.Equal(3, structuralArray.Count);
        }

        [Fact]
        public void GetEnumerator_ReturnsEnumerator()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            var enumerator = structuralArray.GetEnumerator();

            Assert.Equal(array.GetEnumerator(), enumerator);
        }

        [Fact]
        public void IndexOf_ReturnsIndexOfItem()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray = array;

            int index = structuralArray.IndexOf(2, 0, 3, EqualityComparer<ItemNode>.Default);

            Assert.Equal(1, index);
        }

        [Fact]
        public void LastIndexOf_ReturnsLastIndexOfItem()
        {
            var array = ImmutableArray.Create<ItemNode>(1, 2, 3, 2);
            StructuralArray<ItemNode> structuralArray = array;

            int index = structuralArray.LastIndexOf(2, 3, 4, EqualityComparer<ItemNode>.Default);

            Assert.Equal(3, index);
        }

        [Fact]
        public void Equals_ReturnsTrueForEqualArrays()
        {
            var array1 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            var array2 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray1 = array1;
            StructuralArray<ItemNode> structuralArray2 = array2;

            Assert.True(structuralArray1.Equals(structuralArray2));
        }

        [Fact]
        public void Equals_ReturnsFalseForDifferentArrays()
        {
            var array1 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            var array2 = ImmutableArray.Create<ItemNode>(4, 5, 6);
            StructuralArray<ItemNode> structuralArray1 = array1;
            StructuralArray<ItemNode> structuralArray2 = array2;

            Assert.False(structuralArray1.Equals(structuralArray2));
        }

        [Fact]
        public void GetHashCode_ReturnsSameHashCodeForEqualArrays()
        {
            var array1 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            var array2 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray1 = array1;
            StructuralArray<ItemNode> structuralArray2 = array2;

            Assert.Equal(structuralArray1.GetHashCode(), structuralArray2.GetHashCode());
        }

        [Fact]
        public void OperatorEquals_ReturnsTrueForEqualArrays()
        {
            var array1 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            var array2 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            StructuralArray<ItemNode> structuralArray1 = array1;
            StructuralArray<ItemNode> structuralArray2 = array2;

            Assert.True(structuralArray1 == structuralArray2);
        }

        [Fact]
        public void OperatorNotEquals_ReturnsTrueForDifferentArrays()
        {
            var array1 = ImmutableArray.Create<ItemNode>(1, 2, 3);
            var array2 = ImmutableArray.Create<ItemNode>(4, 5, 6);
            StructuralArray<ItemNode> structuralArray1 = array1;
            StructuralArray<ItemNode> structuralArray2 = array2;

            Assert.True(structuralArray1 != structuralArray2);
        }

        [Fact]
        public void Create_WithValues_CreatesStructuralArray()
        {
            StructuralArray<ItemNode> structuralArray = [1, 2, 3];
            Assert.Collection(
                (ImmutableArray<ItemNode>)structuralArray,
                i => Assert.Equal(1, i.Value),
                i => Assert.Equal(2, i.Value),
                i => Assert.Equal(3, i.Value));
        }

        [Fact]
        public void Create_WithEmptyValues_CreatesEmptyStructuralArray()
        {
            StructuralArray<ItemNode> structuralArray = [];
            Assert.Empty((ImmutableArray<ItemNode>)structuralArray);
        }

        [Fact]
        public void IsEnumerable()
        {
            StructuralArray<ItemNode> structuralArray = [1, 2, 3];
            foreach (var item in structuralArray)
            {
                Assert.Contains(item, (ImmutableArray<ItemNode>)structuralArray);
            }
        }
    }
}
