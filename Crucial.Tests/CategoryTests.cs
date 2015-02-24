﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crucial.Providers.Questions.Data;
using Crucial.Services.Managers.Interfaces;
using Crucial.Tests.Bootstrap;
using Crucial.Providers.Questions;
using StructureMap;
using Crucial.Framework.DesignPatterns.CQRS.Messaging;
using Crucial.Qyz.Commands;
using System.Linq;
using Crucial.Framework.IoC.StructureMapProvider;
using Crucial.Framework.Testing.EF;
using Crucial.Providers.Questions.Entities;
using Crucial.Providers.EventStore.Data;
using Crucial.Providers.EventStore.Entities;
using Crucial.Qyz.Events;
using Crucial.EventStore;
using Crucial.Qyz.Domain;

namespace Crucial.Tests
{
    [TestClass]
    public class CategoryTests
    {
        public CategoryTests()
        {
            Dependencies.Setup();
        }
        
        ICategoryRepository _categoryRepo;
        ICommandBus _commandBus;
        IEventBus _eventBus;

        [TestInitialize]
        public void Setup()
        {
            _categoryRepo = DependencyResolver.Container.GetInstance<ICategoryRepository>();
            _commandBus = DependencyResolver.Container.GetInstance<ICommandBus>();
            _eventBus = DependencyResolver.Container.GetInstance<IEventBus>();
        }

        [TestMethod]
        public void UserCategoryCreateCommandTriggersEventToUpdateQueryDbWithCategory()
        {
            var name = "Test Category 1";

            //Arrange
            _commandBus.Send(new UserCategoryCreateCommand(1, name));

            //Act
            var cat = _categoryRepo.FindBy(q => q.Id == 1).FirstOrDefault();

            //Act
            Assert.AreEqual(cat.Name, name);
        }

        [TestMethod]
        public void UserCategoryUpdateCommandTriggersEventToUpdateQueryDbWithNewCategoryName()
        {
            //Arrange
            string newName = "NewCategoryNameForCategory2";

            IQuestionsDbContext qContext = Crucial.Framework.IoC.StructureMapProvider.DependencyResolver.Container.GetInstance<IQuestionsDbContext>();
            qContext.Categories.Add(new Category { Id = 1, Name = "Category1", Version = 0 });
            qContext.Categories.Add(new Category { Id = 2, Name = "Category2", Version = 0 });
            qContext.Categories.Add(new Category { Id = 3, Name = "Category3", Version = 0 });

            IEventStoreContext eContext = Crucial.Framework.IoC.StructureMapProvider.DependencyResolver.Container.GetInstance<IEventStoreContext>();
            UserCategoryCreatedEvent e = new UserCategoryCreatedEvent(2, "Category2");
            eContext.Events.Add(new Event { Id = 2, AggregateId = 2, Data = DatabaseEventStorage.Serialize<UserCategoryCreatedEvent>(e) });
            
            eContext.AggregateRoots.Add(new AggregateRoot { EventVersion = 0, Id = 2 });

            //Act
            _commandBus.Send(new UserCategoryNameChangeCommand(2, newName, 0));
            var cat = _categoryRepo.FindBy(q => q.Id == 2).FirstOrDefault();

            //Assert
            Assert.AreEqual(newName, cat.Name);
            Assert.AreEqual(1, cat.Version);
        }

        [TestMethod]
        public void UserCategoryCreateCommandStoresEventInEventStore()
        {
            //Arrange
            var catName = "Test Category 345";
            IEventStoreContext eContext = Crucial.Framework.IoC.StructureMapProvider.DependencyResolver.Container.GetInstance<IEventStoreContext>();
            var command = new UserCategoryCreateCommand(1, catName);

            //Act
            _commandBus.Send(command);
            var e = eContext.Events.Where(x => x.AggregateId == 1).FirstOrDefault();

            //Assert
            Assert.IsNotNull(e);
            var desrializedEvent = (UserCategoryCreatedEvent)DatabaseEventStorage.DeSerialize<UserCategoryCreatedEvent>(e.Data);
            Assert.IsNotNull(desrializedEvent);
            Assert.AreEqual(catName, desrializedEvent.Name);
        }

        [TestMethod]
        public void UserCategoryCreateCommandStoresAggregateRootInEventStore()
        {
            //Arrange
            var catName = "Test Category 345";
            IEventStoreContext eContext = Crucial.Framework.IoC.StructureMapProvider.DependencyResolver.Container.GetInstance<IEventStoreContext>();
            var command = new UserCategoryCreateCommand(1, catName);

            //Act
            _commandBus.Send(command);
            var agg = eContext.AggregateRoots.Where(x => x.Id == 1).FirstOrDefault();

            //Assert
            Assert.IsNotNull(agg);
            Assert.AreEqual(0, agg.Version);
        }

        [TestMethod]
        public void UserCategoryAggregateRootCanBeConstructedFromMemento()
        {
            IEventStoreContext eContext = Crucial.Framework.IoC.StructureMapProvider.DependencyResolver.Container.GetInstance<IEventStoreContext>();

            // Arrange
            // No version - it's new
            var e1 = new UserCategoryCreateCommand(1, "Category Name");
            // Acting on Version 0
            var e2 = new UserCategoryNameChangeCommand(1, "Category Name Changed Once", 0);
            // Acting on Version 1
            var e3 = new UserCategoryNameChangeCommand(1, "Category Name Changed Twice", 1);
            // Acting on Version 2
            var e4 = new UserCategoryNameChangeCommand(1, "Category Name Changed Three Times", 2);
            // Acting on Version 3
            var e5 = new UserCategoryNameChangeCommand(1, "Category Name Changed Four Times", 3);

            // Act
            _commandBus.Send<UserCategoryCreateCommand>(e1);
            _commandBus.Send<UserCategoryNameChangeCommand>(e2);
            _commandBus.Send<UserCategoryNameChangeCommand>(e3);
            _commandBus.Send<UserCategoryNameChangeCommand>(e4);
            _commandBus.Send<UserCategoryNameChangeCommand>(e5);

            // Assert
            var m = eContext.BaseMementoes.Where(x => x.Id == 1).FirstOrDefault();
            Assert.IsNotNull(m);
            var memento = DatabaseEventStorage.DeSerialize<Crucial.Framework.DesignPatterns.CQRS.Domain.BaseMemento>(m.Data);
            UserCategory c = new UserCategory();
            c.SetMemento(memento);
            Assert.AreEqual("Category Name Changed Three Times", c.Name);
        }
    }
}
