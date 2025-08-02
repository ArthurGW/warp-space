using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using LevelGenerator;
using LevelGenerator.Extensions;
using UnityEngine;

namespace LevelGeneratorTests
{
    public class TestLevelGenerator
    {
        private string _program;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var request = Resources.LoadAsync("programs/ship");
            yield return request;
            var program = request.asset as TextAsset;
            if (program != null)
            {
                _program = program.text;
            }
        }
        
        [Test]
        public void LevelGeneratorCanBeCreated()
        {
            // Act
            using (
                var genUnderTest = new LevelGenerator.LevelGenerator(
                    1, 1, 2, 3, 4, 5, _program, 1
                )
            )
            {
                // Assert
                Assert.IsNotNull(genUnderTest);
            }
        }
        
        [Test]
        public void LevelGeneratorCanBeDisposed()
        {
            // Arrange
            var genUnderTest = new LevelGenerator.LevelGenerator(
                1, 1, 2, 3, 4, 5, _program, 1
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
            using (
                var genUnderTest = new LevelGenerator.LevelGenerator(
                    2, 10, 7, 2, 6, 1234, _program, 1
                )
            )
            {
                // Act
                var result = genUnderTest.Solve();
                
                // Assert
                Assert.That(result, Is.Not.Null.And.Not.Empty);
                Assert.That(result.Replace('\n', ' ').Split(' ').Count(word => word == "Model:"), Is.EqualTo(2));
                Assert.That(genUnderTest.NumLevels, Is.EqualTo(2));
            }
        }
        
        [Test]
        public void LevelGeneratorHasBestLevel()
        {
            // Arrange
            using (
                var genUnderTest = new LevelGenerator.LevelGenerator(
                    2, 10, 7, 2, 6, 1234, _program, 1
                )
            )
            {
                // Act
                genUnderTest.Solve();
                using var bestLevel = genUnderTest.BestLevel();
                
                // Assert
                Assert.IsNotNull(bestLevel);
            }
        }
        
        [Test]
        public void BestLevelHasCorrectNumbers()
        {
            // Arrange
            using (
                var gen = new LevelGenerator.LevelGenerator(
                    2, 10, 7, 2, 6, 1234, _program, 1
                )
            )
            {
                // Act
                gen.Solve();
                using var bestLevelUnderTest = gen.BestLevel();
                
                // Assert - these values are the same as in the C++ equivalent tests with the same seed
                Assert.That(bestLevelUnderTest.Cost, Is.EqualTo(5));
                Assert.That(bestLevelUnderTest.NumMapSquares, Is.EqualTo(84));
                Assert.That(bestLevelUnderTest.NumRooms, Is.EqualTo(5));
                Assert.That(bestLevelUnderTest.NumAdjacencies, Is.EqualTo(8));
            }
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
                while (iter.MoveNext())
                {
                    using var current = iter.Current();
                    ++sum;
                }
            }
            return sum;
        }
        
        [Test]
        public void BestLevelHasIterableParts()
        {
            // Arrange
            using (
                var gen = new LevelGenerator.LevelGenerator(
                    2, 10, 7, 2, 6, 1234, _program, 1
                )
            )
            {
                // Act
                gen.Solve();
                using var bestLevelUnderTest = gen.BestLevel();
                using var mapSquares = bestLevelUnderTest.MapSquares();
                using var rooms= bestLevelUnderTest.Rooms();
                using var adjacencies = bestLevelUnderTest.Adjacencies();
                
                // Assert - these values are the same as in the C++ equivalent tests with the same seed
                Assert.That(CountAndDisposeSymbols(mapSquares, false), Is.EqualTo(84));
                Assert.That(CountAndDisposeSymbols(rooms, false), Is.EqualTo(5));
                Assert.That(CountAndDisposeSymbols(adjacencies, false), Is.EqualTo(8));
            }
        }
        
        [Test]
        public void LevelPartIterExtensionsAllowForEach()
        {
            // Arrange
            using (
                var gen = new LevelGenerator.LevelGenerator(
                    2, 10, 7, 2, 6, 1234, _program, 1
                )
            )
            {
                // Act
                gen.Solve();
                using var bestLevel = gen.BestLevel();
                using var mapSquaresUnderTest = bestLevel.MapSquares();
                using var roomsUnderTest= bestLevel.Rooms();
                using var adjacenciesUnderTest = bestLevel.Adjacencies();
                
                // Assert - these values are the same as in the C++ equivalent tests with the same seed
                Assert.That(CountAndDisposeSymbols(mapSquaresUnderTest, true), Is.EqualTo(84));
                Assert.That(CountAndDisposeSymbols(roomsUnderTest, true), Is.EqualTo(5));
                Assert.That(CountAndDisposeSymbols(adjacenciesUnderTest, true), Is.EqualTo(8));
            }
        }
    }
}
