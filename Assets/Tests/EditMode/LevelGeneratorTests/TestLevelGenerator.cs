using System;
using System.Linq;
using LevelGenerator;
using LevelGenerator.Extensions;
using NUnit.Framework;

namespace Tests.EditMode.LevelGeneratorTests
{
    internal class CancelAfterN
    {
        public CancelAfterN(uint n)
        {
            _num = n;
        }

        public bool CheckCancel()
        {
            return --_num == 0U;
        }

        private uint _num;
    }
    
    public class TestLevelGenerator
    {
        [Test]
//uint max_num_levels, uint width, uint height, uint min_rooms, uint max_rooms,
// uint num_breaches, ulong seed, bool load_prog_from_file, uint num_threads)
        public void LevelGeneratorCanBeCreated()
        {
            // Act
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                1, 1, 2, 3, 4, 1, 0, 5, false, 1
            );
            
            // Assert
            Assert.IsNotNull(genUnderTest);
        }
        
        [Test]
        public void LevelGeneratorCanBeDisposed()
        {
            // Arrange
            var genUnderTest = new LevelGenerator.LevelGenerator(
                1, 1, 2, 3, 4, 1, 0, 5, false, 1
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
                2, 10, 10, 1, 6, 1, 0, 1234, false, 2
            );
            
            // Act
            var result = genUnderTest.Solve(null);
                
            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(result.Replace('\n', ' ').Split(' ').Count(word => word == "Model:"), Is.EqualTo(2));
            Assert.That(genUnderTest.NumLevels, Is.EqualTo(2));
        }
        
        [Test]
        public void LevelGeneratorCanBeCancelled([Values(4U, 5U, 6U)] uint n)
        {
            // Arrange
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                20, 10, 10, 1, 6, 1, 0, 1234, false, 2
            );
            var canceller = new CancelAfterN(n * 2);  // n*2 as cancellation is checked twice per model
            
            // Act
            var result = genUnderTest.Solve(canceller.CheckCancel);
                
            // Assert
            Assert.That(result, Is.Not.Null.And.Not.Empty);
            Assert.That(genUnderTest.NumLevels, Is.EqualTo(n));
        }
        
        [Test]
        public void LevelGeneratorHasBestLevel()
        {
            // Arrange
            using var genUnderTest = new LevelGenerator.LevelGenerator(
                3, 12, 12, 2, 8, 2, 0, 1234, false, 1
            );
            
            // Act
            genUnderTest.Solve(null);
            using var bestLevel = genUnderTest.BestLevel();
                
            // Assert
            Assert.IsNotNull(bestLevel);
        }
        
        [Test]
        public void BestLevelHasCorrectNumbers()
        {
            // Arrange
            using var gen = new LevelGenerator.LevelGenerator(
                1, 9, 10, 1, 6, 1, 0, 1234, false, 1
            );
            // Act
            gen.Solve(null);
            using var bestLevelUnderTest = gen.BestLevel();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(bestLevelUnderTest.Cost, Is.EqualTo(23));
            Assert.That(bestLevelUnderTest.NumMapSquares, Is.EqualTo(90));
            Assert.That(bestLevelUnderTest.NumRooms, Is.EqualTo(7));
            Assert.That(bestLevelUnderTest.NumDoors, Is.EqualTo(16));
            Assert.That(bestLevelUnderTest.NumPortals, Is.EqualTo(0));
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
                1, 9, 10, 1, 6, 1, 0, 1234, false, 1
            );
            
            // Act
            gen.Solve(null);
            using var bestLevelUnderTest = gen.BestLevel();
            using var mapSquares = bestLevelUnderTest.MapSquares();
            using var rooms= bestLevelUnderTest.Rooms();
            using var doors = bestLevelUnderTest.Doors();
            using var portals = bestLevelUnderTest.Portals();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(CountAndDisposeSymbols(mapSquares, false), Is.EqualTo(90));
            Assert.That(CountAndDisposeSymbols(rooms, false), Is.EqualTo(7));
            Assert.That(CountAndDisposeSymbols(doors, false), Is.EqualTo(16));
            Assert.That(CountAndDisposeSymbols(portals, false), Is.EqualTo(0));
        }
        
        [Test]
        public void LevelPartIterExtensionsAllowForEach()
        {
            // Arrange
            using var gen = new LevelGenerator.LevelGenerator(
                1, 9, 10, 1, 6, 1, 0, 1234, false, 1
            );
            
            // Act
            gen.Solve(null);
            using var bestLevel = gen.BestLevel();
            using var mapSquaresUnderTest = bestLevel.MapSquares();
            using var roomsUnderTest= bestLevel.Rooms();
            using var doorsUnderTest = bestLevel.Doors();
            using var portalsUnderTest = bestLevel.Portals();
                
            // Assert - these values are the same as in the C++ equivalent tests with the same seed
            Assert.That(CountAndDisposeSymbols(mapSquaresUnderTest, true), Is.EqualTo(90));
            Assert.That(CountAndDisposeSymbols(roomsUnderTest, true), Is.EqualTo(7));
            Assert.That(CountAndDisposeSymbols(doorsUnderTest, true), Is.EqualTo(16));
            Assert.That(CountAndDisposeSymbols(portalsUnderTest, true), Is.EqualTo(0));
        }
    }
}
