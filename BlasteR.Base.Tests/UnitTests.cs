using System.Linq;
using Xunit;

namespace BlasteR.Base.Tests
{
    public class UnitTests : IClassFixture<TestFixture>
    {
        public IUnitOfWork UnitOfWork { get; set; }
        public UnitTests(TestFixture fixture)
        {
            UnitOfWork = fixture.UnitOfWork;
        }

        [Fact]
        public void CRUD()
        {
            // Arrange
            FirstService firstService = new FirstService(UnitOfWork);
            var entity = new FirstEntity()
            {
                IntValue = 1,
                StringValue = "Test"
            };

            // Act CREATE
            firstService.Save(entity);

            // Assert
            int entitiesCount = firstService.GetAll().Count();
            Assert.NotEqual(0, entity.Id);
            Assert.NotEqual(0, entitiesCount);

            // Act READ
            entity = firstService.GetById(entity.Id);

            // Assert
            Assert.NotNull(entity);

            // Act UPDATE
            entity.StringValue = "Test Updated";
            firstService.Save(entity);

            // Assert
            entity = firstService.GetById(entity.Id);
            Assert.Equal("Test Updated", entity.StringValue);

            // Act DELETE
            firstService.Delete(entity);

            // Assert
            Assert.Equal(entitiesCount - 1, firstService.GetAll().Count());
        }

        [Fact]
        public void InsertParentChild()
        {
            // Arrange
            FirstService firstService = new FirstService(UnitOfWork);
            SecondService secondService = new SecondService(UnitOfWork);
            FirstEntity firstEntity = new FirstEntity()
            {
                IntValue = 1,
                StringValue = "Test",
                SecondEntity = new SecondEntity()
                {
                    IntValue = 2,
                    StringValue = "Second"
                }
            };

            // Act
            firstService.Save(firstEntity);

            // Assert
            Assert.NotEqual(0, firstEntity.Id);
            Assert.NotEqual(0, firstEntity.SecondEntity.Id);
            Assert.Equal(firstEntity.Id, firstEntity.SecondEntity.FirstEntityId);

            // Cleanup
            secondService.Delete(firstEntity.SecondEntity);
            firstService.Delete(firstEntity);
        }

        [Fact]
        public void InsertParentChild_NoRewire()
        {
            // Arrange
            FirstService firstService = new FirstService(UnitOfWork);
            SecondService secondService = new SecondService(UnitOfWork);

            SecondEntity parent = new SecondEntity()
            {
                IntValue = 0,
                StringValue = "Parent"
            };
            secondService.Save(parent);

            FirstEntity child1 = new FirstEntity()
            {
                IntValue = 1,
                StringValue = "Child1"
            };
            firstService.Save(child1);

            FirstEntity child2 = new FirstEntity()
            {
                IntValue = 2,
                StringValue = "Child2"
            };
            firstService.Save(child2);

            parent.FirstEntity = child1;
            secondService.Save(parent);
            parent = secondService.GetById(parent.Id);

            // Act
            parent.FirstEntity = child2;
            secondService.Save(parent);
            parent = secondService.GetById(parent.Id);

            // Assert
            Assert.NotEqual(child2.Id, parent.FirstEntityId);

            // Cleanup
            secondService.Delete(parent);
            firstService.Delete(child1);
            firstService.Delete(child2);
        }

        [Fact]
        public void InsertParentChild_Rewire()
        {
            // Arrange
            FirstService firstService = new FirstService(UnitOfWork);
            SecondService secondService = new SecondService(UnitOfWork);

            SecondEntity parent = new SecondEntity()
            {
                IntValue = 0,
                StringValue = "Parent"
            };
            secondService.Save(parent);

            FirstEntity child1 = new FirstEntity()
            {
                IntValue = 1,
                StringValue = "Child1"
            };
            firstService.Save(child1);

            FirstEntity child2 = new FirstEntity()
            {
                IntValue = 2,
                StringValue = "Child2"
            };
            firstService.Save(child2);

            parent.FirstEntity = child1;
            secondService.Save(parent);
            parent = secondService.GetById(parent.Id);

            // Act
            parent.FirstEntityId = child2.Id;
            secondService.Save(parent);
            parent = secondService.GetById(parent.Id);

            // Assert
            Assert.Equal(child2.Id, parent.FirstEntityId);

            // Cleanup
            secondService.Delete(parent);
            firstService.Delete(child1);
            firstService.Delete(child2);
        }

        [Fact]
        public void InsertParentChildReverse()
        {
            // Arrange
            FirstService firstService = new FirstService(UnitOfWork);
            SecondService secondService = new SecondService(UnitOfWork);
            SecondEntity secondEntity = new SecondEntity()
            {
                IntValue = 2,
                StringValue = "Second",
                FirstEntity = new FirstEntity()
                {
                    IntValue = 1,
                    StringValue = "Test"
                }
            };

            // Act
            secondService.Save(secondEntity);

            // Assert
            Assert.NotEqual(0, secondEntity.Id);
            Assert.NotEqual(0, secondEntity.FirstEntity.Id);
            Assert.Equal(secondEntity.FirstEntity.Id, secondEntity.FirstEntityId);

            // Cleanup
            secondService.Delete(secondEntity);
            firstService.Delete(secondEntity.FirstEntity);
        }

        [Fact]
        public void SoftDelete()
        {
            // Arrange
            SoftDeletableTestService softDeletableTestService = new SoftDeletableTestService(UnitOfWork);
            SoftDeletableTestEntity softDeletableTestEntity = new SoftDeletableTestEntity()
            {
                IntValue = 1,
                StringValue = "Test",
            };

            softDeletableTestService.Save(softDeletableTestEntity);

            // Act Soft Delete
            softDeletableTestService.Delete(softDeletableTestEntity);

            // Assert
            Assert.DoesNotContain(softDeletableTestEntity.Id, softDeletableTestService.GetAll(false).Select(x => x.Id));
            Assert.Contains(softDeletableTestEntity.Id, softDeletableTestService.GetAll(true).Select(x => x.Id));

            // Act Hard Delete
            softDeletableTestService.Delete(softDeletableTestEntity, true);

            // Assert
            Assert.DoesNotContain(softDeletableTestEntity.Id, softDeletableTestService.GetAll(true).Select(x => x.Id));
        }
    }
}
