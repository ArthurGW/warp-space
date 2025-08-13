using System;
using System.Collections;
using System.Linq;
using LevelGenerator;
using LevelGenerator.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode.LevelGeneratorTests
{
    public class TestLevelGenerator
    {
        [Test]
//uint max_num_levels, uint width, uint height, uint min_rooms, uint max_rooms,
// uint num_breaches, ulong seed, bool load_prog_from_file, uint num_threads)
        public void LevelGeneratorCanBeCreated()
        {
            // Act
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                1, 1, 2, 3, 4, 1, 5, false, 1
            );
            
            // Assert
            Assert.IsNotNull(genUnderTest);
        }
        
        [Test]
        public void LevelGeneratorCanBeDisposed()
        {
            // Arrange
            var genUnderTest = new LevelGenerator.LevelGenerator(
                1, 1, 2, 3, 4, 1, 5, false, 1
            );
            
            // Act - checking that no exceptions happen during the native dtor
            genUnderTest.Dispose();
            genUnderTest = null;

            // Assert
            Assert.IsNull(genUnderTest);
        }
        
        [Test]
        public void LevelGeneratorCanBeSolved()
        {
            // Arrange
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                2, 10, 10, 1, 6, 1, 1234, false, 2
            );
            
            // Act
            var result = genUnderTest.Solve();
                
            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Replace('\n', ' ').Split(' ').Count(word => word == "Model:"), Is.EqualTo(2));
            Assert.That(genUnderTest.NumLevels, Is.EqualTo(2));
        }
        
        [Test]
        public void LevelGeneratorHasBestLevel()
        {
            // Arrange
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                3, 12, 12, 2, 8, 2, 1234, false, 1
            );
            
            // Act
            genUnderTest.Solve();
            using var bestLevel = genUnderTest.BestLevel();
                
            // Assert
            Assert.IsNotNull(bestLevel);
        }
        
        [Test]
        public void BestLevelHasCorrectNumbers()
        {
            // Arrange
            using var gen = new LevelGenerator.LevelGenerator(
                1, 9, 10, 1, 6, 1, 1234, false, 1
            );
            // Act
            gen.Solve();
            using var bestLevelUnderTest = gen.BestLevel();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(bestLevelUnderTest.Cost, Is.EqualTo(7));
            Assert.That(bestLevelUnderTest.NumMapSquares, Is.EqualTo(90));
            Assert.That(bestLevelUnderTest.NumRooms, Is.EqualTo(7));
            Assert.That(bestLevelUnderTest.NumAdjacencies, Is.EqualTo(12));
        }

        private static uint CountAndDisposeSymbols<T>(LevelPartIter<T> iter, bool useExtensions) where T : IDisposable
        {
            uint sum = 0;

            if (useExtensions)
            {
                foreach (var part in iter)
                {
                    part.Dispose();
                    ++sum;
                }
                
                // Iterate again, to test repeated usage
                foreach (var part in iter)
                {
                    part.Dispose();
                }
            }
            else
            {
                iter.Reset();
                while (iter.MoveNext())
                {
                    using var current = iter.Current();
                    ++sum;
                }
            
                // Iterate again, to test repeated usage
                iter.Reset();
                while (iter.MoveNext())
                {
                    using var current = iter.Current();
                }
            }
            return sum;
        }
        
        [Test]
        public void BestLevelHasIterableParts()
        {
            // Arrange
            using var gen = new LevelGenerator.LevelGenerator(
                1, 9, 10, 1, 6, 1, 1234, false, 1
            );
            
            // Act
            gen.Solve();
            using var bestLevelUnderTest = gen.BestLevel();
            using var mapSquares = bestLevelUnderTest.MapSquares();
            using var rooms= bestLevelUnderTest.Rooms();
            using var adjacencies = bestLevelUnderTest.Adjacencies();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(CountAndDisposeSymbols(mapSquares, false), Is.EqualTo(90));
            Assert.That(CountAndDisposeSymbols(rooms, false), Is.EqualTo(7));
            Assert.That(CountAndDisposeSymbols(adjacencies, false), Is.EqualTo(12));
        }
        
        [Test]
        public void LevelPartIterExtensionsAllowForEach()
        {
            // Arrange
            using var gen = new LevelGenerator.LevelGenerator(
                1, 9, 10, 1, 6, 1, 1234, false, 1
            );
            
            // Act
            gen.Solve();
            using var bestLevel = gen.BestLevel();
            using var mapSquaresUnderTest = bestLevel.MapSquares();
            using var roomsUnderTest= bestLevel.Rooms();
            using var adjacenciesUnderTest = bestLevel.Adjacencies();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(CountAndDisposeSymbols(mapSquaresUnderTest, true), Is.EqualTo(90));
            Assert.That(CountAndDisposeSymbols(roomsUnderTest, true), Is.EqualTo(7));
            Assert.That(CountAndDisposeSymbols(adjacenciesUnderTest, true), Is.EqualTo(12));
        }
    }
}
